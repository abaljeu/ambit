namespace Gambol.Server

open System
open Gambol.Shared

type CoreRuntime =
    { changes: unit -> CoreChanges
      bindChanges: Credential -> CoreChanges
      /// Browser Change posts: secret is the request cookie (`gambol_auth`), not a closed-over GUID.
      browserChanges: Credential -> CoreChanges
      credentials: CoreCredentials
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
        let rejectActorStop (_: ActorResult) =
            async.Return(
                Error
                    "Database persistence is unavailable; file fallback is read-only.")
        let rec wrap h : CoreChanges =
            { h with
                postChange = rejectWrite
                postGraphOnlyChange = rejectWrite
                actorStop = rejectActorStop
                asCaller = fun caller -> wrap (h.asCaller caller) }
        wrap handle

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

    let private startHost
        (persistenceMode: DatabaseSetup.PersistenceMode)
        (dbStatus: DatabaseSetup.DbStatus)
        (credentials: CoreCredentials)
        (pool: CoreActorPool)
        (dbConnectionString: string)
        (dataDir: string)
        : MailboxHost =
        match persistenceMode, dbStatus with
        | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
            CoreMailbox.host
                credentials
                pool
                (DbAgent.persist
                    (DbAgent.createWithDataDir dbConnectionString dataDir))
        | _ ->
            CoreMailbox.host
                credentials
                pool
                (FileAgent.persist (FileAgent.create dataDir))

    let private bindRuntime
        host
        credentials
        browserAuthority
        browserCredential
        parseCredential
        makeHandle
        : CoreRuntime =
        { changes =
            fun () ->
                makeHandle
                    { authority = browserAuthority
                      secret = browserCredential }
          bindChanges =
            fun sender ->
                makeHandle
                    { authority = Authority "Caller"
                      secret = sender }
          browserChanges =
            fun secret ->
                makeHandle
                    { authority = browserAuthority
                      secret = secret }
          credentials = credentials
          browserAuthority = browserAuthority
          browserCredential = browserCredential
          parseCredential = parseCredential
          flushFileSnapshot = fun () -> CoreMailbox.flushSnapshot host
          getFileRevision = fun () -> CoreMailbox.getRevision host }

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
        let pool = CoreActorPool.create credentials
        pool.register (ActorName "test") TestActor.actorFn
        let host =
            startHost
                persistenceMode
                dbStatus
                credentials
                pool
                dbConnectionString
                dataDir
        let writable =
            persistenceMode <> DatabaseSetup.PersistenceMode.Db
            || dbStatus = DatabaseSetup.DbStatus.Ok
        let makeHandle caller =
            let raw = CoreMailbox.coreChanges host credentials caller
            if writable then raw else readOnly raw
        bindRuntime
            host
            credentials
            browserAuthority
            browserCredential
            parseCredential
            makeHandle
