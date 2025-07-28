namespace Seaglass 

open System
open System.Text

open Markdig
open Markdig.Syntax;
open Markdig.Syntax.Inlines;
open Serilog

open Styles

module Markdown = 

    let private getSlice (elem : LiteralInline) = 
        let start = elem.Content.Start
        let length = elem.Content.Length
        elem.Content.Text.Substring(start, length)

    let private build (sb : StringBuilder) (text : string) = sb.Append(text) |> ignore


    let rec renderInlineElements (output : StringBuilder) (block : ContainerInline) =

        for elem in block do 
            Log.Debug($"Markdown Inline {elem.GetType().Name}")

            match elem with 
            | :? LiteralInline as literal -> build output (getSlice literal)
            | _ ->  raise (Exception("Unknown element"))


    let private markupHeader output (header : HeadingBlock) = 
        let hashes = String('#', header.Level)
        let markup = markdownMarkup Header

        build output $"{markup}{hashes} "
        renderInlineElements output header.Inline
        build output "[/]"


    let private markupParagraph output (paragraph : ParagraphBlock) = renderInlineElements output paragraph.Inline


    let private renderMarkdownBlock (block : Block) : string = 
        Log.Debug($"Markdown Block: {block.GetType().Name}")
        let output = StringBuilder()

        match block with 
        | :? HeadingBlock as heading -> markupHeader output heading
        | :? ParagraphBlock as paragraph -> markupParagraph output paragraph
        | _ -> ()

        output.ToString()


    let parseMarkdown (text : string) = 
        Markdown.Parse text 
        |> Seq.map renderMarkdownBlock
        |> Seq.toArray

