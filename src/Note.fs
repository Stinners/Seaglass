namespace Seaglass

open Spectre.Console
open Spectre.Console.Rendering

open System
open System.IO

open Model

module Note =

    let makeFile file = { root = File; path = file; contents = []; isFocused = false }

    let rec buildFSRecord (rootDir : string) =
        let directories = Directory.GetDirectories rootDir |> Seq.map (buildFSRecord)
        let files = Directory.GetFiles rootDir |> Seq.map makeFile

        let contents = Seq.append directories files |> Seq.toList

        { root = Directory (expanded = false)
          path = rootDir
          contents = contents
          isFocused = false }


    //======================= Update ============================//

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

        let control = hadModifier input ConsoleModifiers.Control
        //let alt = hadModifier input ConsoleModifiers.Alt
        //let shift = hadModifier input ConsoleModifiers.Shift

        match input.Key with
        | ConsoleKey.LeftArrow -> updateFileTreeSize model -10
        | ConsoleKey.RightArrow -> updateFileTreeSize model 10
        | ConsoleKey.F when control -> toggleFileTree model
        | ConsoleKey.Q -> { model with shutdown = true }
        | ConsoleKey.H -> { model with view = Help }
        | _ -> model

    //======================= Handling Focus ============================//
    
    let isAtomic record = 
        if record.contents.Length = 0 then false 
        else match record.root with 
             | File -> false 
             | Directory expanded -> expanded

    let focus record = { record with isFocused = true }
    let unfocus record = { record with isFocused = false }

    type Direction = Up | Down | Done

    // TODO think about if I can impliment a Zipper here 
    // There must be a better way to do this
    let rec 
        // Apply move focus to the particular child of a record
        recurseMoveFocus record direction idx : FSRecord * Direction = 
            let contents = record.contents
            let updatedChild, newDirection = moveFocus direction (contents[idx])
            let newContents = List.updateAt idx updatedChild contents
            { record with contents = newContents }, newDirection 

        and 

        moveFocus (direction : Direction) (record : FSRecord) = 
            // If the record is atomic then just focus it
            if isAtomic record && not record.isFocused then 
                focus record, Done

            else if isAtomic && record.isFocused then 
                unfocus record, direction

            else 
                let currentFocus = Utils.findIndex _.isFocused record.contents

                match currentFocus with 
                | None ->
                    match record.isFocused, direction with 
                    // Entering the directory from the top
                    | false, Down ->  focus record, Done 

                    // Entering the first child of the directory from the top 
                    | true, Down -> (focusChild 0 record), Done 

                    // TODO: there must be a cleaner way to do this
                    // Entering the element from the bottem 
                    | false, Up -> 
                        let lastIdx = record.contents.Length - 1
                        let newContents = recurseMoveFocus record direction lastIdx
                        { record with isFocused = true; contents = newContents }, Done

                    // Exiting the directory from the top
                    | true, Up -> { record with isFocused = false }, Up

                    // Getting to this point without a direction should be impossible 
                    | _, Done -> raise (Exception("Impossible"))

                | Some childIdx -> 
                    let newContents, newDirection = recurseMoveFocus record direction childIdx

                    if newDirection.isDone then
                        { record with contents = newContents }, Done 

                    else 
                        // We want to unset the focus on the currently targeted element 
                        // Then try to find the next element 
                        // If we can find it then we recurse onto it 
                        // Else we're at an edge, so we propagate direction up




    //======================= Render ============================//

    let fsRecordName record = Path.GetFileName record.path

    let fsRecordMarkup record =
        let name = fsRecordName record |> Markup.Escape
        match record.root with
        | File -> Markup name
        | Directory expanded ->
            let symbol = if expanded then ":open_file_folder:" else ":file_folder:"
            Markup ($"{symbol} {name}")


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
