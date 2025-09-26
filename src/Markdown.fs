namespace Seaglass 

open System
open System.Text

open Markdig
open Markdig.Syntax;
open Markdig.Helpers
open Markdig.Syntax.Inlines;
open Serilog
open Spectre.Console
open Spectre.Console.Rendering

open Styles

module Markdown = 

    // String builder helpers
    let private build (sb : StringBuilder) (text : string) = sb.Append(text) |> ignore
    let private (>>=) (sb: StringBuilder) (text : string) = build sb text; sb
    let private (<*>) (sb: StringBuilder) (func : StringBuilder -> unit) = func sb; sb
    let private buildMarkup (sb : StringBuilder) = (sb.ToString().TrimEnd(Environment.NewLine.ToCharArray())) |> Markup
    
    let private getSliceText (slice: StringSlice) = 
        if obj.ReferenceEquals(slice.Text, null) then ""
        else slice.Text.Substring(slice.Start, slice.Length)


    let private getInlineText (elem : LiteralInline) = elem.Content |> getSliceText |> Markup.Escape

    let rec private renderEmphasis (sb: StringBuilder) (elem: EmphasisInline) = 
        Log.Debug($"Emphasis: {elem.DelimiterChar} | {elem.DelimiterCount}")
        let markup = Styles.emphasisMarkup elem.DelimiterChar elem.DelimiterCount
        let markupEnd = if markup <> "" then "[/]" else ""
        sb 
        >>= markup
        <*> renderInlineContainer elem 
        >>= markupEnd
        |> ignore


    // This takes a Container element and adds it's text to the string builder - potentially including 
    // Spectre markup
    and private renderInlineContainer (block : ContainerInline) (output : StringBuilder) =
        for elem in block do 
            Log.Debug($"Markdown ContainerInline {elem.GetType().Name}")

            match elem with 
            | :? LiteralInline as literal -> build output (getInlineText literal)
            | :? LineBreakInline -> build output (Environment.NewLine)
            | :? EmphasisInline as emph -> renderEmphasis output emph
            | _ ->  raise (Exception($"Unknown element {elem.GetType()}"))


    let private markupHeader (header : HeadingBlock) = 
        StringBuilder() 
        >>= markdownMarkup Header
        >>= (String('#', header.Level))
        >>= " "
        <*> renderInlineContainer header.Inline
        >>= "[/]"
        |> buildMarkup


    let private markupParagraph (paragraph : ParagraphBlock) = 
        StringBuilder() 
        <*> renderInlineContainer paragraph.Inline
        |> buildMarkup

    let getBullet defaultBullet (row : Block): IRenderable = 
        let row = row :?> ListItemBlock
        let text = if row.SourceBullet.IsEmpty then defaultBullet
                   else getSliceText row.SourceBullet + "."
        Markup(text)


    // We want to avoid line breaks at the end of blocks - because we handle those


    let rec private markupList (list : ListBlock) = 
        let grid = Grid().AddColumn().AddColumn()
        let defaultBullet = list.BulletType.ToString()
        
        for row in list do 
            let row = row :?> ListItemBlock
            let bulletText = if row.SourceBullet.IsEmpty then defaultBullet
                             else getSliceText row.SourceBullet + "."
            let bullet = Markup(bulletText) :> IRenderable
            grid.AddRow([|bullet; renderBlockElement row|]) |> ignore

        grid


    and private markupContainerBlock (block: ContainerBlock) = Seq.map renderBlockElement block |> Rows


    // Handle top level blocks
    and private renderBlockElement (block : Block) : IRenderable = 
        Log.Debug($"Markdown Block: {block.GetType().Name}")

        match block with 
        | :? HeadingBlock as heading -> markupHeader heading
        | :? ParagraphBlock as paragraph -> markupParagraph paragraph
        | :? ListBlock as list -> markupList list
        | :? ListItemBlock as block -> markupContainerBlock block
        | :? ContainerBlock as block -> markupContainerBlock block
        | :? ThematicBreakBlock -> Rule()
        | _ -> Markup($"Unknown Block: {block.GetType()}")



    let parseMarkdown (text : string): IRenderable array = 
        let pipeline = MarkdownPipelineBuilder().EnableTrackTrivia().UseEmphasisExtras().Build()
        Markdown.Parse(text, pipeline)
        |> Seq.map renderBlockElement
        |> Seq.toArray

