#r "nuget: Markdig"

open Markdig
open Markdig.Syntax
open Markdig.Syntax.Inlines

open System.IO

let mdFile = "resources/lists.md"

let displayMd level (md: MarkdownObject) =
    let indent = String.replicate level "  "
    printfn "%s [%A] " (indent + "  ") (md.GetType())

let displayInline level (elem: LiteralInline) = 
    let indent = String.replicate level "  "
    let start = elem.Content.Start
    let length = elem.Content.Length
    let text = elem.Content.Text.Substring(start, length)
    printfn "%s [%A %s]" indent  (elem.GetType()) text


let rec displayBlock level (md: MarkdownObject) = 
    displayMd level md

    let nextLevel = level+1
    match md with 
    | :? ContainerBlock as container -> 
        for block in container do 
            displayBlock nextLevel block

    // A leaf block contains a (nullable) ContainerInline
    | :? LeafBlock as leaf -> 
        let inlineContainer = leaf.Inline
        if not <| obj.ReferenceEquals(inlineContainer, null) then 
            displayBlock (nextLevel+1) (leaf.Inline)

    | :? ContainerInline as container -> 
        for block in container do 
            displayBlock nextLevel block

    // All actual content is stored at the literal inline level
    | :? LiteralInline as elem ->
        displayInline nextLevel elem

    // Onlye tracked if we're considering trivia, can probable ignore in rendering
    | :? LineBreakInline as linebreak -> 
        displayMd nextLevel linebreak

    | _ -> printfn "Unexpected Node type %A" (md.GetType())



let main _ = 
    let mdText = File.ReadAllText mdFile

    printfn "%s\n=========\n" mdText

    let pipeline = MarkdownPipelineBuilder().EnableTrackTrivia().Build()
    let markdown = Markdown.Parse(mdText, pipeline)

    // At the top level a document is a sequence of blocks
    for block in markdown do 
        displayBlock 0 block


    0

main () 
