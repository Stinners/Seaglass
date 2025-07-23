namespace Seaglass 

open Spectre.Console

module Styles = 

    let DefaultBorder = BoxBorder.Square
    let FocusBorder = BoxBorder.Heavy

    let border isFocused = if isFocused then FocusBorder else DefaultBorder
