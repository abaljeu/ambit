namespace Gambol.Server

open Gambol.Shared

/// Composition result: mailbox door plus in-process Parse Caller.
/// Not a second admission API. HTTP builds Browser Caller from the cookie.
type CoreRuntime =
    { host: MailboxHost
      parseCaller: Caller
      pool: CoreActorPool }

/// Persist choice, auth seed, and optional actors to boot a CoreRuntime.
type CoreBoot =
    {
        DbStatus: DatabaseSetup.DbStatus
        DbConnectionString: string
        DataDir: string
        AuthUser: string
        AuthPass: string
        Actors: (ActorName * ActorFn) list
    }

[<RequireQualifiedAccess>]
module CoreRuntime =

    let private startHost
        (boot: CoreBoot)
        (pool: CoreActorPool)
        (credentials: CoreCredentials)
        (parsePush: NodeId -> unit)
        : MailboxHost =
        match boot.DbStatus with
        | DatabaseSetup.DbStatus.Ok ->
            CoreMailbox.hostPublishing
                pool
                (DbAgent.persist
                    (DbAgent.createWithDataDir
                        boot.DbConnectionString
                        boot.DataDir))
                credentials
                parsePush
        | _ ->
            CoreMailbox.hostPublishing
                pool
                (FileAgent.persist (FileAgent.create boot.DataDir))
                credentials
                parsePush

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

    let internal createPublishing
        (boot: CoreBoot)
        (parsePush: NodeId -> unit)
        : CoreRuntime =
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
                parsePush
        { host = host
          parseCaller = parseCaller
          pool = pool }

    let create (boot: CoreBoot) (parsePush: NodeId -> unit) : CoreRuntime =
        createPublishing boot parsePush
