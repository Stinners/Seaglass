namespace Seaglass 

open Spectre.Console

module Styles = 

    let DefaultBorder = BoxBorder.Square
    let FocusBorder = BoxBorder.Heavy

    let border isFocused = if isFocused then FocusBorder else DefaultBorder

    let MaxTextWidth = 80 
    let TextPadding = 2

    type MarkdownNode = 
        | Header 
        | Paragraph 

    let markdownMarkup nodeType = 
        match nodeType with
        | Header -> "[bold green]"
        | Paragraph -> ""


    // These are based on Obsidian's rules
    // Markdig supports super/subscripts which don't have a reliable way to support 
    // in a terminal so we ignore them 
    // It also supports 'inserts' which we're just treaking as highlights for now
    let emphasisMarkup delimiterChar delimiterCount = 
        match (delimiterChar, delimiterCount) with 
        | ('*', 1) | ('_', 1) -> "[italic]"
        | ('*', 2) | ('_', 2) -> "[yellow]"
        | ('*', 3) | ('_', 3) -> "[yellow italic]"
        | ('~', 2) -> "[strikethrough]"
        | ('=', 2) -> "[default on blue3]"
        | ('^', 1) -> ""  // Superscript
        | ('~', 1) -> ""  // Subscript
        | ('+', 2) -> "[default on blue3]"  // Insert
        | _ -> "[red]Invalid Markup characters"
