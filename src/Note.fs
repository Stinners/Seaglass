namespace Seaglass

open Spectre.Console
open Spectre.Console.Rendering
open Serilog

open System
open System.IO

open Model
open Utils

(* The Note view is broken into two panes - Filetree and Content *)

module Metrics = 
    let columns () = Console.BufferWidth

    let fileTreeSize filetree = 
        let fileTreeFraction = float filetree.size / 100.0
        let nRows = fileTreeFraction * float (columns())
        Math.Floor nRows |> int

    let rows () = Console.BufferHeight

    let noteSize filetree  = 
        if filetree.isOpen then 
            columns() - fileTreeSize filetree
        else 
            columns()

    // Note the actually availible space for characters may be 2 columns 
    // narrower than this - once the borders are acounted for
    let lineLength filetree = 
        let availibleWidth = noteSize filetree - 2 * Styles.TextPadding - 2
        Math.Min(availibleWidth, Styles.MaxTextWidth)

    let characterSpace filetree = noteSize filetree * (rows() - 6)


module FiletreePane =

    //======================= Update Helpers ======================//
    
    let updateFocusUp filetree =
        Log.Information "updateFocusUp"
        let newFileState = Cursor.moveFocusUp filetree.filesystem |> Cursor.focus 
        { filetree with filesystem = newFileState }

    let updateFocusDown filetree =
        Log.Information "updateFocusDown"
        let newFileState = Cursor.moveFocusDown filetree.filesystem |> Cursor.focus 
        { filetree with filesystem = newFileState }

    let updateFileTreeSize (filetree: FileTree) amount =
        let minSize = 10
        let maxSize = 80
        let newSize = Math.Clamp(filetree.size + amount, minSize, maxSize)
        { filetree with size = newSize }

    let toggleFiletreeFocus (filetree : FileTree)  = 
        { filetree with isFocused = not filetree.isFocused }

    let toggleFileTree (filetree : FileTree) =
        let isOpen = not filetree.isOpen
        let isFocused = if not isOpen then false else filetree.isFocused 
        {filetree with isOpen = isOpen; isFocused = isFocused }
    
    //======================= Update ======================//
    
    let updateFiletree (filetree : FileTree) (input : ConsoleKeyInfo) (mods : ModifierKeys) =
        match input.Key with
        // Fire only if the tree is focused
        | ConsoleKey.UpArrow when filetree.isFocused -> updateFocusUp filetree
        | ConsoleKey.DownArrow when filetree.isFocused -> updateFocusDown filetree

        | ConsoleKey.LeftArrow when mods.Shift && filetree.isOpen -> updateFileTreeSize filetree -10
        | ConsoleKey.RightArrow when mods.Shift && filetree.isOpen -> updateFileTreeSize filetree 10
        | ConsoleKey.F when mods.Shift -> toggleFileTree filetree
        | ConsoleKey.Tab when filetree.isOpen -> toggleFiletreeFocus filetree 
        | _ -> filetree

    //======================= Render Helpers ======================//
    
    let fsRecordAddEmoji record name = 
        match record.root with 
        | File -> name
        | Directory expanded ->
            let symbol = if expanded then ":open_file_folder:" else ":file_folder:"
            $"{symbol} {name}"

    let fsRecordFocus (record : FSRecord) name = 
        if record.isFocused then $"[green]{name}[/]"
        else name

    let fsRecordMarkup record =
        record 
        |> Utils.fsRecordName
        |> fsRecordFocus record
        |> fsRecordAddEmoji record
        |> Markup

    let rec renderNode (tree : IHasTreeNodes) (record : FSRecord) =
        let displayName = fsRecordMarkup record
        match record.root with
        | File -> tree.AddNode displayName |> ignore
        | Directory expanded ->
            if expanded then
                let node = tree.AddNode displayName
                Seq.iter (renderNode node) record.contents
            else
                tree.AddNode displayName |> ignore

    
    //======================= Main Render ======================//
    
    let render (filetree : FileTree) (layout : Layout) =
        let record = filetree.filesystem
        if filetree.isOpen then 
            let root = Tree(Utils.fsRecordName record)
            Seq.iter (renderNode root) record.contents

            let panel = Panel(
                root,
                Padding = Padding(0,0,0,4),
                Border = Styles.border filetree.isFocused,
                Height = Metrics.rows()
            )
        
            layout.Update(panel).Size(Metrics.fileTreeSize filetree)
        else 
            layout.Invisible() 


module ContentPane = 

    //======================= Update Helpers ======================//
    
    let updateScroll (note : OpenNote) (step : int) = 
        // Don't allow scroll to make invalid values
        let nBlocks = Math.Max(note.text.Length - 1, 0)
        let newScroll = Math.Clamp(
            note.scroll + step,
            min = 0,
            max = nBlocks
        )

        // TODO: prevet scrolling past the end of the display 
        Log.Debug($"Updaing scroll from {note.scroll} to {newScroll}")
        { note with scroll = newScroll }
    
    //======================= Update ======================//
    
    let updateContent (note : OpenNote) (input : ConsoleKeyInfo) (_ : ModifierKeys) (noteFocused : bool) =
        //Log.Debug($"Note Input: {input.Key} | control: {control} | shift: {shift}") 

        match input.Key with
        | ConsoleKey.UpArrow when noteFocused -> updateScroll note -1 
        | ConsoleKey.DownArrow when noteFocused -> updateScroll note 2

        | _ -> note
    
    //======================= Render Helpers ======================//
    
    let addLineBreaks (value : IRenderable) (blocks : IRenderable array) = 
        seq {
            for block in blocks do
                yield block 
                yield value
        }
        |> Seq.toArray
    
    let renderNoteContents note: Align = 
        if note.name = "" then 
           Markup("Note Placeholder") |> Align.Center
        else 
            let allBlocks = addLineBreaks (Markup("  ")) note.text[note.scroll..]
            allBlocks
            |> fun rows -> Rows(rows)
            |> Align.Left

    
    //======================= Main Render ======================//
    
    let render (note : OpenNote) (filetree : FileTree) (layout : Layout) : Layout = 
        let contents = renderNoteContents note
        let textWidth = Metrics.lineLength filetree 

        // We need all this to make controling the text width actually work
        let textArea = Layout("TextArea") |> _.Update(contents) |> _.Size(textWidth)
        let textLayout = Layout("TextContainer").SplitColumns([|textArea|])

        let panel = Panel(
            Align.Center(textLayout),
            Header = PanelHeader(note.name),
            Padding = Padding(2,0,2,1),
            Border = Styles.border (not filetree.isFocused),
            Height = Metrics.rows()
        )

        layout.Update(panel).Size(Metrics.noteSize filetree)


module Note =

    //======================= Setup ======================//

    let makeFile file = { root = File; path = file; contents = []; isFocused = false }

    let rec buildFSRecord rootDir = 
            let directories = Directory.GetDirectories rootDir |> Seq.map (buildFSRecord)
            let files = Directory.GetFiles rootDir |> Seq.map makeFile

            let contents = Seq.append directories files |> Seq.toList

            { root = Directory (expanded = false)
              path = rootDir
              contents = contents
              isFocused = false }



    //======================= Update ============================//


    let isMarkdownFile (path : string) = (Path.GetExtension path) = ".md"

    let getNoteText (filepath : string): IRenderable array = 
        let text = File.ReadAllText filepath |> Markup.Escape
        if isMarkdownFile filepath then 
            text |> Markdown.parseMarkdown |> Array.map (fun text -> Markup(text))
        else 
            [| Markup(text.EscapeMarkup()) |]

    let getNoteHeader (file : FSRecord) = 
        let name = Utils.fsRecordName file
        match Path.GetFileNameWithoutExtension name with
        | "" -> name
        | noExtension -> noExtension

    let openFile note file = 
        { note with name = getNoteHeader file
                    path = file.path
                    text = getNoteText file.path
                    scroll = 0 }

    let selectFile (model : Model) = 
        let filesystem = model.filetree.filesystem
        let focusedNode = Cursor.focusedRecord filesystem

        if focusedNode.root.IsFile then 
            { model with note = openFile model.note focusedNode }
        else 
            { model with filetree.filesystem = Cursor.toggleExpand filesystem }

    let update (model : Model) (input : ConsoleKeyInfo) (mods : ModifierKeys) =
        let isNoteFocused = not model.filetree.isFocused

        let filetree = FiletreePane.updateFiletree model.filetree input mods
        let note = ContentPane.updateContent model.note input mods isNoteFocused

        let model = { model with filetree = filetree; note = note }

        // Global update
        match input.Key with 
        | ConsoleKey.E when isNoteFocused && File.Exists model.note.path ->
            { model with command = Some(Editor) }
        | ConsoleKey.Q -> { model with shutdown = true }
        | ConsoleKey.H -> { model with view = Help }
        | ConsoleKey.Enter -> selectFile model
        | _ -> model 

    //======================= Render ============================//

    let joinBlocks (blocks : array<string>) = Array.fold (fun state block -> state + "\n\n" + block) "" blocks

    //==================== Handling Widths ============================//

    let MaxTextWitdth = 100

    let getTextWidth layoutWidth = Math.Min(MaxTextWitdth, layoutWidth)
    let getTextPadding layoutWidth = 
        let textWidth = getTextWidth layoutWidth

        Log.Information $"Layout width is: {layoutWidth}"
        Log.Information $"textWidth is: {textWidth}"
        (layoutWidth - textWidth) / 2 

    let render (model : Model) : IRenderable =
        let notePane = ContentPane.render model.note model.filetree (Layout("Note"))
        let filetreePane = FiletreePane.render model.filetree (Layout("Filetree"))

        Layout("SeaGlass").SplitColumns([|
            filetreePane
            notePane;
        |])
