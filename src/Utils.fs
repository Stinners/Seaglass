namespace Seaglass 

open System 
open System.IO

open Model 

module Utils = 


    type public ModifierKeys = {
        Control : bool 
        Alt : bool 
        Shift : bool 
    }

    let hadModifier (input : ConsoleKeyInfo) (modifier : ConsoleModifiers) =
        (input.Modifiers &&& modifier) = modifier

    let getModifiers (input : ConsoleKeyInfo) = {
        Control = hadModifier input ConsoleModifiers.Control
        Alt = hadModifier input ConsoleModifiers.Alt
        Shift = hadModifier input ConsoleModifiers.Shift
    }

    let public intersperse (stream : 'T seq) (value : 'T) =
        seq {
            for i in stream do
            yield i 
            yield value 
        }
        
     

    let findIndex predicate source = 
        try Some(Seq.findIndex predicate source) with | _ -> None

    
    let fsRecordName (record : FSRecord) = Path.GetFileName record.path

    let unwrap (def : 'T) (option : Nullable<'T>) = option |> Option.ofNullable |> Option.defaultValue def

