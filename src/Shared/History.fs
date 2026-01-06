namespace Gambol.Shared

[<RequireQualifiedAccess>]
type Op =
    | NewNode of nodeId: NodeId * text: string
    | SetText of nodeId: NodeId * oldText: string * newText: string
    | Replace of
        parentId: NodeId *
        index: int *
        oldIds: NodeId list *
        newIds: NodeId list


type Change =
    { id: int
      ops: Op list }


type History =
    { past: Change list
      future: Change list
      nextId: int }


[<RequireQualifiedAccess>]
module Change =
    let addOp (op: Op) (change: Change) : Change =
        { change with ops = change.ops @ [ op ] }


[<RequireQualifiedAccess>]
module History =
    let empty: History =
        { past = []
          future = []
          nextId = 0 }

    let newChange (history: History) : Change =
        { id = history.nextId
          ops = [] }

    let addChange (change: Change) (history: History) : History =
        let nextId = max history.nextId (change.id + 1)

        { history with
              past = change :: history.past
              future = []
              nextId = nextId }


type State =
    { graph: Graph
      history: History }
