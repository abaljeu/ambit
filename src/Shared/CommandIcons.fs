module Gambol.Shared.CommandIcons

open Gambol.Shared.CommandDockLayout

/// SVG symbol ids in src/Server/wwwroot/command-dock.svg
/// Lucide source names: doc/reference/command-icon-index.md
let undo = "amb-icon-undo"              // lucide: undo-2
let redo = "amb-icon-redo"              // lucide: redo-2
let zoomOut = "amb-icon-zoom-out"       // lucide: zoom-out
let zoomIn = "amb-icon-zoom-in"         // lucide: zoom-in
let moveTools = "amb-icon-move-tools"   // custom: solid rect + 4 chevrons
let selectTools = "amb-icon-select-tools" // custom: hollow rect + 4 chevrons
let find = "amb-icon-find"              // lucide: search
let jump = "amb-icon-jump"              // lucide: link-external
let more = "amb-icon-more"              // lucide: ellipsis
let close = "amb-icon-close"            // lucide: x
let selUp = "amb-icon-sel-up"           // custom: hollow block + up chevrons
let selDown = "amb-icon-sel-down"       // custom: hollow block + down chevrons
let selLeft = "amb-icon-sel-left"       // custom: hollow block + left chevrons
let selRight = "amb-icon-sel-right"     // custom: hollow block + right chevrons
let moveUp = "amb-icon-move-up"         // custom: solid block + up chevrons
let moveDown = "amb-icon-move-down"     // custom: solid block + down chevrons
let moveLeft = "amb-icon-move-left"     // custom: solid block + left chevrons
let moveRight = "amb-icon-move-right"   // custom: solid block + right chevrons
let moveToStart = "amb-icon-move-to-start" // custom: level start bar + move block
let moveToEnd = "amb-icon-move-to-end"   // custom: level end bar + move block
let selToStart = "amb-icon-sel-to-start" // custom: level start bar + hollow block
let selToEnd = "amb-icon-sel-to-end"     // custom: level end bar + hollow block
let palette = "amb-icon-palette"        // lucide: command
let copy = "amb-icon-copy"              // lucide: copy
let duplicate = "amb-icon-duplicate"    // lucide: copy-plus
let editClasses = "amb-icon-edit-classes" // lucide: tags
let moveSelected = "amb-icon-move-selected" // lucide: move
let spritePath = "/ambit/command-dock.svg"

let iconForCommand (name: string) : string option =
    match name with
    | "Undo" -> Some undo
    | "Redo" -> Some redo
    | "Zoom out" -> Some zoomOut
    | "Zoom in" -> Some zoomIn
    | "Find" -> Some find
    | "Jump to Target" -> Some jump
    | "Move Up" -> Some moveUp
    | "Move Down" -> Some moveDown
    | "Selection up" | "Cursor up" -> Some selUp
    | "Selection down" | "Cursor down" -> Some selDown
    | "Selection left" | "Cursor left to parent" -> Some selLeft
    | "Selection right" | "Cursor unfold right" -> Some selRight
    | "Indent" -> Some moveRight
    | "Outdent" -> Some moveLeft
    | "Move Selection to Start" -> Some moveToStart
    | "Move Selection to End" -> Some moveToEnd
    | "Select to Start" -> Some selToStart
    | "Select to End" -> Some selToEnd
    | "Move Selected" -> Some moveSelected
    | "Command palette" -> Some palette
    | "Copy content" -> Some copy
    | "Duplicate (link)" -> Some duplicate
    | "Edit classes" -> Some editClasses
    | _ -> None

let iconForTrigger = function
    | OpenMove -> moveTools
    | OpenSelect -> selectTools
    | OpenMore -> more

let dockCommandNames (slots: DockSlot list) : string list = commandNames slots

let dockCommandIconIds (slots: DockSlot list) : string list =
    slots
    |> List.choose (function
        | DockCommand name -> iconForCommand name
        | _ -> None)
