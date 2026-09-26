namespace Gambol.Shared

/// Editing commit gate for Browser Run. No DOM.
module RunEditCommit =

    /// SetText against the live node text. Empty when unchanged or the graph root.
    let commitTextOps
        (nodeId: NodeId)
        (newText: string)
        (graph: Graph)
        : Op list
        =
        if nodeId = graph.root then
            []
        else
            match Map.tryFind nodeId graph.nodes with
            | None -> []
            | Some node when node.text = newText -> []
            | Some node -> [ Op.SetText(nodeId, node.text, newText) ]

    /// Failed Editing commit: do not ActorStart or Amble.
    let mayLaunchAfterEditCommit
        (wasEditing: bool)
        (after: CmdLastResult option)
        : bool =
        match wasEditing, after with
        | true, Some (CmdLastResult.Error _) -> false
        | _ -> true

    /// Commit when Editing. Third value is false when that commit failed.
    let commitIfEditingForRun
        (commit: VM -> VM * Effect list)
        (model: VM)
        : VM * Effect list * bool =
        let wasEditing =
            match model.mode with
            | Editing _ -> true
            | _ -> false
        let committed, effects = commit model
        let mayLaunch =
            mayLaunchAfterEditCommit
                wasEditing committed.lastCmdResult
        committed, effects, mayLaunch

    /// Commit, then continue only when that Editing commit did not fail.
    let afterEditCommit
        (commit: VM -> VM * Effect list)
        (model: VM)
        (cont: VM -> Effect list -> VM * Effect list)
        : VM * Effect list =
        let committed, commitEffects, mayLaunch =
            commitIfEditingForRun commit model
        if not mayLaunch then
            committed, commitEffects
        else
            cont committed commitEffects
