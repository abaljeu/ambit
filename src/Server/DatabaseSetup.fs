namespace Gambol.Server

open Gambol.Shared

/// Database initialisation, schema setup, and agent caching at startup.
[<RequireQualifiedAccess>]
module DatabaseSetup =

    type DbStatus =
        | Ok
        | Mismatch1
        | Mismatch2
        | Absent

    let private decodeChangePayload (s: string) =
        Thoth.Json.Newtonsoft.Decode.fromString Serialization.decodeChange s



    // DB agent is a single shared instance (one database, one handle name).
    let private dbAgentCache: (string * DbAgent) option ref = ref None
    let private dbAgentLock = obj ()

    let getOrCreateDbAgent (connStr: string) (filename: string) : DbAgent =
        lock dbAgentLock (fun () ->
            match !dbAgentCache with
            | Some (name, agent) when name = filename -> agent
            | _ ->
                let agent = DbAgent.create connStr
                dbAgentCache.Value <- Some (filename, agent)
                agent
        )

    let statusFromMatches (matchesBeforeRebuild: bool) (matchesAfterRebuild: bool) : DbStatus =
        if matchesBeforeRebuild then
            DbStatus.Ok
        elif matchesAfterRebuild then
            DbStatus.Mismatch1
        else
            DbStatus.Mismatch2

    /// Resolve the DB connection string from config and run startup checks.
    let resolveDbConnection (connStr: string) (dataDir: string) : DbStatus =
        if connStr = "" then
            DbStatus.Absent
        else
            try
                Database.initSchema connStr |> Async.AwaitTask |> Async.RunSynchronously

                // Inline ensurePostgresMatchesFileAuthority logic
                let fileSt = DocumentLoader.loadState dataDir "gambol"
                let dbSt =
                    Database.loadPersistedState connStr decodeChangePayload
                    |> Async.AwaitTask
                    |> Async.RunSynchronously

                let statesMatch left right =
                    let graphOk = GraphProjection.graphEquals left.graph right.graph
                    let revOk = left.revision.Value = right.revision.Value
                    graphOk && revOk

                let status =
                    if statesMatch fileSt dbSt then
                        DbStatus.Ok
                    else
                        Database.rebuildFromDocumentFiles connStr dataDir "gambol"
                        |> Async.AwaitTask
                        |> Async.RunSynchronously

                        let dbStAfterRebuild =
                            Database.loadPersistedState connStr decodeChangePayload
                            |> Async.AwaitTask
                            |> Async.RunSynchronously

                        statusFromMatches false (statesMatch fileSt dbStAfterRebuild)

                match status with
                | DbStatus.Ok
                | DbStatus.Mismatch1 ->
                    getOrCreateDbAgent connStr "gambol" |> ignore
                | DbStatus.Mismatch2
                | DbStatus.Absent -> ()

                status
            with ex ->
                eprintfn "Gambol: DB_CONNECTION_STRING set but connection failed - falling back to file store. %s" ex.Message
                DbStatus.Absent
