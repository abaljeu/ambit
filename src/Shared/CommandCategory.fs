module Gambol.Shared.CommandCategory

type CommandCategory =
    | Primary
    | Navigate
    | EditText
    | MoveStructure
    | Selection
    | Clipboard
    | Format
    | FileIO

let dockCssClass = function
    | MoveStructure -> "amb-dock-move"
    | Selection -> "amb-dock-select"
    | _ -> "amb-dock-base"
