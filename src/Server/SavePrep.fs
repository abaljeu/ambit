namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
module SavePrep =

    let syncDataDir
        (dbStatus: DatabaseSetup.DbStatus)
        (getState: unit -> Async<Result<State, string>>)
        (getFileEventId: unit -> Async<EventId>)
        (dataDir: string)
        : Async<Result<int, string>> =
        async {
            match dbStatus with
            | DatabaseSetup.DbStatus.Ok ->
                let! stateResult = getState ()
                match stateResult with
                | Error err -> return Error err
                | Ok state ->
                    // Live-save already materialized artifacts; sync only needs eventId.
                    return Ok (EventId.value state.eventId)
            | _ ->
                let! eventId = getFileEventId ()
                return Ok (EventId.value eventId)
        }
