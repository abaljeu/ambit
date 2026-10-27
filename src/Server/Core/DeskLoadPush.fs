namespace Gambol.Server

open Gambol.Shared

/// Desk Load asks mailbox Load for a Workspace, Directory, or File.
module internal DeskLoadPush =

    let private subjectToLoad
        (request: LoadSaveCommandRequest)
        (path: LoadSavePath)
        (graph: Graph)
        : NodeId option =
        if request.holdStack then
            None
        else
            match path, request.operation with
            | LoadSavePath.Desk, LoadSaveOperation.Load ->
                LoadSubject.enclosingSubject
                    graph
                    request.start.focusId
                |> Option.map fst
            | _ -> None

    /// Mailbox-Load when the chosen path is Desk.
    let runLoadSave
        (run: LoadSaveContext)
        (load: NodeId -> Result<unit, string>)
        (request: LoadSaveCommandRequest)
        (path: LoadSavePath)
        =
        let graph = run.getState ()
        CoreActorPool.startLoadSaveCommand
            run
            path
            (LoadPathChoice.ofRequest request graph)
            request
        |> Result.bind (fun secret ->
            match subjectToLoad request path graph with
            | None -> Ok secret
            | Some subject ->
                load subject |> Result.map (fun () -> secret))
