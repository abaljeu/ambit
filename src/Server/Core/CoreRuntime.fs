namespace Gambol.Server

open System
open Gambol.Shared

type CoreRuntime =
    { changes: unit -> CoreChanges
      bindChanges: Credential -> CoreChanges
      browserChanges: unit -> CoreChanges
      credentials: CoreCredentials
      command: CoreActorPool
      browserCredential: Credential
      parseCredential: Credential
      flushFileSnapshot: unit -> Async<Result<unit, string>>
      getFileRevision: unit -> Async<Revision> }

[<RequireQualifiedAccess>]
module CoreRuntime =

    let readOnly (handle: CoreChanges) : CoreChanges =
        let rejectWrite (_: Change list) =
            async.Return(
                Error
                    "Database persistence is unavailable; file fallback is read-only.")
        { handle with
            postChange = rejectWrite
            postGraphOnlyChange = rejectWrite }

    let ofFileWithDbMirror
        (file: CoreChanges)
        (db: CoreChanges option)
        : CoreChanges =
        let mirror logFailure postFile postDb changes = async {
            let! fileResult = postFile changes
            match fileResult, db with
            | Ok accepted, Some dbHandle ->
                let! dbResult = postDb dbHandle changes
                match dbResult with
                | Error err -> logFailure err
                | Ok _ -> ()
                return Ok accepted
            | Ok accepted, None -> return Ok accepted
            | Error err, _ -> return Error err
        }
        { file with
            postChange =
                mirror
                    (eprintfn
                        "[Core] Secondary DB write failed after file persist: %s")
                    file.postChange
                    (fun handle -> handle.postChange)
            postGraphOnlyChange =
                mirror
                    (eprintfn
                        "[Core] Secondary DB graph-only write failed: %s")
                    file.postGraphOnlyChange
                    (fun handle -> handle.postGraphOnlyChange) }

    let private addLifetimeCredentials
        (credentials: CoreCredentials)
        (callers: CallerTable)
        =
        let browser = Credential(Guid.NewGuid().ToString("N"))
        let parse = Credential(Guid.NewGuid().ToString("N"))
        credentials.add browser |> Async.RunSynchronously
        credentials.add parse |> Async.RunSynchronously
        callers.add (Authority "browser") browser
        callers.add (Authority "parse") parse
        browser, parse

    let create
        (persistenceMode: DatabaseSetup.PersistenceMode)
        (dbStatus: DatabaseSetup.DbStatus)
        (dbConnectionString: string)
        (dataDir: string)
        : CoreRuntime =
        let fileHost = lazy (CoreMailbox.createFile dataDir)
        let getFile () = fileHost.Value |> CoreMailbox.coreChanges
        let actorHost =
            match persistenceMode, dbStatus with
            | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
                DatabaseSetup.getOrCreateDbHost
                    dbConnectionString
                    dataDir
            | _ ->
                fileHost.Value
        let rawHandle () =
            match persistenceMode, dbStatus with
            | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
                DatabaseSetup.getOrCreateDbAgent dbConnectionString dataDir
            | DatabaseSetup.PersistenceMode.File, DatabaseSetup.DbStatus.Ok ->
                let db =
                    DatabaseSetup.getOrCreateDbAgent dbConnectionString dataDir
                ofFileWithDbMirror (getFile ()) (Some db)
            | DatabaseSetup.PersistenceMode.Db, _ ->
                getFile () |> readOnly
            | DatabaseSetup.PersistenceMode.File, _ ->
                getFile ()
        let credentials = actorHost.credentials
        let browserCredential, parseCredential =
            addLifetimeCredentials credentials actorHost.callers
        let pool = actorHost.pool
        let changes () = pool.withLocks (rawHandle ())
        let bindChanges sender =
            CoreAuth.bindHandle credentials sender (changes ())
        { changes = changes
          bindChanges = bindChanges
          browserChanges = fun () -> bindChanges browserCredential
          credentials = credentials
          command = pool
          browserCredential = browserCredential
          parseCredential = parseCredential
          flushFileSnapshot =
            fun () -> fileHost.Value |> CoreMailbox.flushSnapshot
          getFileRevision =
            fun () -> fileHost.Value |> CoreMailbox.getRevision }
