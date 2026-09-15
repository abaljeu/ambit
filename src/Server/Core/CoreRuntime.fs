namespace Gambol.Server

open Gambol.Shared

type CoreRuntime =
    { changes: unit -> CoreChanges
      bindChanges: Credential -> CoreChanges
      /// Browser Change posts: secret is the request cookie (`gambol_auth`).
      browserChanges: Credential -> CoreChanges
      browserAuthority: Authority
      browserCredential: Credential
      login: Credential -> Async<Result<unit, string>>
      isAdmitted: Credential -> Async<bool>
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

    let private startHost
        (persistenceMode: DatabaseSetup.PersistenceMode)
        (dbStatus: DatabaseSetup.DbStatus)
        (pool: CoreActorPool)
        (dbConnectionString: string)
        (dataDir: string)
        (initialSecrets: Set<Credential>)
        : MailboxHost =
        match persistenceMode, dbStatus with
        | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
            CoreMailbox.host
                pool
                (DbAgent.persist
                    (DbAgent.createWithDataDir dbConnectionString dataDir))
                initialSecrets
        | _ ->
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create dataDir))
                initialSecrets

    let private bindRuntime
        host
        browserAuthority
        browserCredential
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
          browserAuthority = browserAuthority
          browserCredential = browserCredential
          login = fun secret -> CoreMailbox.login host secret
          isAdmitted = fun secret -> CoreMailbox.isAdmitted host secret
          flushFileSnapshot = fun () -> CoreMailbox.flushSnapshot host
          getFileRevision = fun () -> CoreMailbox.getRevision host }

    let create
        (persistenceMode: DatabaseSetup.PersistenceMode)
        (dbStatus: DatabaseSetup.DbStatus)
        (dbConnectionString: string)
        (dataDir: string)
        (authUser: string)
        (authPass: string)
        (actors: (ActorName * ActorFn) list)
        : CoreRuntime =
        let browserAuthority = Authority "Browser"
        let browserCredential =
            Credential(AuthToken.deriveToken authUser authPass)
        let pool = CoreActorPool.create ()
        actors
        |> List.iter (fun (name, actorFn) -> pool.register name actorFn)
        let host =
            startHost
                persistenceMode
                dbStatus
                pool
                dbConnectionString
                dataDir
                (Set.singleton browserCredential)
        let writable =
            persistenceMode <> DatabaseSetup.PersistenceMode.Db
            || dbStatus = DatabaseSetup.DbStatus.Ok
        let makeHandle caller =
            let raw = CoreMailbox.coreChanges host caller
            if writable then raw else readOnly raw
        bindRuntime host browserAuthority browserCredential makeHandle
