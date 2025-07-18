namespace Seaglass 

module Utils = 

    let findIndex predicate source = 
        try Some(Seq.findIndex predicate source) with | _ -> None

