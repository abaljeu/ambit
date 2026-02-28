namespace Gambol.Client

open Gambol.Shared

type Mode =
    | Selection
    | Editing of originalText: string

type Model =
    { graph: Graph
      revision: Revision
      selectedNode: NodeId option
      mode: Mode }

type Msg =
    | StateLoaded of Graph * Revision
    | SelectRow of NodeId
    | StartEdit of prefill: string
    | CommitEdit of newText: string
    | InsertSibling
    | CancelEdit
    | SubmitResponse of Revision
