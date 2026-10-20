namespace Gambol.Server

open Gambol.Shared

/// MailboxHost creation for file, database, and publishing hosts.
[<RequireQualifiedAccess>]
module CoreMailbox =

    let internal hostPublishing
        (pool: CoreActorPool)
        (persist: PersistFilling)
        (credentials: CoreCredentials)
        (parsePush: NodeId -> unit)
        : MailboxHost * (NodeId -> unit) =
        let session = Session.create credentials
        let context, push =
            CoreMailboxLoad.makeMailBox
                persist
                pool
                parsePush
                session
        let started = CoreMailboxLoad.start context persist
        let created =
            MailboxHost.create
                started.processor
                persist
                context.noticeLog
                session
        persist.bindPersistFinished (CoreMailboxApi.postNotice created)
        PersistNodeThread.start {
            requests = context.persistRequests
            write = persist.handlers.persistNode
            finish = fun nodeId ->
                CoreMailboxApi.postNotice
                    created
                    (Notice.NodePersisted nodeId)
        }
        started.bindCoreChanges (fun caller ->
            { host = created
              caller = caller
              writable = true })
        created, push

    let internal hostWithParsePush
        (pool: CoreActorPool)
        (persist: PersistFilling)
        (credentials: CoreCredentials)
        (parsePush: NodeId -> unit)
        : MailboxHost =
        let host, _ = hostPublishing pool persist credentials parsePush
        host

    let internal host
        (pool: CoreActorPool)
        (persist: PersistFilling)
        (credentials: CoreCredentials)
        : MailboxHost =
        hostWithParsePush
            pool
            persist
            credentials
            (fun _ -> ())

    let createFile
        (dataDir: string)
        (credentials: CoreCredentials)
        : MailboxHost =
        host
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create dataDir))
            credentials

    let createDb
        (connectionString: string)
        (credentials: CoreCredentials)
        : MailboxHost =
        host
            (CoreActorPool.create ())
            (DbAgent.persist (DbAgent.create connectionString))
            credentials

    let createDbWithDataDir
        (connectionString: string)
        (dataDir: string)
        (credentials: CoreCredentials)
        : MailboxHost =
        host
            (CoreActorPool.create ())
            (DbAgent.persist
                (DbAgent.createWithDataDir connectionString dataDir))
            credentials
