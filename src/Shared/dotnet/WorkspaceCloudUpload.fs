namespace Gambol.Shared

open System
open System.Net.Http

/// Named workspace + mapped folder for Desktop non-UI Upload.
type WorkspaceCloudUploadArgs =
    { ambitBase: string
      mappedRoot: string
      label: string
      clientHint: string }

/// Create + stub + WebDAV push + Unparsed mark against one Ambit.
type WorkspaceCloudUploadProof =
    { created: ChangeSuccessResponse
      stubDetail: string
      pushed: WorkspaceFileSync.SyncResult
      markDetail: string
      state: StateResponse }

/// Desktop non-UI create / inventory / stub / push / mark. Stretch calls run.
[<RequireQualifiedAccess>]
module WorkspaceCloudUpload =

    let workspaceScope (label: string) : WorkspaceSyncScope =
        { label = label
          relative = ""
          kind = SyncScopeKind.Workspace }

    let stubItemsFromLocal (items: LocalSyncItem list) =
        items
        |> List.map (fun item ->
            ({ relative = item.relative
               isDirectory = item.isDirectory }
             : WorkspaceUploadStructure.InventoryItem))

    let listForUpload
        (mappedRoot: string)
        (scope: WorkspaceSyncScope)
        =
        WorkspaceLocalInventory.listForUpload mappedRoot scope

    let inventoryItems
        (mappedRoot: string)
        (scope: WorkspaceSyncScope)
        =
        match listForUpload mappedRoot scope with
        | Error e -> Error e
        | Ok(_mode, items) -> Ok(stubItemsFromLocal items)

    let push
        (client: HttpClient)
        (ambitBase: string)
        (mappedRoot: string)
        (scope: WorkspaceSyncScope)
        (cookie: string option)
        (clientHint: string option)
        =
        WorkspaceFileSync.post
            client
            ambitBase
            mappedRoot
            scope
            cookie
            clientHint

    let pushMapped
        (session: AmbitSession)
        (mappedRoot: string)
        (scope: WorkspaceSyncScope)
        =
        push
            session.client
            session.ambitBase
            mappedRoot
            scope
            (Some session.cookie)
            session.clientHint

    let createWorkspace
        (session: AmbitSession)
        (graph: Graph)
        (label: string)
        =
        let _id, ops = FileNodeOps.planCreateWorkspace graph label
        if ops.IsEmpty then
            Error "planCreateWorkspace returned no ops"
        else
            AmbitSession.postOps session ops

    let postStubs
        (session: AmbitSession)
        (graph: Graph)
        (label: string)
        (items: WorkspaceUploadStructure.InventoryItem list)
        =
        match WorkspaceUploadStructure.planStubOps graph label items with
        | Error e -> Error e
        | Ok [] -> Ok "no stub ops (paths already present or Unloaded)"
        | Ok ops ->
            match AmbitSession.postOps session ops with
            | Error e -> Error e
            | Ok _ -> Ok "stubs posted"

    let markBodiesPresent
        (session: AmbitSession)
        (graph: Graph)
        (label: string)
        (paths: string list)
        =
        let ops =
            WorkspaceUploadStructure.planServerFilePresentOps
                graph
                label
                paths
        if ops.IsEmpty then
            Ok "no NoServerFile rows to mark Unparsed"
        else
            match AmbitSession.postOps session ops with
            | Error e -> Error e
            | Ok _ -> Ok("marked Unparsed: " + String.concat "," paths)

    let private pushAndMark
        (session: AmbitSession)
        (args: WorkspaceCloudUploadArgs)
        (created: ChangeSuccessResponse)
        (stubDetail: string)
        =
        let scope = workspaceScope args.label
        match pushMapped session args.mappedRoot scope with
        | Error e -> Error("workspace-push: " + e)
        | Ok pushed ->
            match AmbitSession.getFullState session with
            | Error e -> Error("state after push: " + e)
            | Ok state2 ->
                match
                    markBodiesPresent
                        session
                        state2.graph
                        args.label
                        pushed.uploadedPaths
                with
                | Error e -> Error("mark present: " + e)
                | Ok markDetail ->
                    match AmbitSession.getFullState session with
                    | Error e -> Error("state after mark: " + e)
                    | Ok state3 ->
                        Ok
                            { created = created
                              stubDetail = stubDetail
                              pushed = pushed
                              markDetail = markDetail
                              state = state3 }

    let private uploadAfterCreate
        (session: AmbitSession)
        (args: WorkspaceCloudUploadArgs)
        (created: ChangeSuccessResponse)
        =
        match AmbitSession.getFullState session with
        | Error e -> Error("state after create: " + e)
        | Ok state1 ->
            let scope = workspaceScope args.label
            match inventoryItems args.mappedRoot scope with
            | Error e -> Error("inventory: " + e)
            | Ok items ->
                match postStubs session state1.graph args.label items with
                | Error e -> Error("stubs: " + e)
                | Ok stubDetail ->
                    pushAndMark session args created stubDetail

    let runWithClient
        (client: HttpClient)
        (args: WorkspaceCloudUploadArgs)
        =
        match
            AmbitSession.loginByGet
                client
                args.ambitBase
                (Some args.clientHint)
        with
        | Error e -> Error("login: " + e)
        | Ok session ->
            match AmbitSession.getFullState session with
            | Error e -> Error("state: " + e)
            | Ok state0 ->
                match
                    createWorkspace session state0.graph args.label
                with
                | Error e -> Error("create workspace: " + e)
                | Ok(_, created) ->
                    uploadAfterCreate session args created

    let run (args: WorkspaceCloudUploadArgs) =
        use client = AmbitSession.createHttpClient ()
        runWithClient client args
