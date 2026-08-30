namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
module SavePrep =

    let syncDataDir
        (persistenceMode: DatabaseSetup.PersistenceMode)
        (dbStatus: DatabaseSetup.DbStatus)
        (getState: unit -> Async<Result<StateResponse, string>>)
        (flushFileSnapshot: unit -> Async<Result<unit, string>>)
        (getFileRevision: unit -> Async<int>)
        (dataDir: string)
        : Async<Result<int, string>> =
        async {
            match persistenceMode, dbStatus with
            | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
                let! stateResult = getState ()
                match stateResult with
                | Error err -> return Error err
                | Ok state ->
                    // Live-save already materialized artifacts; sync only needs revision.
                    return Ok state.revision.Value
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
        (getState: unit -> Async<Result<StateResponse, string>>)
        (flushFileSnapshot: unit -> Async<Result<unit, string>>)
        (getFileRevision: unit -> Async<int>)
        (dataDir: string)
        : Async<Result<int, string>> =
        syncDataDir
            persistenceMode
            dbStatus
            getState
            flushFileSnapshot
            getFileRevision
            dataDir
