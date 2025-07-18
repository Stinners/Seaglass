namespace Seaglass

open System
open System.IO

open Spectre.Console

open Model

module Main =

//================= Initialization =======================//

    let initFileTree = 
        let rootDir = Directory.GetCurrentDirectory()
        { filesystem = Note.buildFSRecord rootDir
          size = 40 
          isOpen = true }

    let initModel = 
        { view = Note 
          fileTree = initFileTree
          shutdown = false }


//================= Runtime =======================//


    let render model =
        match model.view with
        | Note -> Note.render model
        | Help -> Help.render model
        //| Search -> Search.render model
        | _ -> Note.render model


    let update model (input : ConsoleKeyInfo) =
        match model.view with
        | Note -> Note.update model input
        | Help -> Help.update model input
        | Search -> model


    let rec loop model (ctx: LiveDisplayContext) =
        let layout = render model
        ctx.UpdateTarget layout

        let input = Console.ReadKey(true)

        let newModel = update model input

        if not newModel.shutdown then
            loop newModel ctx

    [<EntryPoint>]
    let main _args =
        let model = initModel
        let initLayout = render model
        AnsiConsole.Live(initLayout).Start(loop model)
        AnsiConsole.Clear()

        0
