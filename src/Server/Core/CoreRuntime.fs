namespace Gambol.Server

open System
open Gambol.Shared

type CoreRuntime =
    { changes: unit -> CoreChanges
      bindChanges: Credential -> CoreChanges
      /// Browser Change posts: secret is the request cookie (`gambol_auth`), not a closed-over GUID.
      browserChanges: Credential -> CoreChanges
      credentials: CoreCredentials
      command: CoreActorPool
      browserAuthority: Authority
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
        let rec wrap h : CoreChanges =
            { h with
                postChange = rejectWrite
                postGraphOnlyChange = rejectWrite
                asCaller = fun a s -> wrap (h.asCaller a s) }
        wrap handle

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
        let rec wrap fileHandle dbHandle : CoreChanges =
            { fileHandle with
                postChange =
                    mirror
                        (eprintfn
                            "[Core] Secondary DB write failed after file persist: %s")
                        fileHandle.postChange
                        (fun handle -> handle.postChange)
                postGraphOnlyChange =
                    mirror
                        (eprintfn
                            "[Core] Secondary DB graph-only write failed: %s")
                        fileHandle.postGraphOnlyChange
                        (fun handle -> handle.postGraphOnlyChange)
                asCaller =
                    fun a s ->
                        wrap
                            (fileHandle.asCaller a s)
                            (dbHandle
                             |> Option.map (fun d -> d.asCaller a s)) }
        wrap file db

    let private seedBrowserCredential
        (credentials: CoreCredentials)
        (authUser: string)
        (authPass: string)
        : Credential =
        let browser =
            Credential(AuthToken.deriveToken authUser authPass)
        credentials.add browser |> Async.RunSynchronously
        browser

    let private seedParseCredential (credentials: CoreCredentials) : Credential =
        let parse = Credential(Guid.NewGuid().ToString("N"))
        credentials.add parse |> Async.RunSynchronously
        parse

    let create
        (persistenceMode: DatabaseSetup.PersistenceMode)
        (dbStatus: DatabaseSetup.DbStatus)
        (dbConnectionString: string)
        (dataDir: string)
        (authUser: string)
        (authPass: string)
        : CoreRuntime =
        let credentials = CoreCredentials.create ()
        let browserAuthority = Authority "Browser"
        let browserCredential =
            seedBrowserCredential credentials authUser authPass
        let parseCredential = seedParseCredential credentials
        let fileStart = CoreMailbox.startFile credentials
        let dbStart = CoreMailbox.startDb credentials
        let fileHost =
            lazy (CoreMailbox.createFile fileStart dataDir)
        let makeHandle authority secret =
            let file =
                CoreMailbox.coreChanges
                    fileHost.Value
                    credentials
                    authority
                    secret
            let raw =
                match persistenceMode, dbStatus with
                | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
                    let dbHost =
                        DatabaseSetup.getOrCreateDbHost
                            dbStart
                            dbConnectionString
                            dataDir
                    CoreMailbox.coreChanges
                        dbHost
                        credentials
                        authority
                        secret
                | DatabaseSetup.PersistenceMode.File, DatabaseSetup.DbStatus.Ok ->
                    let dbHost =
                        DatabaseSetup.getOrCreateDbHost
                            dbStart
                            dbConnectionString
                            dataDir
                    let db =
                        CoreMailbox.coreChanges
                            dbHost
                            credentials
                            authority
                            secret
                    ofFileWithDbMirror file (Some db)
                | DatabaseSetup.PersistenceMode.Db, _ ->
                    readOnly file
                | DatabaseSetup.PersistenceMode.File, _ ->
                    file
            raw
        let pool = CoreActorPool.create credentials
        let changes () =
            pool.withLocks (
                makeHandle browserAuthority browserCredential)
        let bindChanges sender =
            pool.withLocks (
                makeHandle (Authority "Caller") sender)
        { changes = changes
          bindChanges = bindChanges
          browserChanges =
            fun secret ->
                pool.withLocks (
                    makeHandle browserAuthority secret)
          credentials = credentials
          command = pool
          browserAuthority = browserAuthority
          browserCredential = browserCredential
          parseCredential = parseCredential
          flushFileSnapshot =
            fun () -> fileHost.Value |> CoreMailbox.flushSnapshot
          getFileRevision =
            fun () -> fileHost.Value |> CoreMailbox.getRevision }
