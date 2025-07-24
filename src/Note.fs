namespace Seaglass

open Spectre.Console
open Spectre.Console.Rendering
open Serilog

open System
open System.IO

open Model

module Note =

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
    
    let updateFocusUp filetree =
        Log.Information "updateFocusUp"
        let newFileState = Cursor.moveFocusUp filetree.filesystem |> Cursor.focus 
        { filetree with filesystem = newFileState }

    let updateFocusDown filetree =
        Log.Information "updateFocusDown"
        let newFileState = Cursor.moveFocusDown filetree.filesystem |> Cursor.focus 
        { filetree with filesystem = newFileState }


    let updateFileTreeSize (model: Model) amount =
        let minSize = 10
        let maxSize = 80
        let newSize = Math.Clamp(model.fileTree.size + amount, minSize, maxSize)
        { model with fileTree.size = newSize }

    let toggleFiletreeFocus model = 
        { model with fileTree.isFocused = not model.fileTree.isFocused }


    let toggleFileTree model =
        let isOpen = not model.fileTree.isOpen
        let isFocused = if not isOpen then false else model.fileTree.isFocused 
        {model with fileTree.isOpen = isOpen; fileTree.isFocused = isFocused }


    let hadModifier (input : ConsoleKeyInfo) (modifier : ConsoleModifiers) =
        (input.Modifiers &&& modifier) = ConsoleModifiers.Control


    let openFile model file = 
        let filename = Utils.fsRecordName file
        let headerName = filename |> Path.GetFileNameWithoutExtension
        let headerName' = if headerName = "" then filename else headerName
        let fileText = File.ReadAllText file.path
        { model with note.name = headerName'; note.path = file.path; note.text = fileText }

    let selectFile (model : Model) = 
        let filesystem = model.fileTree.filesystem
        let focused = Cursor.focusedRecord filesystem

        if focused.root.IsFile then 
            openFile model focused 
        else 
            { model with fileTree.filesystem = Cursor.toggleExpand filesystem }


    let update (model : Model) (input : ConsoleKeyInfo) =

        let files = model.fileTree.isFocused
        let filesVisible = model.fileTree.isOpen
        let control = hadModifier input ConsoleModifiers.Control
        //let alt = hadModifier input ConsoleModifiers.Alt
        //let shift = hadModifier input ConsoleModifiers.Shift
        

        match input.Key with
        // Fire only if the tree is focused
        | ConsoleKey.UpArrow when files -> { model with fileTree = updateFocusUp model.fileTree }
        | ConsoleKey.DownArrow when files -> { model with fileTree = updateFocusDown model.fileTree }

        // Fire only if the note is focused

        // Fire regardless of filetree focus
        | ConsoleKey.LeftArrow when control && filesVisible -> updateFileTreeSize model -10
        | ConsoleKey.RightArrow when control && filesVisible -> updateFileTreeSize model 10
        | ConsoleKey.F when control -> toggleFileTree model
        | ConsoleKey.Tab when filesVisible -> toggleFiletreeFocus model 
        | ConsoleKey.Q -> { model with shutdown = true }
        | ConsoleKey.H -> { model with view = Help }
        | ConsoleKey.Enter -> selectFile model
        | _ -> model

    //======================= Render ============================//
    
    //======================= Handling Sizes ============================//
    
    let columns () = Console.BufferWidth
    let rows () = Console.BufferHeight
    
    let fileTreeSize model = 
        let fileTreeFraction = float model.fileTree.size / 100.0
        let nRows = fileTreeFraction * float (columns())
        Math.Floor nRows |> int

    let noteSize model = 
        if model.fileTree.isOpen then 
            columns() - fileTreeSize model 
        else 
            columns()

    let lineLength model = 
        let availibleWidth = noteSize model - 2 * Styles.TextPadding - 2
        Math.Min(availibleWidth, Styles.MaxTextWidth)

    //====================================================================//


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

    let renderFileTree (model : Model) (layout : Layout) =
        let filetree = model.fileTree
        let record = filetree.filesystem
        if filetree.isOpen then 
            let root = Tree(Utils.fsRecordName record)
            Seq.iter (renderNode root) record.contents

            let panel = Panel(
                root,
                Padding = Padding(0,0,0,2),
                Border = Styles.border filetree.isFocused,
                Height = rows()
            )
        
            layout.Update(panel).Size(fileTreeSize model)
        else 
            layout.Invisible() 

    let renderNoteContents model = 
        let note = model.note 
        let text = if note.name = "" then 
                       Markup("Note Placeholder") |> Align.Center
                   else 
                       note.text |> Markup.Escape |> Markup |> Align.Left
        text 

    //==================== Handling Widths ============================//

    let MaxTextWitdth = 100

    let getTextWidth layoutWidth = Math.Min(MaxTextWitdth, layoutWidth)
    let getTextPadding layoutWidth = 
        let textWidth = getTextWidth layoutWidth

        Log.Information $"Layout width is: {layoutWidth}"
        Log.Information $"textWidth is: {textWidth}"
        (layoutWidth - textWidth) / 2 


    let renderNote (model : Model) (layout : Layout) : Layout = 
        let contents = renderNoteContents model
        let textWidth = lineLength model 

        // We need all this to make controling the text width actually work
        let textArea = Layout("TextArea") |> _.Update(contents) |> _.Size(textWidth)
        let textLayout = Layout("TextContainer").SplitColumns([|textArea|])

        let panel = Panel(
            Align.Center(textLayout),
            Header = PanelHeader(model.note.name),
            Padding = Padding(2,1,2,1),
            Border = Styles.border (not model.fileTree.isFocused),
            Height = rows()
        )

        layout.Update(panel).Size(noteSize model)
        


    let render (model : Model) : IRenderable =
        let notePane = renderNote model (Layout("Note"))
        let filetreePane = renderFileTree model (Layout("Filetree"))

        Layout("SeaGlass").SplitColumns([|
            filetreePane
            notePane;
        |])
