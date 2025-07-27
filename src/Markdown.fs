namespace Seaglass 

open System
open System.Text

open Markdig
open Markdig.Syntax;
open Markdig.Syntax.Inlines;
open Serilog

open Styles

module Markdown = 

    let getSlice (elem : LiteralInline) = 
        let start = elem.Content.Start
        let length = elem.Content.Length
        elem.Content.Text.Substring(start, length)

    let private build (sb : StringBuilder) (text : string) = 
        Log.Debug $"Writing '{text}'"
        sb.Append(text) |> ignore


    let newLine output = 
        build output System.Environment.NewLine
        build output System.Environment.NewLine



    let rec renderInlineElements (output : StringBuilder) (block : ContainerInline) =

        for elem in block do 
            Log.Debug($"Markdown Inline {elem.GetType().Name}")

            match elem with 
            | :? LiteralInline as literal -> build output (getSlice literal)
            | _ ->  raise (Exception("Unknown element"))


    let markupHeader output (header : HeadingBlock) = 
        let hashes = String('#', header.Level)
        let markup = markdownMarkup Header

        build output $"{markup}{hashes} "
        renderInlineElements output header.Inline
        build output "[/]"


    let markupParagraph output (paragraph : ParagraphBlock) = renderInlineElements output paragraph.Inline



    let parseMarkdown (text : string) = 
        let output = StringBuilder(Capacity = text.Length)
        let markdown = Markdown.Parse text 

        for block in markdown do 

            Log.Debug($"Markdown Block: {block.GetType().Name}")

            match block with 
            | :? HeadingBlock as heading -> markupHeader output heading
            | :? ParagraphBlock as paragraph -> markupParagraph output paragraph
            | _ -> ()

            newLine output
        output.ToString()


