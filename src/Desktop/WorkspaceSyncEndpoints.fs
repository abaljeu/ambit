namespace Gambol.Desktop

open System
open System.IO
open System.Net.Http
open System.Text.Json
open System.Threading.Tasks
open Gambol.Server
open Gambol.Shared
open Microsoft.AspNetCore.Http

/// `/_desktop/workspace-push` and `/_desktop/workspace-pull` (WebDAV).
[<RequireQualifiedAccess>]
module WorkspaceSyncEndpoints =

    let private quoteJson (text: string) =
        JsonSerializer.Serialize text

    let private writeJson (context: HttpContext) (json: string) = task {
        context.Response.StatusCode <- StatusCodes.Status200OK
        context.Response.ContentType <- "application/json; charset=utf-8"
        do! context.Response.WriteAsync(json, context.RequestAborted)
    }

    let private writeBadRequest (context: HttpContext) (message: string) =
        task {
            context.Response.StatusCode <-
                StatusCodes.Status400BadRequest
            context.Response.ContentType <-
                "application/json; charset=utf-8"
            let json = "{\"error\":" + quoteJson message + "}"
            do! context.Response.WriteAsync(json, context.RequestAborted)
        }

    let private readBody (context: HttpContext) = task {
        use reader = new StreamReader(context.Request.Body)
        return! reader.ReadToEndAsync(context.RequestAborted)
    }

    let private tryGetString (root: JsonElement) (name: string) =
        let mutable value = Unchecked.defaultof<JsonElement>
        if not (root.TryGetProperty(name, &value)) then None
        elif value.ValueKind <> JsonValueKind.String then None
        else
            match value.GetString() with
            | null -> None
            | s when s.Trim().Length = 0 -> None
            | s -> Some(s.Trim())

    let private parseKind (text: string) =
        match text.Trim().ToLowerInvariant() with
        | "workspace" -> Ok SyncScopeKind.Workspace
        | "directory" -> Ok SyncScopeKind.Directory
        | "file" -> Ok SyncScopeKind.File
        | _ -> Error "kind must be workspace, directory, or file"

    let private decodeScope
        (body: string)
        : Result<WorkspaceSyncScope, string> =
        try
            use document = JsonDocument.Parse body
            let root = document.RootElement
            match tryGetString root "label" with
            | None -> Error "label is required"
            | Some label ->
                let relative =
                    tryGetString root "relative"
                    |> Option.defaultValue ""
                let kindText =
                    tryGetString root "kind"
                    |> Option.defaultValue "workspace"
                match parseKind kindText with
                | Error e -> Error e
                | Ok kind ->
                    match WorkspaceSyncScope.normalizeRelative relative with
                    | Error e -> Error e
                    | Ok rel ->
                        match kind, rel with
                        | SyncScopeKind.Directory, "" ->
                            Error "directory path is empty"
                        | SyncScopeKind.File, "" ->
                            Error "file path is empty"
                        | SyncScopeKind.Workspace, _ ->
                            Ok
                                { label = label
                                  relative = ""
                                  kind = SyncScopeKind.Workspace }
                        | _ ->
                            Ok
                                { label = label
                                  relative = rel
                                  kind = kind }
        with
        | :? JsonException -> Error "invalid JSON"

    let private resolveMappedRoot
        (workspaceMap: Map<string, WorkspaceMapping>)
        (label: string)
        : Result<string, string> =
        match WorkspaceLocalMapping.resolvePath workspaceMap label "" with
        | Ok path -> Ok path
        | Error _ ->
            Error(WorkspaceLocalMapping.missingMappingMessage label)

    let private cookieHeader
        (creds: LoginForm.Credentials option)
        : string option =
        creds
        |> Option.map (fun c ->
            AuthToken.cookieHeaderValue c.Username c.Password)

    let private okSync (r: WorkspaceFileSync.SyncResult) =
        sprintf
            "{\"ok\":true,\"uploaded\":%d,\"downloaded\":%d,\"detail\":%s}"
            r.uploaded
            r.downloaded
            (quoteJson r.detail)

    let private clientHint (context: HttpContext) =
        match
            context.Request.Headers.TryGetValue(
                ClientIdentity.HeaderName)
        with
        | true, values -> ClientIdentity.tryFromValues values
        | _ -> None

    let private handlePush
        (workspaceMap: Map<string, WorkspaceMapping>)
        (client: HttpClient)
        (ambitBase: string)
        (creds: LoginForm.Credentials option)
        (context: HttpContext)
        = task {
        let! body = readBody context
        match decodeScope body with
        | Error message -> do! writeBadRequest context message
        | Ok scope ->
            match resolveMappedRoot workspaceMap scope.label with
            | Error message -> do! writeBadRequest context message
            | Ok mappedRoot ->
                match
                    WorkspaceFileSync.post
                        client
                        ambitBase
                        mappedRoot
                        scope
                        (cookieHeader creds)
                        (clientHint context)
                with
                | Error err ->
                    eprintfn
                        "[Desktop workspace-push] '%s': %s"
                        scope.label
                        err
                    do! writeBadRequest context err
                | Ok result ->
                    do! writeJson context (okSync result)
    }

    let private handlePull
        (workspaceMap: Map<string, WorkspaceMapping>)
        (client: HttpClient)
        (ambitBase: string)
        (creds: LoginForm.Credentials option)
        (context: HttpContext)
        = task {
        let! body = readBody context
        match decodeScope body with
        | Error message -> do! writeBadRequest context message
        | Ok scope ->
            match resolveMappedRoot workspaceMap scope.label with
            | Error message -> do! writeBadRequest context message
            | Ok mappedRoot ->
                match
                    WorkspaceFileSync.get
                        client
                        ambitBase
                        mappedRoot
                        scope
                        (cookieHeader creds)
                with
                | Error err ->
                    eprintfn
                        "[Desktop workspace-pull] '%s': %s"
                        scope.label
                        err
                    do! writeBadRequest context err
                | Ok result -> do! writeJson context (okSync result)
    }

    let tryHandle
        (workspaceMap: Map<string, WorkspaceMapping>)
        (client: HttpClient)
        (ambitBase: string)
        (creds: LoginForm.Credentials option)
        (context: HttpContext)
        : Task<bool> =
        task {
            if not (HttpMethods.IsPost context.Request.Method) then
                return false
            else
                let path = context.Request.Path
                if path.Equals(PathString "/_desktop/workspace-push") then
                    do!
                        handlePush
                            workspaceMap
                            client
                            ambitBase
                            creds
                            context
                    return true
                elif path.Equals(PathString "/_desktop/workspace-pull") then
                    do!
                        handlePull
                            workspaceMap
                            client
                            ambitBase
                            creds
                            context
                    return true
                else
                    return false
        }
