module Gambol.Shared.CommandIconLookup

open Gambol.Shared.CommandDockLayout
open Gambol.Shared.CommandEntry

let iconForCommand (id: CommandId) : string option =
    commandFor id |> Option.bind (fun e -> e.iconId)

let iconForTrigger = function
    | OpenMove -> "amb-icon-move-tools"
    | OpenSelect -> "amb-icon-select-tools"
    | OpenMore -> "amb-icon-more"

let dockCommandIds (slots: DockSlot list) : CommandId list = commandIds slots

let dockCommandIconIds (slots: DockSlot list) : string list =
    slots
    |> List.choose (function
        | DockCommand id -> iconForCommand id
        | _ -> None)
