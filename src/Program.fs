namespace Seaglass

open System

open Spectre.Console

open Model

module Main =

    let render model =
        match model.view with
        | Note _ -> Note.render model
        //| Help -> Help.render model
        //| Search -> Search.render model
        | _ -> Note.render model

    let update model (input : ConsoleKeyInfo) =
        match model.view with
        | Note _ -> Note.update model input
        //| Help -> Help.render model
        //| Search -> Search.render model
        | _ -> Note.update model input


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
