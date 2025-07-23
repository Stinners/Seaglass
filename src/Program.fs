namespace Seaglass

open System
open System.IO

open Spectre.Console
open Serilog

open Model

module Main =

//================= Initialization =======================//

    let initFileTree = 
        let files = Note.buildFSRecord (Directory.GetCurrentDirectory())
        let filesystem = { files with root = Directory (expanded = true) }
        { filesystem = filesystem
          size = 40 
          isOpen = true 
          isFocused = true }

    let initNote = 
        { name = "" }

    let initModel = 
        { view = Note 
          fileTree = initFileTree
          shutdown = false
          note =  initNote }

    let initLogging () = 
        Log.Logger <-
            LoggerConfiguration()
            |> _.MinimumLevel.Debug()
            |> _.WriteTo.File("log.txt", rollingInterval = RollingInterval.Day)
            |> _.CreateLogger()


//================= Runtime =======================//


    let render (model : Model) =
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
        initLogging()

        Log.Information " ======== Starting Seaglass ========="

        let model = initModel
        let initLayout = render model
        AnsiConsole.Live(initLayout).Start(loop model)
        AnsiConsole.Clear()

        Log.Information " ======== Closing Seaglass =========="

        0
