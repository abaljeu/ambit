module Gambol.Shared.CommandDockLayout

open Gambol.Shared.CommandEntry

type DockTrigger =
    | OpenMove
    | OpenSelect
    | OpenMore

type DockSlot =
    | DockCommand of CommandId
    | DockTrigger of DockTrigger
    | DockClose

/// Base strip: undo, redo, zoom, move/select triggers, find, delete, more.
let baseStripSlots : DockSlot list =
    [ DockCommand Undo
      DockCommand Redo
      DockCommand ZoomOut
      DockCommand ZoomIn
      DockTrigger OpenMove
      DockTrigger OpenSelect
      DockCommand Find
      DockCommand Delete
      DockTrigger OpenMore ]

let moveToolsSlots : DockSlot list =
    [ DockCommand MoveUp
      DockCommand MoveDown
      DockCommand Outdent
      DockCommand Indent
      DockCommand MoveSelectionToStart
      DockCommand MoveSelectionToEnd
      DockCommand MoveSelected ]

let selectToolsSlots : DockSlot list =
    [ DockCommand SelectionUp
      DockCommand SelectionDown
      DockCommand SelectToStart
      DockCommand SelectToEnd ]

let moreToolsSlots : DockSlot list =
    [ DockCommand CommandPalette
      DockCommand CopyContent
      DockCommand DuplicateLink
      DockCommand EditClasses
      DockCommand JumpToTarget ]

let commandIds (slots: DockSlot list) : CommandId list =
    slots
    |> List.choose (function
        | DockCommand id -> Some id
        | _ -> None)
