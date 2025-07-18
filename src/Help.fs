namespace Seaglass 

open System

open Spectre.Console
open Spectre.Console.Rendering

open Model

module Help = 

    let render _model : IRenderable = 
        let helpText = Markup("Help")

        let panel = Panel(
            Align.Center(helpText),
            Header = PanelHeader("Help"),
            Expand = true,
            Border = BoxBorder.Rounded
        )

        panel
        

    let update (model : Model) (input : ConsoleKeyInfo) = 

        match input.Key with 
        | ConsoleKey.H -> { model with view = Note }
        | _ -> model

