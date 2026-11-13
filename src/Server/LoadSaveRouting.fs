namespace Gambol.Server

open Gambol.Shared

type LoadSaveCommandRouter =
    { resolvePath:
        GraphAtEvent -> LoadSaveCommandRequest -> Result<LoadSavePath, string>
      startCommand:
        LoadSavePath ->
        LoadSaveCommandRequest ->
        Async<Result<unit, string>> }

[<RequireQualifiedAccess>]
module LoadSaveRouting =

    /// A directory with no `.git` is not a git workspace.
    let private remoteAtRoot (root: string) =
        if not (WorkspaceGit.isRepo root) then
            Ok false
        else
            WorkspaceGit.remoteExists root

    /// Plain Save asks this. ROOT uses the data directory.
    let private plainSaveRemote
        (dataDir: string)
        (graph: Graph)
        (focusId: NodeId)
        ()
        =
        match DocumentPersistPath.repositoryRoot dataDir graph focusId with
        | None -> Ok false
        | Some root -> remoteAtRoot root

    let resolvePath
        (dataDir: string)
        (state: GraphAtEvent)
        (request: LoadSaveCommandRequest)
        : Result<LoadSavePath, string> =
        let choice =
            LoadPathChoice.ofRequest request state.graph
        PathPick.resolveCommand
            choice
            (plainSaveRemote dataDir state.graph request.start.focusId)
