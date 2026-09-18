namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
module SavePrep =

    let syncDataDir
        (persistenceMode: DatabaseSetup.PersistenceMode)
        (dbStatus: DatabaseSetup.DbStatus)
        (getState: unit -> Async<Result<State, string>>)
        (flushFileSnapshot: unit -> Async<Result<unit, string>>)
        (getFileEventId: unit -> Async<EventId>)
        (dataDir: string)
        : Async<Result<int, string>> =
        async {
            match persistenceMode, dbStatus with
            | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
                let! stateResult = getState ()
                match stateResult with
                | Error err -> return Error err
                | Ok state ->
                    // Live-save already materialized artifacts; sync only needs eventId.
                    return Ok (EventId.value state.eventId)
            | _ ->
                let! flushResult = flushFileSnapshot ()
                match flushResult with
                | Error err -> return Error err
                | Ok () ->
                    let! eventId = getFileEventId ()
                    return Ok (EventId.value eventId)
        }

    let syncGitArtifacts
        (persistenceMode: DatabaseSetup.PersistenceMode)
        (dbStatus: DatabaseSetup.DbStatus)
        (getState: unit -> Async<Result<State, string>>)
        (flushFileSnapshot: unit -> Async<Result<unit, string>>)
        (getFileEventId: unit -> Async<EventId>)
        (dataDir: string)
        : Async<Result<int, string>> =
        syncDataDir
            persistenceMode
            dbStatus
            getState
            flushFileSnapshot
            getFileEventId
            dataDir
