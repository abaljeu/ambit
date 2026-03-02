namespace Gambol.Client

open Gambol.Shared

type Mode =
    | Selecting
    | Editing of originalText: string * cursorPos: int option
    // cursorPos: None = place cursor at end; Some n = place cursor at position n

/// A NodeRange with a focus index marking the "active" end used for Shift-Arrow and editing.
/// focus is always range.start or range.endd - 1.
type Selection =
    { range: NodeRange
      focus: int }

type Model =
    { graph: Graph
      revision: Revision
      selectedNodes: Selection option
      mode: Mode }

type Msg =
    | StateLoaded of Graph * Revision
    | SelectRow of NodeId
    | MoveSelectionUp
    | MoveSelectionDown
    | ShiftArrowUp
    | ShiftArrowDown
    | StartEdit of prefill: string
    | SplitNode of currentText: string * cursorPos: int
    | CancelEdit
    | SubmitResponse of Revision
