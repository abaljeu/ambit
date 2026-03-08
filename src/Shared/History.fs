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
      history: History
      revision: Revision }


[<RequireQualifiedAccess>]
type ApplyResult =
    | Changed of State
    | Unchanged of State
    | Invalid of State * string


[<RequireQualifiedAccess>]
module Op =
    let private fromGraphResult (state: State) (result: Result<Graph, string>) : ApplyResult =
        match result with
        | Ok graph -> ApplyResult.Changed { state with graph = graph }
        | Error msg -> ApplyResult.Invalid(state, msg)

    let private fromGraphResultUnchanged (state: State) (result: Result<Graph, string>) : ApplyResult =
        match result with
        | Ok graph -> ApplyResult.Changed { state with graph = graph }
        | Error msg -> ApplyResult.Invalid(state, msg)

    let apply (op: Op) (state: State) : ApplyResult =
        match op with
        | Op.NewNode(nodeId, text) ->
            let node: Node =
                { id = nodeId
                  text = text
                  name = None
                  children = [] }

            ApplyResult.Changed
                { state with
                      graph =
                          { state.graph with
                                nodes = state.graph.nodes |> Map.add nodeId node } }
        | Op.SetText(nodeId, oldText, newText) ->
            Graph.setText nodeId oldText newText state.graph
            |> fromGraphResult state
        | Op.Replace(parentId, index, oldIds, newIds) ->
            Graph.replace parentId index oldIds newIds state.graph
            |> fromGraphResult state

    let undo (op: Op) (state: State) : ApplyResult =
        match op with
        | Op.NewNode(nodeId, _) ->
            let nodes = state.graph.nodes |> Map.remove nodeId
            ApplyResult.Changed { state with graph = { state.graph with nodes = nodes } }
        | Op.SetText(nodeId, oldText, newText) ->
            // Inverse: ensure current text == newText, set back to oldText
            Graph.setText nodeId newText oldText state.graph
            |> fromGraphResult state
        | Op.Replace(parentId, index, oldIds, newIds) ->
            // Inverse: swap old/new to restore
            Graph.replace parentId index newIds oldIds state.graph
            |> fromGraphResult state


[<RequireQualifiedAccess>]
module Change =
    let addOp (op: Op) (change: Change) : Change =
        { change with ops = change.ops @ [ op ] }

    /// Construct the inverse of a change: reversed op list, each op with old/new swapped.
    /// Change.undo(invert c) re-applies c's effect (valid for SetText and Replace).
    /// NewNode has no DeleteNode counterpart, so its inversion is imperfect; undo-of-undo
    /// for splits will return ApplyResult.Invalid and leave state unchanged.
    let invert (change: Change) : Change =
        let invertOp op =
            match op with
            | Op.NewNode(id, text)              -> Op.NewNode(id, text)
            | Op.SetText(id, old, new_)          -> Op.SetText(id, new_, old)
            | Op.Replace(pid, i, olds, news)     -> Op.Replace(pid, i, news, olds)
        { change with ops = change.ops |> List.rev |> List.map invertOp }

    let apply (change: Change) (state: State) : ApplyResult =
        let step (accState, hasChanged) op =
            match Op.apply op accState with
            | ApplyResult.Invalid _ as err -> Error err
            | ApplyResult.Unchanged s' -> Ok(s', hasChanged)
            | ApplyResult.Changed s' -> Ok(s', true)

        let result =
            change.ops
            |> List.fold
                (fun acc op ->
                    match acc with
                    | Error err -> Error err
                    | Ok (s, changed) -> step (s, changed) op)
                (Ok(state, false))

        match result with
        | Error err -> err
        | Ok (s, false) -> ApplyResult.Unchanged s
        | Ok (s, true) -> ApplyResult.Changed s

    let undo (change: Change) (state: State) : ApplyResult =
        let step (accState, hasChanged) op =
            match Op.undo op accState with
            | ApplyResult.Invalid _ as err -> Error err
            | ApplyResult.Unchanged s' -> Ok(s', hasChanged)
            | ApplyResult.Changed s' -> Ok(s', true)

        let result =
            change.ops
            |> List.rev
            |> List.fold
                (fun acc op ->
                    match acc with
                    | Error err -> Error err
                    | Ok (s, changed) -> step (s, changed) op)
                (Ok(state, false))

        match result with
        | Error err -> err
        | Ok (s, false) -> ApplyResult.Unchanged s
        | Ok (s, true) -> ApplyResult.Changed s



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
        // Emacs stack model: instead of discarding the future on a new change, fold it back
        // into past as inverse changes. Subsequent undos will re-apply those inverses,
        // giving "undo the undo" (redo-via-undo) without a separate redo stack clearing.
        let requeued = history.future |> List.map Change.invert
        { history with
              past = change :: requeued @ history.past
              future = []
              nextId = nextId }

    let applyChange (change: Change) (state: State) : ApplyResult =
        match Change.apply change state with
        | ApplyResult.Invalid _ as err -> err
        | ApplyResult.Unchanged s -> ApplyResult.Unchanged s
        | ApplyResult.Changed s ->
            let history' = addChange change s.history
            ApplyResult.Changed { s with history = history' }

    let undo (state: State) : ApplyResult =
        match state.history.past with
        | [] -> ApplyResult.Unchanged state
        | change :: restPast ->
            match Change.undo change state with
            | ApplyResult.Invalid _ as err -> err
            | ApplyResult.Unchanged s -> ApplyResult.Unchanged s
            | ApplyResult.Changed s ->
                let history' =
                    { s.history with
                        past = restPast
                        future = change :: s.history.future }

                ApplyResult.Changed { s with history = history' }

    let redo (state: State) : ApplyResult =
        match state.history.future with
        | [] -> ApplyResult.Unchanged state
        | change :: restFuture ->
            match Change.apply change state with
            | ApplyResult.Invalid _ as err -> err
            | ApplyResult.Unchanged s -> ApplyResult.Unchanged s
            | ApplyResult.Changed s ->
                let history' =
                    { s.history with
                        past = change :: s.history.past
                        future = restFuture }

                ApplyResult.Changed { s with history = history' }

