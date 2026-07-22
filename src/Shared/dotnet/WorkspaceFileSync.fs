namespace Gambol.Shared

open System
open System.IO
open System.Net.Http

/// Desktop Post (Push) / Get (Pull) over WebDAV.
[<RequireQualifiedAccess>]
module WorkspaceFileSync =

    type SyncResult =
        { uploaded: int
          downloaded: int
          detail: string }

    let private localFull (mappedRoot: string) (relative: string) =
        if relative = "" then mappedRoot
        else
            let parts =
                relative.Split(
                    [| '/' |],
                    StringSplitOptions.RemoveEmptyEntries)
            Path.GetFullPath(Path.Combine(Array.append [| mappedRoot |] parts))

    let private ensureLocalDir (path: string) =
        try
            Directory.CreateDirectory path |> ignore
            Ok ()
        with ex ->
            Error ex.Message

    let private writeFile (path: string) (bytes: byte[]) =
        try
            let parent = Path.GetDirectoryName path
            if not (String.IsNullOrEmpty parent) then
                Directory.CreateDirectory parent |> ignore
            File.WriteAllBytes(path, bytes)
            Ok ()
        with ex ->
            Error ex.Message

    let private readFile (path: string) =
        try Ok(File.ReadAllBytes path)
        with ex -> Error ex.Message

    let private uploadOne
        (client: HttpClient)
        (ambitBase: string)
        (label: string)
        (mappedRoot: string)
        (cookie: string option)
        (hint: string option)
        (item: LocalSyncItem)
        : Result<unit, string> =
        if item.isDirectory then
            WorkspaceDavClient.mkcol
                client ambitBase label item.relative cookie hint
        else
            match readFile (localFull mappedRoot item.relative) with
            | Error e -> Error e
            | Ok bytes ->
                WorkspaceDavClient.putBytes
                    client
                    ambitBase
                    label
                    item.relative
                    bytes
                    cookie
                    hint

    /// Push: local inventory → check-ignore → MKCOL/PUT → finish-commit.
    let post
        (client: HttpClient)
        (ambitBase: string)
        (mappedRoot: string)
        (scope: WorkspaceSyncScope)
        (cookieHeader: string option)
        (clientHint: string option)
        : Result<SyncResult, string> =
        match WorkspaceLocalInventory.listForPush mappedRoot scope with
        | Error e -> Error e
        | Ok items ->
            match
                WorkspaceDavClient.preparePush
                    client
                    ambitBase
                    scope.label
                    cookieHeader
                    clientHint
            with
            | Error e -> Error e
            | Ok () ->
                let ordered = WorkspaceLocalInventory.orderForUpload items

                let rec upload remaining count =
                    match remaining with
                    | [] -> Ok count
                    | item :: rest ->
                        match
                            uploadOne
                                client
                                ambitBase
                                scope.label
                                mappedRoot
                                cookieHeader
                                clientHint
                                item
                        with
                        | Error e ->
                            Error(item.relative + ": " + e)
                        | Ok () -> upload rest (count + 1)

                match upload ordered 0 with
                | Error e -> Error e
                | Ok uploaded ->
                    match
                        WorkspaceDavClient.finishCommit
                            client
                            ambitBase
                            scope.label
                            cookieHeader
                            clientHint
                    with
                    | Error e -> Error e
                    | Ok body ->
                        let baseDetail =
                            sprintf
                                "uploaded %d; finish-commit ok"
                                uploaded
                            + if body = "" then ""
                              else " (" + body + ")"

                        let detail =
                            if DesktopGit.isAvailable() then
                                baseDetail
                            else
                                baseDetail
                                + "; .gitignore filter skipped (git unavailable)"

                        Ok
                            { uploaded = uploaded
                              downloaded = 0
                              detail = detail }

    let private downloadFiles
        (client: HttpClient)
        (ambitBase: string)
        (label: string)
        (mappedRoot: string)
        (cookie: string option)
        (entries: DavInventoryEntry list)
        : Result<int, string> =
        let files =
            entries |> List.filter (fun e -> not e.isCollection)

        let dirs =
            entries
            |> List.filter (fun e -> e.isCollection && e.relative <> "")

        let ensureDirs =
            dirs
            |> List.fold
                (fun acc e ->
                    match acc with
                    | Error err -> Error err
                    | Ok () ->
                        ensureLocalDir (localFull mappedRoot e.relative))
                (Ok ())

        match ensureDirs with
        | Error e -> Error e
        | Ok () ->
            let rec loop remaining count =
                match remaining with
                | [] -> Ok count
                | (e: DavInventoryEntry) :: rest ->
                    match
                        WorkspaceDavClient.getBytes
                            client
                            ambitBase
                            label
                            e.relative
                            cookie
                    with
                    | Error err -> Error(e.relative + ": " + err)
                    | Ok bytes ->
                        match
                            writeFile
                                (localFull mappedRoot e.relative)
                                bytes
                        with
                        | Error err -> Error(e.relative + ": " + err)
                        | Ok () -> loop rest (count + 1)

            loop files 0

    /// Pull: PROPFIND inventory → GET files under mapped root.
    let get
        (client: HttpClient)
        (ambitBase: string)
        (mappedRoot: string)
        (scope: WorkspaceSyncScope)
        (cookieHeader: string option)
        : Result<SyncResult, string> =
        let depth =
            match scope.kind with
            | SyncScopeKind.File -> "0"
            | _ -> "infinity"

        match
            WorkspaceDavClient.propfind
                client
                ambitBase
                scope.label
                scope.relative
                depth
                cookieHeader
        with
        | Error e -> Error e
        | Ok inventory ->
            let scoped =
                inventory
                |> List.filter (fun e ->
                    WorkspaceSyncScope.isUnderScope scope e.relative)

            match
                downloadFiles
                    client
                    ambitBase
                    scope.label
                    mappedRoot
                    cookieHeader
                    scoped
            with
            | Error e -> Error e
            | Ok downloaded ->
                Ok
                    { uploaded = 0
                      downloaded = downloaded
                      detail = sprintf "downloaded %d files" downloaded }
