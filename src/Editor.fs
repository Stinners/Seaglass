namespace Seaglass 

open System.Diagnostics

open Serilog

open Model

module Editor = 

    // We probably want to get this from the editor environment 
    // variable
    let EditorCommand = "nvim"

    let launchEditor model =
        let path = model.note.path
        Log.Information $"Launching Editor: {EditorCommand} {path}"
        use editorProcess = new Process(
            StartInfo = ProcessStartInfo(
                UseShellExecute = false ,
                CreateNoWindow = true,
                FileName = EditorCommand,
                Arguments = path
            )
        )
        editorProcess.Start() |> ignore
        editorProcess.WaitForExit() |> ignore

        { model with note.text = Note.getNoteText path } 
