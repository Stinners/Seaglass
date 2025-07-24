namespace Seaglass 

open System 
open System.IO

open Model 

module Utils = 

    let findIndex predicate source = 
        try Some(Seq.findIndex predicate source) with | _ -> None

    
    let fsRecordName (record : FSRecord) = Path.GetFileName record.path

    let unwrap (def : 'T) (option : Nullable<'T>) = option |> Option.ofNullable |> Option.defaultValue def

