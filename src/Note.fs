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


    let toggleFileTree model =
        {model with fileTree.isOpen = not model.fileTree.isOpen }


    let hadModifier (input : ConsoleKeyInfo) (modifier : ConsoleModifiers) =
        (input.Modifiers &&& modifier) = ConsoleModifiers.Control


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
        | ConsoleKey.Q -> { model with shutdown = true }
        | ConsoleKey.H -> { model with view = Help }
        | _ -> model

    //======================= Render ============================//

    let fsRecordName record = Path.GetFileName record.path

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
        |> fsRecordName
        |> fsRecordFocus record
        |> fsRecordAddEmoji record
        |> Markup

    let renderFileTree (record : FSRecord) (layout : Layout) =

        let rec renderNode (tree : IHasTreeNodes) (record : FSRecord) =
            let displayName = fsRecordMarkup record
            match record.root with
            | File -> tree.AddNode displayName |> ignore
            | Directory (expanded) ->
                if expanded then
                    let node = tree.AddNode displayName
                    Seq.iter (renderNode node) record.contents
                else
                    tree.AddNode displayName |> ignore

        let root = Tree(fsRecordName record)
        Seq.iter (renderNode root) record.contents

        let panel = Panel(
            root,
            Expand = true,
            Padding = Padding(0,0,0,2)
        )
    
        layout.Update(panel)

    let renderNote _model (layout : Layout) : Layout = 
        let panel = Panel(
            Align.Center(Markup("Note Placeholder")),
            Expand = true,
            Header = PanelHeader("Note"),
            Padding = Padding(1,1,1,1)
        )

        layout.Update(panel)
        


    let render model : IRenderable =
        let notePane = Layout("Note") |> renderNote model
        let fileTreePane =
            if model.fileTree.isOpen then
                Layout("Files")
                |> _.Visible()
                |> renderFileTree model.fileTree.filesystem
            else
                Layout("Files").Invisible()

        let fileTreeSize = model.fileTree.size
        Layout("SeaGlass").SplitColumns([|
            fileTreePane.Ratio(fileTreeSize);
            notePane.Ratio(100 - fileTreeSize)|]
        )
