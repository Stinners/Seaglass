namespace Seaglass 

open Spectre.Console

open System

open Model

module Note = 

    //======================= Update ============================//
    
    let updateFileTreeSize (model: Model) amount = 
        let minSize = 10 
        let maxSize = 80
        let newSize = Math.Clamp(model.fileTree.size + amount, minSize, maxSize)
        { model with fileTree.size = newSize }


    let toggleFileTree model = 
        {model with fileTree.isOpen = not model.fileTree.isOpen }


    let render model = 
        let notePane = Layout("Note")
        let fileTreePane = 
            if model.fileTree.isOpen then 
                Layout("Files").Visible()
            else 
                Layout("Files").Invisible()

        let fileTreeSize = model.fileTree.size
        Layout("SeaGlass").SplitColumns([|
            fileTreePane.Ratio(fileTreeSize); 
            notePane.Ratio(100 - fileTreeSize)|]
        )


    let hadModifier (input : ConsoleKeyInfo) (modifier : ConsoleModifiers) = 
        (input.Modifiers &&& modifier) <> ConsoleModifiers.Control


    // Probably set a different update function for each of the views
    let update (model : Model) (input : ConsoleKeyInfo) = 

        let control = hadModifier input ConsoleModifiers.Control 
        //let alt = hadModifier input ConsoleModifiers.Alt 
        //let shift = hadModifier input ConsoleModifiers.Shift

        match input.Key with 
        | ConsoleKey.LeftArrow -> updateFileTreeSize model -10 
        | ConsoleKey.RightArrow -> updateFileTreeSize model 10
        | ConsoleKey.F when control -> toggleFileTree model
        | ConsoleKey.Q -> { model with shutdown = true }
        | _ -> model

