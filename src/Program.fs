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
        { name = ""
          path = ""
          text = [||] 
          scroll = 0 }

    let initModel = 
        { view = Note 
          fileTree = initFileTree
          shutdown = false
          note =  initNote 
          command = None }

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


    // Properties which we want to be true of the model at the start of every loop 
    let reset model =
        { model with command = None }
    

    let handleCommand model = 
        match model.command with 
        | None -> model   // Shutdown 
        | Some(Editor) -> 
            Editor.launchEditor model


    let rec renderLoop oldModel =
        let layout = render oldModel
        AnsiConsole.Write(layout)

        let input = Console.ReadKey(true)
        let newModel = update oldModel input |> handleCommand

        if not newModel.shutdown then 
            renderLoop (reset newModel)
        else 
            AnsiConsole.Clear()

        (*
        if not newModel.shutdown && Option.isNone model.command then
            renderLoop newModel ctx
        else 
            Log.Information "Breaking render loop"
        *)



    [<EntryPoint>]
    let main _args =
        initLogging()

        Log.Information " ======== Starting Seaglass ========="

        let model = initModel
        renderLoop model
        AnsiConsole.Clear()

        Log.Information " ======== Closing Seaglass =========="

        0
