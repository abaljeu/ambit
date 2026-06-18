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
    | FileIO -> "amb-dock-file"
    | _ -> "amb-dock-base"

/// Dock accent for search-dialog title bars (matches invoked command category).
let searchDialogDockCssClass (invokedCommand: string) : string =
    match invokedCommand with
    | "Move Selected" -> dockCssClass MoveStructure
    | "Insert File" -> dockCssClass FileIO
    | _ -> dockCssClass Primary
