module Gambol.Shared.CommandDockLayout

open Gambol.Shared.CommandEntry
open Gambol.Shared.CommandCategory

type DockTriggerEntry = {
    category: CommandCategory
    name: string
    iconId: string
    slots: DockSlot list
}

and DockSlot =
    | DockCommand of CommandId
    | DockTrigger of DockTriggerEntry

let triggerDockCssClass (trigger: DockTriggerEntry) : string =
    dockCssClass trigger.category

let moveToolsTrigger : DockTriggerEntry =
    { category = MoveStructure
      name = "Move tools"
      iconId = "amb-icon-move-tools"
      slots =
        [ DockCommand Outdent
          DockCommand Indent
          DockCommand MoveUp
          DockCommand MoveDown
          DockCommand MoveSelectionToStart
          DockCommand MoveSelectionToEnd
          DockCommand MoveSelected ] }

let selectToolsTrigger : DockTriggerEntry =
    { category = Selection
      name = "Select tools"
      iconId = "amb-icon-select-tools"
      slots =
        [ DockCommand SelectionUp
          DockCommand SelectionDown
          DockCommand SelectToStart
          DockCommand SelectToEnd ] }

let moreToolsTrigger : DockTriggerEntry =
    { category = More
      name = "More commands"
      iconId = "amb-icon-more"
      slots =
        [ DockCommand Undo
          DockCommand Redo
          DockCommand Rename
          DockCommand Exec
          DockCommand CopyContent
          DockCommand JumpToTarget ] }

let allDockTriggers = [ moveToolsTrigger; selectToolsTrigger; moreToolsTrigger ]

let triggerFor (category: CommandCategory) : DockTriggerEntry option =
    allDockTriggers |> List.tryFind (fun t -> t.category = category)

/// Backward-compat aliases for tests and callers.
let moveToolsSlots = moveToolsTrigger.slots
let selectToolsSlots = selectToolsTrigger.slots
let moreToolsSlots = moreToolsTrigger.slots

let baseStripSlots : DockSlot list =
    [ DockCommand CommandPalette
      DockTrigger moveToolsTrigger
      DockTrigger selectToolsTrigger
      DockCommand Find
      DockCommand Delete
      DockCommand DupNodes
      DockCommand EditClasses
      DockTrigger moreToolsTrigger ]

let commandIds (slots: DockSlot list) : CommandId list =
    slots
    |> List.choose (function
        | DockCommand id -> Some id
        | _ -> None)
