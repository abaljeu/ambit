namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
module LoadSaveStart =

    let private transportOperation =
        function
        | LoadSaveOperation.Load -> GithubTransportOperation.Load
        | LoadSaveOperation.Save -> GithubTransportOperation.Save

    let private startDeskLoad
        (door: CoreChangesDoor)
        (path: LoadSavePath)
        (request: LoadSaveCommandRequest)
        =
        CoreMailboxApi.startLoadSaveCommand
            door.host
            door.caller
            path
            (GithubTransportActor.peerName
                (transportOperation request.operation))
            request

    let private startGit
        (door: CoreChangesDoor)
        (dataDir: string)
        (request: LoadSaveCommandRequest)
        =
        GithubTransportActor.start
            door.host
            door.caller
            dataDir
            GithubTransportActor.productionDependencies
            (transportOperation request.operation)
            request.start

    /// Desk Save persists the focus. Git Save starts the transport actor.
    let startCommand
        (door: CoreChangesDoor)
        (dataDir: string)
        (path: LoadSavePath)
        (request: LoadSaveCommandRequest)
        =
        match path, request.operation with
        | LoadSavePath.Desk, LoadSaveOperation.Save ->
            // Persist only. The post-Save commit stays on the Save actor.
            GithubTransportActor.persistSaveFocus
                (CoreDoor.actorChanges door)
                request.start.focusId
        | LoadSavePath.Desk, _ ->
            startDeskLoad door path request
        | _ -> startGit door dataDir request
