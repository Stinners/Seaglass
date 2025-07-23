namespace Seaglass 

open System.IO

open Model 

module Utils = 

    let findIndex predicate source = 
        try Some(Seq.findIndex predicate source) with | _ -> None

    
    let fsRecordName record = Path.GetFileName record.path

