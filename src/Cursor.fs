namespace Seaglass 

open Model

module Cursor  = 

    let private isAtomic  record = 
        match record.root with 
        | File -> true
        | Directory expanded -> not expanded || record.contents.Length = 0 


    let focus (record : FSRecord) = { record with isFocused = true }
    let private unfocus (record : FSRecord) = { record with isFocused = false }


    let private focusedChild record = 
        let rec go idx (children : List<FSRecord>) = 
            match children with 
            | head :: _ when head.isFocused -> Some(idx, head)
            | _ :: tail -> go (idx+1) tail 
            | [] -> None 
        go 0 record.contents


    let private updateChildAt idx updatedChild record = 
        let newContents = List.updateAt idx updatedChild record.contents
        { record with contents = newContents }


    // These are called when a directory is focused but none of it's children are 
    let private focusFirstChild record =
        let firstChild = record.contents[0]
        updateChildAt 0 (focus firstChild) record


    let rec private focusLastChild record = 
        let lastIdx = record.contents.Length - 1 
        let lastChild = record.contents[lastIdx]
        let updatedChild = if isAtomic lastChild then focus lastChild
                           else focusLastChild lastChild
        record 
        |> focus
        |> updateChildAt lastIdx updatedChild

    // We assume that this is always called on a focused element
    let rec moveFocusDown record = 
        if isAtomic record then 
            unfocus record
        else 
            match focusedChild record with 
            // If we have a focused child then we recurse onto that 
            | Some(idx, child) -> 
                let updatedChild = moveFocusDown child 
                let firstUpdate = updateChildAt idx updatedChild record

                // If the updated record is focused then something has changed 
                // furthe down the stack and we can return the changes value
                if updatedChild.isFocused then firstUpdate

                // If it's not then we need to move to the next element 
                else 
                    let isLastChild = idx = (firstUpdate.contents.Length - 1)

                    // If the focus element is the last one at this level, we unfocus 
                    // this level completly and raise to the next one
                    if isLastChild then 
                        unfocus firstUpdate

                    // Else know the next element exists and is unfocused
                    // so we can jsut focus it 
                    else 
                        let updatedNextChild = focus firstUpdate.contents[idx+1]
                        updateChildAt (idx+1) updatedNextChild firstUpdate

            // Else we need to enter the directory - focus its' first child
            | None -> 
                focusFirstChild record


    // This will only be called on an unfocused element when we're entering a new 
    // directory
    let rec moveFocusUp record = 
        if isAtomic record then 
            unfocus record 
        else
            match focusedChild record with 
            | Some(idx, child) -> 
                let updatedChild = moveFocusUp child
                let firstUpdate = updateChildAt idx updatedChild record

                // If the returned value is focused then something has been modified down 
                // the stack and we can just return that
                if updatedChild.isFocused then 
                    firstUpdate 

                // Else we need to look at the next value
                else 
                    let isFirstChild = idx = 0 

                    // If this is the first record then we just unfocus it, so the 
                    // current directory decomes the deepest focused item 
                    if isFirstChild then 
                        firstUpdate

                    // Else we recurse to the next child up
                    else 
                        let nextChild = firstUpdate.contents[idx-1]
                        if isAtomic nextChild then 
                            updateChildAt (idx-1) (focus nextChild) firstUpdate

                        else 
                            let updatedNextChild = moveFocusUp firstUpdate.contents[idx-1]
                            updateChildAt (idx-1) updatedNextChild firstUpdate

            // Note this cases problems with the root, which is always focused
            | None -> 
                // If the record is focused then we're leaving it and going up 
                if record.isFocused then unfocus record 
                // Else we enter the record at it's last child
                else focusLastChild record


    let rec toggleExpand (record : FSRecord) : FSRecord  = 
        match record.root with 
        | File -> record 
        | Directory expanded -> 
            match focusedChild record with 
            | None -> { record with root = Directory (expanded = not expanded) }
            | Some (idx, child) -> 
                let updatedChild = toggleExpand child 
                updateChildAt idx updatedChild record

    let rec focusedRecord (record : FSRecord) : FSRecord = 
        match record.root with 
        | File -> record
        | Directory _ -> 
            match focusedChild record with 
            | None -> record
            | Some (_, child) -> focusedRecord child
