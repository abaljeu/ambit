module Gambol.Shared.CommandEntry

open Gambol.Shared.CommandCategory

type CommandKeyScope =
    | SelectionOnly
    | EditingOnly
    | SelectionOrEditing

type CommandId =
    | EditNode
    | SplitAtCursor
    | Delete
    | JoinWithPrevious
    | JoinWithNext
    | CursorUp
    | CursorDown
    | CursorFoldLeft
    | CursorLeftToParent
    | CursorUnfoldRight
    | MoveToPreviousNode
    | MoveToNextNode
    | SelectionUp
    | SelectionDown
    | EditCursorUp
    | EditCursorDown
    | MoveUp
    | MoveDown
    | CursorToStart
    | CursorToEnd
    | MoveSelectionToStart
    | MoveSelectionToEnd
    | SelectToStart
    | SelectToEnd
    | CursorToTopOfView
    | CursorToEndOfView
    | MoveSelectionToTopOfView
    | MoveSelectionToEndOfView
    | Indent
    | Outdent
    | Escape
    | FoldUnfold
    | ZoomIn
    | ZoomOut
    | Undo
    | Redo
    | CopyContent
    | CopyAsLinks
    | DuplicateLink
    | CommandPalette
    | MoveSelected
    | Find
    | EditClasses
    | JumpToTarget
    | Import
    | Export

type CommandEntry = {
    id: CommandId
    name: string
    keys: string list
    keyScope: CommandKeyScope
    category: CommandCategory
    iconId: string option
}

let allCommands : CommandEntry list =
    [
        { id = EditNode; name = "Edit node"
          keys = [ "F2"; "Enter" ]; keyScope = SelectionOnly
          category = EditText; iconId = None }
        { id = SplitAtCursor; name = "Split at cursor"
          keys = [ "Enter" ]; keyScope = EditingOnly
          category = EditText; iconId = None }
        { id = Delete; name = "Delete"
          keys = [ "Delete"; "Backspace" ]; keyScope = SelectionOnly
          category = EditText; iconId = Some "amb-icon-delete" }
        { id = JoinWithPrevious; name = "Join with previous"
          keys = [ "Backspace" ]; keyScope = EditingOnly
          category = EditText; iconId = None }
        { id = JoinWithNext; name = "Join with next"
          keys = [ "Delete" ]; keyScope = EditingOnly
          category = EditText; iconId = None }
        { id = CursorUp; name = "Cursor up"
          keys = [ "ArrowUp"; "," ]; keyScope = SelectionOnly
          category = Navigate; iconId = None }
        { id = CursorDown; name = "Cursor down"
          keys = [ "ArrowDown"; "o" ]; keyScope = SelectionOnly
          category = Navigate; iconId = None }
        { id = CursorFoldLeft; name = "Cursor fold left"
          keys = [ "Shift+ArrowLeft"; "A" ]; keyScope = SelectionOnly
          category = Navigate; iconId = None }
        { id = CursorLeftToParent; name = "Cursor left to parent"
          keys = [ "ArrowLeft"; "a" ]; keyScope = SelectionOnly
          category = Navigate; iconId = None }
        { id = CursorUnfoldRight; name = "Cursor unfold right"
          keys = [ "Shift+ArrowRight"; "ArrowRight"; "e"; "E" ]
          keyScope = SelectionOnly; category = Navigate; iconId = None }
        { id = MoveToPreviousNode; name = "Move to previous node"
          keys = [ "ArrowLeft" ]; keyScope = EditingOnly
          category = EditText; iconId = None }
        { id = MoveToNextNode; name = "Move to next node"
          keys = [ "ArrowRight" ]; keyScope = EditingOnly
          category = EditText; iconId = None }
        { id = SelectionUp; name = "Selection up"
          keys = [ "Shift+ArrowUp"; "<" ]; keyScope = SelectionOrEditing
          category = Selection; iconId = Some "amb-icon-sel-up" }
        { id = SelectionDown; name = "Selection down"
          keys = [ "Shift+ArrowDown"; "O" ]; keyScope = SelectionOrEditing
          category = Selection; iconId = Some "amb-icon-sel-down" }
        { id = EditCursorUp; name = "Edit cursor up"
          keys = [ "ArrowUp" ]; keyScope = EditingOnly
          category = EditText; iconId = None }
        { id = EditCursorDown; name = "Edit cursor down"
          keys = [ "ArrowDown" ]; keyScope = EditingOnly
          category = EditText; iconId = None }
        { id = MoveUp; name = "Move Up"
          keys = [ "Alt+ArrowUp"; "Ctrl+ArrowUp" ]; keyScope = SelectionOrEditing
          category = MoveStructure; iconId = Some "amb-icon-move-up" }
        { id = MoveDown; name = "Move Down"
          keys = [ "Alt+ArrowDown"; "Ctrl+ArrowDown" ]; keyScope = SelectionOrEditing
          category = MoveStructure; iconId = Some "amb-icon-move-down" }
        { id = CursorToStart; name = "Cursor to Start"
          keys = [ "PageUp" ]; keyScope = SelectionOnly
          category = Navigate; iconId = None }
        { id = CursorToEnd; name = "Cursor to End"
          keys = [ "PageDown" ]; keyScope = SelectionOnly
          category = Navigate; iconId = None }
        { id = MoveSelectionToStart; name = "Move Selection to Start"
          keys = [ "Alt+PageUp" ]; keyScope = SelectionOrEditing
          category = MoveStructure; iconId = Some "amb-icon-move-to-start" }
        { id = MoveSelectionToEnd; name = "Move Selection to End"
          keys = [ "Alt+PageDown" ]; keyScope = SelectionOrEditing
          category = MoveStructure; iconId = Some "amb-icon-move-to-end" }
        { id = SelectToStart; name = "Select to Start"
          keys = [ "Shift+PageUp" ]; keyScope = SelectionOnly
          category = Selection; iconId = Some "amb-icon-sel-to-start" }
        { id = SelectToEnd; name = "Select to End"
          keys = [ "Shift+PageDown" ]; keyScope = SelectionOnly
          category = Selection; iconId = Some "amb-icon-sel-to-end" }
        { id = CursorToTopOfView; name = "Cursor to Top of View"
          keys = [ "Home" ]; keyScope = SelectionOnly
          category = Navigate; iconId = None }
        { id = CursorToEndOfView; name = "Cursor to End of View"
          keys = [ "End" ]; keyScope = SelectionOnly
          category = Navigate; iconId = None }
        { id = MoveSelectionToTopOfView; name = "Move Selection to Top of View"
          keys = [ "Alt+Home" ]; keyScope = SelectionOrEditing
          category = MoveStructure; iconId = None }
        { id = MoveSelectionToEndOfView; name = "Move Selection to End of View"
          keys = [ "Alt+End" ]; keyScope = SelectionOrEditing
          category = MoveStructure; iconId = None }
        { id = Indent; name = "Indent"
          keys = [ "Tab" ]; keyScope = SelectionOrEditing
          category = MoveStructure; iconId = Some "amb-icon-move-right" }
        { id = Outdent; name = "Outdent"
          keys = [ "Shift+Tab" ]; keyScope = SelectionOrEditing
          category = MoveStructure; iconId = Some "amb-icon-move-left" }
        { id = Escape; name = "Escape"
          keys = [ "Escape" ]; keyScope = SelectionOrEditing
          category = Navigate; iconId = None }
        { id = FoldUnfold; name = "Fold / unfold"
          keys = [ "Ctrl+." ]; keyScope = SelectionOrEditing
          category = Navigate; iconId = None }
        { id = ZoomIn; name = "Zoom in"
          keys = [ "Ctrl+]"; "]" ]; keyScope = SelectionOrEditing
          category = Navigate; iconId = Some "amb-icon-zoom-in" }
        { id = ZoomOut; name = "Zoom out"
          keys = [ "Ctrl+["; "[" ]; keyScope = SelectionOrEditing
          category = Navigate; iconId = Some "amb-icon-zoom-out" }
        { id = Undo; name = "Undo"
          keys = [ "Ctrl+Z"; "z" ]; keyScope = SelectionOrEditing
          category = Primary; iconId = Some "amb-icon-undo" }
        { id = Redo; name = "Redo"
          keys = [ "Ctrl+Y"; "y" ]; keyScope = SelectionOrEditing
          category = Primary; iconId = Some "amb-icon-redo" }
        { id = CopyContent; name = "Copy content"
          keys = [ "c" ]; keyScope = SelectionOnly
          category = Clipboard; iconId = Some "amb-icon-copy" }
        { id = CopyAsLinks; name = "Copy as links"
          keys = [ "Ctrl+C"; "C" ]; keyScope = SelectionOnly
          category = Clipboard; iconId = None }
        { id = DuplicateLink; name = "Duplicate (link)"
          keys = [ "D" ]; keyScope = SelectionOnly
          category = Clipboard; iconId = Some "amb-icon-duplicate" }
        { id = CommandPalette; name = "Command palette"
          keys = [ "Ctrl+P"; "p" ]; keyScope = SelectionOrEditing
          category = Primary; iconId = Some "amb-icon-palette" }
        { id = MoveSelected; name = "Move Selected"
          keys = [ "m"; "Ctrl+m" ]; keyScope = SelectionOrEditing
          category = MoveStructure; iconId = Some "amb-icon-move-selected" }
        { id = Find; name = "Find"
          keys = [ "/"; "Ctrl+f" ]; keyScope = SelectionOrEditing
          category = Primary; iconId = Some "amb-icon-find" }
        { id = EditClasses; name = "Edit classes"
          keys = [ "Alt+C"; "." ]; keyScope = SelectionOrEditing
          category = Format; iconId = Some "amb-icon-edit-classes" }
        { id = JumpToTarget; name = "Jump to Target"
          keys = [ "Alt+j"; "j" ]; keyScope = SelectionOrEditing
          category = Navigate; iconId = Some "amb-icon-jump" }
        { id = Import; name = "Import"
          keys = [ "Ctrl+Shift+>" ]; keyScope = SelectionOrEditing
          category = FileIO; iconId = None }
        { id = Export; name = "Export"
          keys = [ "Ctrl+Shift+<" ]; keyScope = SelectionOrEditing
          category = FileIO; iconId = None }
    ]

let commandFor (id: CommandId) : CommandEntry option =
    allCommands |> List.tryFind (fun e -> e.id = id)

let displayName (id: CommandId) : string =
    match commandFor id with
    | Some e -> e.name
    | None -> failwith $"unknown CommandId: {id}"

let inKeyScope (sel: bool) (scope: CommandKeyScope) : bool =
    match scope with
    | SelectionOnly -> sel
    | EditingOnly -> not sel
    | SelectionOrEditing -> true

let scopeInSelection (scope: CommandKeyScope) : bool =
    match scope with
    | SelectionOnly | SelectionOrEditing -> true
    | EditingOnly -> false

let scopeInEditing (scope: CommandKeyScope) : bool =
    match scope with
    | EditingOnly | SelectionOrEditing -> true
    | SelectionOnly -> false
