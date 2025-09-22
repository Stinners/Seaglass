namespace Seaglass

open System
open System.IO

open Spectre.Console
open Serilog

open Model
open Utils

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
          filetree = initFileTree
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
        let mods = getModifiers input
        match model.view with
        | Note -> Note.update model input mods
        | Help -> Help.update model input
        | Search -> model


    // Properties which we want to be true of the model at the start of every loop 
    let reset model =
        AnsiConsole.Cursor.Show(false)
        { model with command = None }
    

    let handleCommand model = 
        match model.command with 
        | None -> model   // Shutdown 
        | Some(Editor) -> 
            Editor.launchEditor model


    let rec renderLoop oldModel =
        let layout = render oldModel
        AnsiConsole.Cursor.SetPosition(0,0)
        AnsiConsole.Write(layout)

        let input = Console.ReadKey(true)
        let newModel = update oldModel input |> handleCommand

        if not newModel.shutdown then 
            renderLoop (reset newModel)

    [<EntryPoint>]
    let main _args =
        initLogging()

        Log.Information " ======== Starting Seaglass ========="

        try
            let model = initModel
            renderLoop model

            // If the renderLoop crashes we want to make sure we don't clear the stack trace 
            // so this doesn't go in the finally block
            AnsiConsole.Clear()
        finally
            AnsiConsole.Cursor.Show(true)

        Log.Information " ======== Closing Seaglass =========="

        0
