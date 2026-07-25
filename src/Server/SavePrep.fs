namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
module SavePrep =

    let private decodeStateJson (json: string) : Result<State, string> =
        Thoth.Json.Newtonsoft.Decode.fromString
            (Thoth.Json.Core.Decode.object (fun get ->
                { graph = get.Required.Field "graph" Serialization.decodeGraph
                  revision = get.Required.Field "revision" Serialization.decodeRevision
                  history = History.empty }))
            json

    let syncDataDir
        (persistenceMode: DatabaseSetup.PersistenceMode)
        (dbStatus: DatabaseSetup.DbStatus)
        (getStateJson: unit -> Async<string>)
        (flushFileSnapshot: unit -> Async<Result<unit, string>>)
        (getFileRevision: unit -> Async<int>)
        (dataDir: string)
        (filename: string)
        : Async<Result<int, string>> =
        async {
            match persistenceMode, dbStatus with
            | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
                let! json = getStateJson ()
                match decodeStateJson json with
                | Error err -> return Error err
                | Ok state ->
                    try
                        DocumentLoader.writeStateBackup dataDir filename state
                        return Ok state.revision.Value
                    with ex ->
                        return Error ex.Message
            | _ ->
                let! flushResult = flushFileSnapshot ()
                match flushResult with
                | Error err -> return Error err
                | Ok () ->
                    let! rev = getFileRevision ()
                    return Ok rev
        }

    let syncGitArtifacts
        (persistenceMode: DatabaseSetup.PersistenceMode)
        (dbStatus: DatabaseSetup.DbStatus)
        (getStateJson: unit -> Async<string>)
        (flushFileSnapshot: unit -> Async<Result<unit, string>>)
        (getFileRevision: unit -> Async<int>)
        (dataDir: string)
        : Async<Result<int, string>> =
        async {
            match persistenceMode, dbStatus with
            | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
                let! json = getStateJson ()
                match decodeStateJson json with
                | Error err -> return Error err
                | Ok state ->
                    // Live-save already materialized artifacts on accept; git prep only needs revision.
                    return Ok state.revision.Value
            | _ ->
                let! flushResult = flushFileSnapshot ()
                match flushResult with
                | Error err -> return Error err
                | Ok () ->
                    let! rev = getFileRevision ()
                    return Ok rev
        }
