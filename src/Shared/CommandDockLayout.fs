module Gambol.Shared.CommandDockLayout

type CommandDockSurface =
    | Base
    | MoveTools
    | SelectTools
    | MoreTools
    | PaletteOnly
    | NoButton

type DockTrigger =
    | OpenMove
    | OpenSelect
    | OpenMore

type DockSlot =
    | DockCommand of string
    | DockTrigger of DockTrigger
    | DockClose

/// Base strip: undo, redo, zoom, move/select triggers, find, jump, more.
let baseStripSlots : DockSlot list =
    [ DockCommand "Undo"
      DockCommand "Redo"
      DockCommand "Zoom out"
      DockCommand "Zoom in"
      DockTrigger OpenMove
      DockTrigger OpenSelect
      DockCommand "Find"
      DockCommand "Jump to Target"
      DockTrigger OpenMore ]

let moveToolsSlots : DockSlot list =
    [ DockClose
      DockCommand "Move Up"
      DockCommand "Move Down"
      DockCommand "Indent"
      DockCommand "Outdent"
      DockCommand "Move Selection to Start"
      DockCommand "Move Selection to End"
      DockCommand "Move Selected" ]

let selectToolsSlots : DockSlot list =
    [ DockClose
      DockCommand "Selection up"
      DockCommand "Selection down"
      DockCommand "Select to Start"
      DockCommand "Select to End" ]

let moreToolsSlots : DockSlot list =
    [ DockClose
      DockCommand "Command palette"
      DockCommand "Copy content"
      DockCommand "Duplicate (link)"
      DockCommand "Edit classes" ]

let maxBaseSlots = 9
let maxMoveSlots = 8
let maxSelectSlots = 5

let commandNames (slots: DockSlot list) : string list =
    slots
    |> List.choose (function
        | DockCommand n -> Some n
        | _ -> None)
