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


type State =
    { graph: Graph
      history: History }


[<RequireQualifiedAccess>]
module Change =
    let addOp (op: Op) (change: Change) : Change =
        { change with ops = change.ops @ [ op ] }


[<RequireQualifiedAccess>]
module Op =
    let apply (op: Op) (state: State) : State =
        match op with
        | Op.NewNode(nodeId, text) ->
            let node: Node =
                { id = nodeId
                  text = text
                  children = [] }

            let nodes = state.graph.nodes |> Map.add nodeId node

            { state with
                  graph = { state.graph with nodes = nodes } }
        | Op.SetText(nodeId, oldText, newText) ->
            match Graph.setText nodeId oldText newText state.graph with
            | Ok graph -> { state with graph = graph }
            | Error _ -> state
        | Op.Replace(parentId, index, oldIds, newIds) ->
            match Graph.replace parentId index oldIds newIds state.graph with
            | Ok graph -> { state with graph = graph }
            | Error _ -> state



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

