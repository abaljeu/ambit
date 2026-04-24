namespace Gambol.Server

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

    /// Resolve the DB connection string from config and run startup checks.
    /// `fileState` is the authoritative state already loaded by the FileAgent — passing it in
    /// avoids a second `Snapshot.read` call that would mint different NodeIds.
    let resolveDbConnection (connStr: string) (fileState: State) : DbStatus =
        if connStr = "" then
            DbStatus.Absent
        else
            try
                Database.initSchema connStr |> Async.AwaitTask |> Async.RunSynchronously

                let dbSt =
                    Database.loadPersistedState connStr decodeChangePayload
                    |> Async.AwaitTask
                    |> Async.RunSynchronously

                let status =
                    if documentStatesMatch fileState dbSt then
                        DbStatus.Ok
                    else
                        Database.rebuildFromDocumentFiles connStr fileState
                        |> Async.AwaitTask
                        |> Async.RunSynchronously

                        let dbStAfterRebuild =
                            Database.loadPersistedState connStr decodeChangePayload
                            |> Async.AwaitTask
                            |> Async.RunSynchronously

                        statusFromMatches false (documentStatesMatch fileState dbStAfterRebuild)

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
