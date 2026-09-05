namespace Gambol.Server

open Gambol.Shared

type GraphAgentHandle =
    { getState: unit -> Async<Result<State, string>>
      getRevision: unit -> Async<Revision>
      getChangesSince: int -> Async<Change list>
      isReady: unit -> bool
      postChange: Change list -> Async<Result<CoreChangesAccepted, string>>
      postGraphOnlyChange:
        Change list -> Async<Result<CoreChangesAccepted, string>> }

type GraphAgentRuntime =
    { getHandle: unit -> GraphAgentHandle
      flushFileSnapshot: unit -> Async<Result<unit, string>>
      getFileRevision: unit -> Async<Revision> }

[<RequireQualifiedAccess>]
module GraphAgentHandle =

    let ofFile (agent: FileAgent) : GraphAgentHandle =
        { getState = fun () -> FileAgent.tryGetState agent
          getRevision = fun () -> FileAgent.getRevision agent
          getChangesSince = fun after -> FileAgent.getChangesSince agent after
          isReady = fun () -> true
          postChange = fun changes -> FileAgent.postChange agent changes
          postGraphOnlyChange =
            fun changes -> FileAgent.postGraphOnlyChange agent changes }

    let ofDb (agent: DbAgent) : GraphAgentHandle =
        { getState = fun () -> DbAgent.tryGetState agent
          getRevision = fun () -> DbAgent.getRevision agent
          getChangesSince = fun after -> DbAgent.getChangesSince agent after
          isReady = fun () -> DbAgent.isReady agent
          postChange = fun changes -> DbAgent.postChange agent changes
          postGraphOnlyChange =
            fun changes -> DbAgent.postGraphOnlyChange agent changes }

    let readOnly (handle: GraphAgentHandle) : GraphAgentHandle =
        let rejectWrite (_: Change list) =
            async.Return(
                Error
                    "Database persistence is unavailable; file fallback is read-only.")
        { handle with
            postChange = rejectWrite
            postGraphOnlyChange = rejectWrite }

    let ofFileWithDbMirror
        (file: FileAgent)
        (db: DbAgent option)
        : GraphAgentHandle =
        let mirror logFailure postFile postDb changes = async {
            let! fileResult = postFile changes
            match fileResult, db with
            | Ok accepted, Some dbAgent ->
                let! dbResult = postDb dbAgent changes
                match dbResult with
                | Error err -> logFailure err
                | Ok _ -> ()
                return Ok accepted
            | Ok accepted, None -> return Ok accepted
            | Error err, _ -> return Error err
        }
        { getState = fun () -> FileAgent.tryGetState file
          getRevision = fun () -> FileAgent.getRevision file
          getChangesSince = fun after -> FileAgent.getChangesSince file after
          isReady = fun () -> true
          postChange =
            mirror
                (eprintfn
                    "[Api] Secondary DB write failed after file persist: %s")
                (FileAgent.postChange file)
                DbAgent.postChange
          postGraphOnlyChange =
            mirror
                (eprintfn
                    "[Api] Secondary DB graph-only write failed: %s")
                (FileAgent.postGraphOnlyChange file)
                DbAgent.postGraphOnlyChange }

    let createRuntime
        (persistenceMode: DatabaseSetup.PersistenceMode)
        (dbStatus: DatabaseSetup.DbStatus)
        (dbConnectionString: string)
        (dataDir: string)
        : GraphAgentRuntime =
        let fileAgent = lazy (FileAgent.create dataDir)
        let getFile () = fileAgent.Value
        let getHandle () =
            match persistenceMode, dbStatus with
            | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
                DatabaseSetup.getOrCreateDbAgent dbConnectionString dataDir
                |> ofDb
            | DatabaseSetup.PersistenceMode.File, DatabaseSetup.DbStatus.Ok ->
                let db =
                    DatabaseSetup.getOrCreateDbAgent dbConnectionString dataDir
                ofFileWithDbMirror (getFile ()) (Some db)
            | DatabaseSetup.PersistenceMode.Db, _ ->
                getFile () |> ofFile |> readOnly
            | DatabaseSetup.PersistenceMode.File, _ ->
                getFile () |> ofFile
        { getHandle = getHandle
          flushFileSnapshot =
            fun () -> getFile () |> FileAgent.flushSnapshot
          getFileRevision =
            fun () -> getFile () |> FileAgent.getRevision }
