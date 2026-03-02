namespace Gambol.Client

open Gambol.Shared

type Mode =
    | Selecting
    | Editing of originalText: string * cursorPos: int option
    // cursorPos: None = place cursor at end; Some n = place cursor at position n

type Model =
    { graph: Graph
      revision: Revision
      selectedNodes: NodeRange option
      mode: Mode }

type Msg =
    | StateLoaded of Graph * Revision
    | SelectRow of NodeId
    | MoveSelectionUp
    | MoveSelectionDown
    | ChangeSelectionUp
    | ExtendSelectionDown
    | StartEdit of prefill: string
    | SplitNode of currentText: string * cursorPos: int
    | CancelEdit
    | SubmitResponse of Revision
