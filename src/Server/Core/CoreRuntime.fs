namespace Gambol.Server

open Gambol.Shared

/// Composition result: mailbox door plus in-process Parse Caller.
/// Not a second admission API. HTTP builds Browser Caller from the cookie.
type CoreRuntime =
    { host: MailboxHost
      parseCaller: Caller }

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
        (credentials: CoreCredentials)
        : MailboxHost =
        match persistenceMode, dbStatus with
        | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
            CoreMailbox.host
                pool
                (DbAgent.persist
                    (DbAgent.createWithDataDir dbConnectionString dataDir))
                credentials
        | _ ->
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create dataDir))
                credentials

    let private bootCallers authUser authPass =
        let browserSecret =
            Credential(AuthToken.deriveToken authUser authPass)
        let browserCaller =
            { authority = Authority "Browser"
              name = ""
              secret = browserSecret }
        let parseCaller =
            { authority = Authority "Parse"
              name = "process"
              secret =
                Credential(
                    "parse:" + AuthToken.deriveToken authUser authPass) }
        browserCaller, parseCaller

    let create
        (persistenceMode: DatabaseSetup.PersistenceMode)
        (dbStatus: DatabaseSetup.DbStatus)
        (dbConnectionString: string)
        (dataDir: string)
        (authUser: string)
        (authPass: string)
        (actors: (ActorName * ActorFn) list)
        : CoreRuntime =
        let browserCaller, parseCaller = bootCallers authUser authPass
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
                (CoreCredentials.ofCallers (
                    Set.ofList [ browserCaller; parseCaller ]))
        { host = host; parseCaller = parseCaller }
