namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
module LoadSaveStart =

    let private transportOperation =
        function
        | LoadSaveOperation.Load -> GithubTransportOperation.Load
        | LoadSaveOperation.Save -> GithubTransportOperation.Save

    /// Desk Load of a valid subject places Order.Load. Desk Save and a held
    /// stack place nothing.
    let internal startDesk
        (door: CoreChangesDoor)
        (request: LoadSaveCommandRequest)
        : Async<Result<unit, string>> =
        async {
            match! CoreDoor.graphAtEvent door with
            | Error err -> return Error err
            | Ok state ->
                match
                    DeskLoadPush.subjectToLoad
                        request
                        LoadSavePath.Desk
                        state.graph
                with
                | None -> return Ok ()
                | Some subject ->
                    let! placed = CoreDoor.loadNode door subject
                    return Result.map ignore placed
        }

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

    /// Desk places Order.Load. Git starts the transport actor.
    let startCommand
        (door: CoreChangesDoor)
        (dataDir: string)
        (path: LoadSavePath)
        (request: LoadSaveCommandRequest)
        =
        match path with
        | LoadSavePath.Desk ->
            startDesk door request
        | _ -> startGit door dataDir request
