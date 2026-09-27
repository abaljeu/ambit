namespace Gambol.Server

open Gambol.Shared

type LoadSaveCommandRouter =
    { resolvePath:
        State -> LoadSaveCommandRequest -> Result<LoadSavePath, string>
      startCommand:
        LoadSavePath ->
        LoadSaveCommandRequest ->
        Async<Result<unit, string>> }

[<RequireQualifiedAccess>]
module LoadSaveRouting =

    let resolvePath
        (dataDir: string)
        (state: State)
        (request: LoadSaveCommandRequest)
        : Result<LoadSavePath, string> =
        PathPick.resolve request.prePick (fun () ->
            match
                DocumentPersistPath.workspaceRootFor
                    dataDir
                    state.graph
                    request.start.focusId
            with
            | None -> Ok false
            | Some workspaceRoot ->
                WorkspaceGit.remoteExists workspaceRoot)
