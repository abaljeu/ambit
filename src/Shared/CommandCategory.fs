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
    | More

let dockCssClass = function
    | MoveStructure -> "amb-dock-move"
    | Selection -> "amb-dock-select"
    | FileIO -> "amb-dock-file"
    | More -> "amb-dock-more"
    | _ -> "amb-dock-base"

/// Dock accent for search-dialog title bars (matches invoked command category).
let searchDialogDockCssClass (invokedCommand: string) : string =
    match invokedCommand with
    | "Move Selected" -> dockCssClass MoveStructure
    | "Insert…" -> dockCssClass FileIO
    | _ -> dockCssClass Primary
