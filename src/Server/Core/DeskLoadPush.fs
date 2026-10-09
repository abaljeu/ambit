namespace Gambol.Server

open Gambol.Shared

/// Desk Load asks mailbox Load for a Workspace, Directory, or File.
module internal DeskLoadPush =

    let private subjectToLoad
        (request: LoadSaveCommandRequest)
        (path: LoadSavePath)
        (graph: Graph)
        : NodeId option =
        match path, request.operation with
        | LoadSavePath.Desk, LoadSaveOperation.Load ->
            match LoadSubject.ofId graph request.start.focusId with
            | Ok _ -> Some request.start.focusId
            | Error _ -> None
        | _ -> None

    /// Coerce plain Load of a Directory or a File onto Desk, then mailbox-Load when Desk.
    let runLoadSave
        (run: LoadSaveContext)
        (load: NodeId -> Result<unit, string>)
        (request: LoadSaveCommandRequest)
        (path: LoadSavePath)
        =
        let graph = run.getState ()
        let chosen = LoadPathChoice.ofRequest request graph
        let path = PathPick.effectiveLoadPath chosen path
        CoreActorPool.startLoadSaveCommand run path chosen request
        |> Result.bind (fun secret ->
            match subjectToLoad request path graph with
            | None -> Ok secret
            | Some subject ->
                load subject |> Result.map (fun () -> secret))
