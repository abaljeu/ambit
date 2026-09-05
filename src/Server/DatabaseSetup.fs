namespace Gambol.Server

open System
open Gambol.Shared
/// Database initialisation, schema setup, and agent caching at startup.
[<RequireQualifiedAccess>]
module DatabaseSetup =
    /// Resolves the connection string, preferring TEST_DB_CONNECTION_STRING over DB_CONNECTION_STRING.
    let resolveConnectionString () : string =
        let testConn = System.Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING")
        if not (System.String.IsNullOrWhiteSpace(testConn)) then testConn
        else
            let mainConn = System.Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            if not (System.String.IsNullOrWhiteSpace(mainConn)) then mainConn
            else ""

    type DbStatus =
        | Ok
        | Mismatch1
        | Mismatch2
        | Absent

    type PersistenceMode =
        | Db
        | File

    let resolvePersistenceMode (raw: string) : Result<PersistenceMode, string> =
        let normalized =
            if isNull raw then ""
            else raw.Trim().ToLowerInvariant()

        match normalized with
        | "" | "db" -> Microsoft.FSharp.Core.Ok PersistenceMode.Db
        | "file" -> Microsoft.FSharp.Core.Ok PersistenceMode.File
        | _ ->
            Microsoft.FSharp.Core.Error (
                $"Unknown Persistence:Mode '{raw}'. " +
                "Use 'db' or 'file'.")

    let private decodeChangePayload (s: string) =
        Thoth.Json.Newtonsoft.Decode.fromString Serialization.decodeChange s

    // DB agent is a single shared instance (one database per dataDir).
    let private dbAgentCache: (string * DbAgent) option ref = ref None
    let private dbAgentLock = obj ()

    /// Hands back the Core Changes contract only; the raw agent stays in the cache.
    let getOrCreateDbAgent (connStr: string) (dataDir: string) : CoreChanges =
        lock dbAgentLock (fun () ->
            match !dbAgentCache with
            | Some (dir, agent) when dir = dataDir -> agent
            | _ ->
                let agent = DbAgent.createWithDataDir connStr dataDir
                dbAgentCache.Value <- Some (dataDir, agent)
                agent
        )
        |> DbAgent.coreChanges

    let statusFromMatches (matchesBeforeRebuild: bool) (matchesAfterRebuild: bool) : DbStatus =
        if matchesBeforeRebuild then
            DbStatus.Ok
        elif matchesAfterRebuild then
            DbStatus.Mismatch1
        else
            DbStatus.Mismatch2

    /// Disk vs DB: same persisted outline (Snapshot.write + normalize), then same revision.
    /// Structural `GraphProjection.graphEquals` is not used: file vs DB ids usually differ.
    let documentStatesMatch (left: State) (right: State) : bool =
        let l = Snapshot.write left.graph
        let r = Snapshot.write right.graph
        let ln = Snapshot.normalizeOutlineForCompare l
        let rn = Snapshot.normalizeOutlineForCompare r

        if ln <> rn then
            let dir = System.IO.Path.GetTempPath()
            let leftPath = System.IO.Path.Combine(dir, "gambol-outline-left.txt")
            let rightPath = System.IO.Path.Combine(dir, "gambol-outline-right.txt")
            System.IO.File.WriteAllText(leftPath, l)
            System.IO.File.WriteAllText(rightPath, r)
            eprintfn "Gambol: outline mismatch detail:%s%s" System.Environment.NewLine (Snapshot.describeOutlineMismatch ln rn)
            eprintfn "Gambol: wrote raw outlines to %s and %s" leftPath rightPath
            false
        elif left.revision.Value <> right.revision.Value then
            false
        else
            true

    let private bootstrapFromFileIfEmpty (connStr: string) (dataDir: string) : unit =
        let empty =
            Database.isEmpty connStr
            |> Async.AwaitTask
            |> Async.RunSynchronously

        if empty then
            let fileState = DocumentLoader.loadState dataDir
            Database.rebuildFromDocumentFiles connStr fileState
            |> Async.AwaitTask
            |> Async.RunSynchronously

    let private loadPersistedDbState (connStr: string) : State =
        Database.loadPersistedState connStr decodeChangePayload
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let private validateAmbNetworkAgainstDb (connStr: string) (dataDir: string) : DbStatus =
        let fileState =
            match DocumentLoader.tryLoadState dataDir with
            | Microsoft.FSharp.Core.Ok state -> state
            | Microsoft.FSharp.Core.Error msg -> failwith msg

        let dbState = loadPersistedDbState connStr
        let matchesBefore = documentStatesMatch fileState dbState

        if not matchesBefore then
            Database.rebuildFromDocumentFiles connStr fileState
            |> Async.AwaitTask
            |> Async.RunSynchronously

        let dbAfter = loadPersistedDbState connStr
        let matchesAfter = documentStatesMatch fileState dbAfter
        statusFromMatches matchesBefore matchesAfter

    /// Initialise schema and create the DB agent. `file` mode may seed an empty DB from files.
    let resolveDbConnection
        (persistenceMode: PersistenceMode)
        (connStr: string)
        (dataDir: string)
        : DbStatus =
        if connStr = "" then
            DbStatus.Absent
        else
            try
                Database.initSchema connStr |> Async.AwaitTask |> Async.RunSynchronously

                let status =
                    if persistenceMode = PersistenceMode.File then
                        let dbEmpty =
                            Database.isEmpty connStr
                            |> Async.AwaitTask
                            |> Async.RunSynchronously

                        if dbEmpty then
                            bootstrapFromFileIfEmpty connStr dataDir
                            DbStatus.Ok
                        else
                            validateAmbNetworkAgainstDb connStr dataDir
                    else
                        DbStatus.Ok

                getOrCreateDbAgent connStr dataDir |> ignore
                status
            with ex ->
                eprintfn "Gambol: DB connection failed - falling back to file store. %s" ex.Message
                DbStatus.Absent

    /// For test use only: clears the DB agent cache so the next startup creates a fresh instance.
    let resetAgentCacheForTest () =
        lock dbAgentLock (fun () -> dbAgentCache.Value <- None)
