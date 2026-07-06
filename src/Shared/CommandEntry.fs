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
    | ZoomOwner
    | Undo
    | Redo
    | CopyContent
    | CopyAsLinks
    | DupNodes
    | CommandPalette
    | MoveSelected
    | Find
    | EditClasses
    | JumpToTarget
    | Import
    | Export
    | Save
    | InsertFile
    | Rename
    | Exec

type CommandEntry = {
    id: CommandId
    name: string
    keys: string list
    keyScope: CommandKeyScope
    cat: CommandCategory
    iconId: string option
}

let allCommands : CommandEntry list =
    [
        { id = EditNode; name = "Edit node"
          keys = [ "Enter" ]; keyScope = SelectionOnly
          cat = EditText; iconId = None }
        { id = SplitAtCursor; name = "Split at cursor"
          keys = [ "Enter" ]; keyScope = EditingOnly
          cat = EditText; iconId = None }
        { id = Delete; name = "Delete"
          keys = [ "Delete"; "Backspace" ]; keyScope = SelectionOnly
          cat = EditText; iconId = Some "amb-icon-delete" }
        { id = JoinWithPrevious; name = "Join with previous"
          keys = [ "Backspace" ]; keyScope = EditingOnly
          cat = EditText; iconId = None }
        { id = JoinWithNext; name = "Join with next"
          keys = [ "Delete" ]; keyScope = EditingOnly
          cat = EditText; iconId = None }
        { id = CursorUp; name = "Cursor up"
          keys = [ "ArrowUp"; "," ]; keyScope = SelectionOnly
          cat = Navigate; iconId = None }
        { id = CursorDown; name = "Cursor down"
          keys = [ "ArrowDown"; "o" ]; keyScope = SelectionOnly
          cat = Navigate; iconId = None }
        { id = CursorFoldLeft; name = "Cursor fold left"
          keys = [ "Shift+ArrowLeft"; "A" ]; keyScope = SelectionOnly
          cat = Navigate; iconId = None }
        { id = CursorLeftToParent; name = "Cursor left to parent"
          keys = [ "ArrowLeft"; "a" ]; keyScope = SelectionOnly
          cat = Navigate; iconId = None }
        { id = CursorUnfoldRight; name = "Cursor unfold right"
          keys = [ "Shift+ArrowRight"; "ArrowRight"; "e"; "E" ]
          keyScope = SelectionOnly; cat = Navigate; iconId = None }
          // if at start of edit area:
        { id = MoveToPreviousNode; name = "Move to previous node"
          keys = [ "ArrowLeft" ]; keyScope = EditingOnly
          cat = EditText; iconId = None }
          // if at end of edit area:
        { id = MoveToNextNode; name = "Move to next node"
          keys = [ "ArrowRight" ]; keyScope = EditingOnly
          cat = EditText; iconId = None }

        { id = SelectionUp; name = "Selection up"
          keys = [ "Shift+ArrowUp"; "<" ]; keyScope = SelectionOrEditing
          cat = Selection; iconId = Some "amb-icon-sel-up" }
        { id = SelectionDown; name = "Selection down"
          keys = [ "Shift+ArrowDown"; "O" ]; keyScope = SelectionOrEditing
          cat = Selection; iconId = Some "amb-icon-sel-down" }
        { id = EditCursorUp; name = "Edit cursor up"
          keys = [ "ArrowUp" ]; keyScope = EditingOnly
          cat = EditText; iconId = None }
        { id = EditCursorDown; name = "Edit cursor down"
          keys = [ "ArrowDown" ]; keyScope = EditingOnly
          cat = EditText; iconId = None }
        { id = MoveUp; name = "Move Up"
          keys = [ "Alt+ArrowUp"; "Ctrl+ArrowUp" ]; keyScope = SelectionOrEditing
          cat = MoveStructure; iconId = Some "amb-icon-move-up" }
        { id = MoveDown; name = "Move Down"
          keys = [ "Alt+ArrowDown"; "Ctrl+ArrowDown" ]; keyScope = SelectionOrEditing
          cat = MoveStructure; iconId = Some "amb-icon-move-down" }
        { id = CursorToStart; name = "Cursor to Start"
          keys = [ "PageUp" ]; keyScope = SelectionOnly
          cat = Navigate; iconId = None }
        { id = CursorToEnd; name = "Cursor to End"
          keys = [ "PageDown" ]; keyScope = SelectionOnly
          cat = Navigate; iconId = None }
        { id = MoveSelectionToStart; name = "Move Selection to Start"
          keys = [ "Alt+PageUp" ]; keyScope = SelectionOrEditing
          cat = MoveStructure; iconId = Some "amb-icon-move-to-start" }
        { id = MoveSelectionToEnd; name = "Move Selection to End"
          keys = [ "Alt+PageDown" ]; keyScope = SelectionOrEditing
          cat = MoveStructure; iconId = Some "amb-icon-move-to-end" }
        { id = SelectToStart; name = "Select to Start"
          keys = [ "Shift+PageUp" ]; keyScope = SelectionOnly
          cat = Selection; iconId = Some "amb-icon-sel-to-start" }
        { id = SelectToEnd; name = "Select to End"
          keys = [ "Shift+PageDown" ]; keyScope = SelectionOnly
          cat = Selection; iconId = Some "amb-icon-sel-to-end" }
        { id = CursorToTopOfView; name = "Cursor to Top of View"
          keys = [ "Home" ]; keyScope = SelectionOnly
          cat = Navigate; iconId = None }
        { id = CursorToEndOfView; name = "Cursor to End of View"
          keys = [ "End" ]; keyScope = SelectionOnly
          cat = Navigate; iconId = None }
        { id = MoveSelectionToTopOfView; name = "Move Selection to Top of View"
          keys = [ "Alt+Home" ]; keyScope = SelectionOrEditing
          cat = MoveStructure; iconId = None }
        { id = MoveSelectionToEndOfView; name = "Move Selection to End of View"
          keys = [ "Alt+End" ]; keyScope = SelectionOrEditing
          cat = MoveStructure; iconId = None }
        { id = Indent; name = "Indent"
          keys = [ "Tab" ]; keyScope = SelectionOrEditing
          cat = MoveStructure; iconId = Some "amb-icon-move-right" }
        { id = Outdent; name = "Outdent"
          keys = [ "Shift+Tab" ]; keyScope = SelectionOrEditing
          cat = MoveStructure; iconId = Some "amb-icon-move-left" }
        { id = Escape; name = "Escape"
          keys = [ "Escape" ]; keyScope = SelectionOrEditing
          cat = Navigate; iconId = None }
        { id = FoldUnfold; name = "Fold / unfold"
          keys = [ "Ctrl+." ]; keyScope = SelectionOrEditing
          cat = Navigate; iconId = None }
        { id = ZoomIn; name = "Zoom in"
          keys = [ "Ctrl+]"; "]" ]; keyScope = SelectionOrEditing
          cat = Navigate; iconId = Some "amb-icon-zoom-in" }
        { id = ZoomOut; name = "Zoom out"
          keys = [ "Ctrl+["; "[" ]; keyScope = SelectionOrEditing
          cat = Navigate; iconId = Some "amb-icon-zoom-out" }
        { id = ZoomOwner; name = "Zoom owner"
          keys = [ "Alt+[" ]; keyScope = SelectionOrEditing
          cat = Navigate; iconId = Some "amb-icon-zoom-out" }
        { id = Undo; name = "Undo"
          keys = [ "Ctrl+Z"; "z" ]; keyScope = SelectionOrEditing
          cat = Primary; iconId = Some "amb-icon-undo" }
        { id = Redo; name = "Redo"
          keys = [ "Ctrl+Y"; "y" ]; keyScope = SelectionOrEditing
          cat = Primary; iconId = Some "amb-icon-redo" }
        { id = CopyContent; name = "Copy content"
          keys = [ "c" ]; keyScope = SelectionOnly
          cat = Clipboard; iconId = Some "amb-icon-copy" }
        { id = CopyAsLinks; name = "Copy as links"
          keys = [ "Ctrl+C"; "C" ]; keyScope = SelectionOnly
          cat = Clipboard; iconId = None }
        { id = DupNodes; name = "Duplicate (link)"
          keys = [ "D" ]; keyScope = SelectionOnly
          cat = Clipboard; iconId = Some "amb-icon-duplicate" }
        { id = CommandPalette; name = "Command palette"
          keys = [ "Ctrl+P"; "p" ]; keyScope = SelectionOrEditing
          cat = Primary; iconId = Some "amb-icon-palette" }
        { id = MoveSelected; name = "Move Selected"
          keys = [ "m"; "Ctrl+m" ]; keyScope = SelectionOrEditing
          cat = MoveStructure; iconId = Some "amb-icon-move-selected" }
        { id = Find; name = "Find"
          keys = [ "/"; "Ctrl+f" ]; keyScope = SelectionOrEditing
          cat = Primary; iconId = Some "amb-icon-find" }
        { id = EditClasses; name = "Edit classes"
          keys = [ "."; "Alt+." ]; keyScope = SelectionOrEditing
          cat = Format; iconId = Some "amb-icon-edit-classes" }
        { id = JumpToTarget; name = "Jump to Target"
          keys = [ "Alt+j"; "j" ]; keyScope = SelectionOrEditing
          cat = Navigate; iconId = Some "amb-icon-jump" }
        { id = Import; name = "Import"
          keys = [ "Ctrl+Shift+>" ]; keyScope = SelectionOrEditing
          cat = FileIO; iconId = None }
        { id = Export; name = "Export"
          keys = [ "Ctrl+Shift+<" ]; keyScope = SelectionOrEditing
          cat = FileIO; iconId = None }
        { id = Save; name = "Save"
          keys = [ "Ctrl+S" ]; keyScope = SelectionOrEditing
          cat = FileIO; iconId = None }
        { id = InsertFile; name = "Insert…"
          keys = [ "f" ]; keyScope = SelectionOrEditing
          cat = FileIO; iconId = None }
        { id = Rename; name = "Rename"
          keys = [ "F2" ]; keyScope = SelectionOnly
          cat = EditText; iconId = None }
        { id = Exec; name = "Run"
          keys = [ "Ctrl+Enter" ]; keyScope = SelectionOrEditing
          cat = Primary; iconId = None }
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
