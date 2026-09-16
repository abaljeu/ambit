namespace Gambol.Server

open Gambol.Shared

/// Composition result: mailbox door plus in-process Parse Caller.
/// Not a second admission API. HTTP builds Browser Caller from the cookie.
type CoreRuntime =
    { host: MailboxHost
      parseCaller: Caller }

/// Persist choice, auth seed, and optional actors to boot a CoreRuntime.
type CoreBoot =
    {
        PersistenceMode: DatabaseSetup.PersistenceMode
        DbStatus: DatabaseSetup.DbStatus
        DbConnectionString: string
        DataDir: string
        AuthUser: string
        AuthPass: string
        Actors: (ActorName * ActorFn) list
    }

[<RequireQualifiedAccess>]
module CoreRuntime =

    let readOnly (handle: CoreChanges) : CoreChanges =
        let reject msg =
            async.Return(Error msg)
        let readOnlyMsg =
            "Database persistence is unavailable; file fallback is read-only."
        let rejectPost (_: Change list) = reject readOnlyMsg
        let rejectEvents (_: Gambol.Shared.Ev list) =
            reject readOnlyMsg
        let rejectGraph (_: Change) = reject readOnlyMsg
        let rejectActorStop (_: ActorResult) = reject readOnlyMsg
        let rec wrap h : CoreChanges =
            { h with
                postChange = rejectPost
                postEvents = rejectEvents
                postGraphOnlyChange = rejectGraph
                actorStop = rejectActorStop
                getEventsSince = h.getEventsSince
                asCaller = fun caller -> wrap (h.asCaller caller) }
        wrap handle

    let private startHost
        (boot: CoreBoot)
        (pool: CoreActorPool)
        (credentials: CoreCredentials)
        : MailboxHost =
        match boot.PersistenceMode, boot.DbStatus with
        | DatabaseSetup.PersistenceMode.Db, DatabaseSetup.DbStatus.Ok ->
            CoreMailbox.host
                pool
                (DbAgent.persist
                    (DbAgent.createWithDataDir
                        boot.DbConnectionString
                        boot.DataDir))
                credentials
        | _ ->
            CoreMailbox.host
                pool
                (FileAgent.persist (FileAgent.create boot.DataDir))
                credentials

    let private bootCallers (boot: CoreBoot) =
        let browserSecret =
            Credential(AuthToken.deriveToken boot.AuthUser boot.AuthPass)
        let browserCaller =
            { authority = Authority "Browser"
              name = ""
              secret = browserSecret }
        let parseSecret =
            "parse:" + AuthToken.deriveToken boot.AuthUser boot.AuthPass
        let parseCaller =
            { authority = Authority "Parse"
              name = "process"
              secret = Credential parseSecret }
        browserCaller, parseCaller

    let create (boot: CoreBoot) : CoreRuntime =
        let browserCaller, parseCaller = bootCallers boot
        let pool = CoreActorPool.create ()
        boot.Actors
        |> List.iter (fun (name, actorFn) -> pool.register name actorFn)
        let host =
            startHost
                boot
                pool
                (CoreCredentials.ofCallers (
                    Set.ofList [ browserCaller; parseCaller ]))
        { host = host; parseCaller = parseCaller }
