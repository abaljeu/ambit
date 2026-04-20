namespace Gambol.Server

open Gambol.Shared

/// Database initialisation, schema setup, and agent caching at startup.
[<RequireQualifiedAccess>]
module DatabaseSetup =

    let private decodeChangePayload (s: string) =
        Thoth.Json.Newtonsoft.Decode.fromString Serialization.decodeChange s

    /// Files are authority: if the DB projection or replay diverges, rebuild from disk.
    /// Returns true if a mismatch was detected and the DB was rebuilt.
    let private ensurePostgresMatchesFileAuthority (connStr: string) (dataDir: string) : bool =
        let fileSt = DocumentLoader.loadState dataDir "gambol"

        let dbSt =
            Database.loadPersistedState connStr decodeChangePayload
            |> Async.AwaitTask
            |> Async.RunSynchronously

        let graphOk = GraphProjection.graphEquals fileSt.graph dbSt.graph
        let revOk = fileSt.revision.Value = dbSt.revision.Value

        if not graphOk || not revOk then
            Database.rebuildFromDocumentFiles connStr dataDir "gambol"
            |> Async.AwaitTask
            |> Async.RunSynchronously

            eprintfn
                "Gambol: PostgreSQL rebuilt from file authority (graphOk=%b revOk=%b)."
                graphOk
                revOk
            true
        else
            false

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

    /// Resolve the DB connection string from config and run startup checks.
    /// Returns Some (connStr, status) where status is "ok" | "mismatch", or None on failure / absent config.
    let resolveDbConnection (dbConnStringOpt: string option) (dataDir: string) : (string * string) option =
        match dbConnStringOpt with
        | None -> None
        | Some connStr ->
            try
                Database.initSchema connStr |> Async.AwaitTask |> Async.RunSynchronously
                let hadMismatch = ensurePostgresMatchesFileAuthority connStr dataDir
                getOrCreateDbAgent connStr "gambol" |> ignore
                let status = if hadMismatch then "mismatch" else "ok"
                Some (connStr, status)
            with ex ->
                eprintfn "Gambol: DB_CONNECTION_STRING set but connection failed — falling back to file store. %s" ex.Message
                None
