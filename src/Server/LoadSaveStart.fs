namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
module LoadSaveStart =

    let private transportOperation =
        function
        | LoadSaveOperation.Load -> GithubTransportOperation.Load
        | LoadSaveOperation.Save -> GithubTransportOperation.Save

    let private startDeskCommand
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

    /// Desk stays the desk command. Git starts the transport actor.
    let startCommand
        (door: CoreChangesDoor)
        (dataDir: string)
        (path: LoadSavePath)
        (request: LoadSaveCommandRequest)
        =
        match path with
        | LoadSavePath.Desk ->
            startDeskCommand door path request
        | _ -> startGit door dataDir request
