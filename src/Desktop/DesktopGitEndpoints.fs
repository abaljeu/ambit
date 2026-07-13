namespace Gambol.Desktop

open System
open System.IO
open System.Text.Json
open System.Threading.Tasks
open Gambol.Shared
open Microsoft.AspNetCore.Http

/// `/_desktop/git-*` handlers (G5). Mapping must already exist in config.
/// Push/pull/clone take optional Ambit `{username,token}` from `/ambit/git-token`
/// and inject Basic auth for that git invocation (no GCM store).
[<RequireQualifiedAccess>]
module DesktopGitEndpoints =

    let private quoteJson (text: string) =
        JsonSerializer.Serialize text

    let private writeJson (context: HttpContext) (json: string) = task {
        context.Response.StatusCode <- StatusCodes.Status200OK
        context.Response.ContentType <- "application/json; charset=utf-8"
        do! context.Response.WriteAsync(json, context.RequestAborted)
    }

    let private writeBadRequest (context: HttpContext) (message: string) = task {
        context.Response.StatusCode <- StatusCodes.Status400BadRequest
        context.Response.ContentType <- "application/json; charset=utf-8"
        let json = "{\"error\":" + quoteJson message + "}"
        do! context.Response.WriteAsync(json, context.RequestAborted)
    }

    let private readBody (context: HttpContext) = task {
        use reader = new StreamReader(context.Request.Body)
        return! reader.ReadToEndAsync(context.RequestAborted)
    }

    let private tryGetString (root: JsonElement) (name: string) =
        let mutable value = Unchecked.defaultof<JsonElement>
        if not (root.TryGetProperty(name, &value)) then
            None
        elif value.ValueKind <> JsonValueKind.String then
            None
        else
            match value.GetString() with
            | null -> None
            | s when s.Trim().Length = 0 -> None
            | s -> Some (s.Trim())

    let private tryGetAuth (root: JsonElement) : (string * string) option =
        match tryGetString root "username", tryGetString root "token" with
        | Some user, Some token -> Some(user, token)
        | _ -> None

    let private decodeLabelAuth
        (body: string)
        : Result<string * (string * string) option, string> =
        try
            use document = JsonDocument.Parse body
            let root = document.RootElement
            match tryGetString root "label" with
            | None -> Error "label is required"
            | Some label -> Ok(label, tryGetAuth root)
        with
        | :? JsonException -> Error "invalid JSON"

    let private decodeLabelAndPath
        (body: string)
        : Result<string * string option * (string * string) option, string> =
        try
            use document = JsonDocument.Parse body
            let root = document.RootElement
            match tryGetString root "label" with
            | None -> Error "label is required"
            | Some label ->
                Ok(label, tryGetString root "path", tryGetAuth root)
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

    let private okDetail (detail: string) =
        "{\"ok\":true,\"detail\":" + quoteJson detail + "}"

    let private statusJson (status: WorkspaceGitStatus) =
        let branch =
            match status.branch with
            | None -> "null"
            | Some b -> quoteJson b
        sprintf
            "{\"ok\":true,\"branch\":%s,\"ahead\":%d,\"behind\":%d,\"dirty\":%b}"
            branch
            status.ahead
            status.behind
            status.dirty

    let writeCapabilities (canGit: bool) (context: HttpContext) = task {
        do! writeJson context (DesktopCapabilities.desktopEnabledJson canGit)
    }

    let private handleRemote
        (workspaceMap: Map<string, WorkspaceMapping>)
        (ambitBase: string)
        (context: HttpContext)
        = task {
        let! body = readBody context
        match decodeLabelAndPath body with
        | Error message -> do! writeBadRequest context message
        | Ok (label, pathOpt, _) ->
            let rootResult =
                match pathOpt with
                | Some path -> Ok path
                | None -> resolveMappedRoot workspaceMap label
            match rootResult with
            | Error message -> do! writeBadRequest context message
            | Ok localPath ->
                match DesktopGit.setAmbitRemoteForLabel localPath label ambitBase with
                | Error err -> do! writeBadRequest context err
                | Ok () ->
                    let url = WorkspaceGitRemote.remoteUrl ambitBase label
                    do! writeJson context (okDetail url)
    }

    let private handlePull
        (workspaceMap: Map<string, WorkspaceMapping>)
        (context: HttpContext)
        = task {
        let! body = readBody context
        match decodeLabelAuth body with
        | Error message -> do! writeBadRequest context message
        | Ok (label, auth) ->
            match resolveMappedRoot workspaceMap label with
            | Error message -> do! writeBadRequest context message
            | Ok localPath ->
                match DesktopGit.gitPull localPath auth with
                | Error err -> do! writeBadRequest context err
                | Ok _ -> do! writeJson context (okDetail localPath)
    }

    let private handlePush
        (workspaceMap: Map<string, WorkspaceMapping>)
        (context: HttpContext)
        = task {
        let! body = readBody context
        match decodeLabelAuth body with
        | Error message -> do! writeBadRequest context message
        | Ok (label, auth) ->
            match resolveMappedRoot workspaceMap label with
            | Error message -> do! writeBadRequest context message
            | Ok localPath ->
                match DesktopGit.push localPath auth with
                | Error err -> do! writeBadRequest context err
                | Ok detail -> do! writeJson context (okDetail detail)
    }

    let private handleStatus
        (workspaceMap: Map<string, WorkspaceMapping>)
        (context: HttpContext)
        = task {
        let! body = readBody context
        match decodeLabelAuth body with
        | Error message -> do! writeBadRequest context message
        | Ok (label, _) ->
            match resolveMappedRoot workspaceMap label with
            | Error message -> do! writeBadRequest context message
            | Ok localPath ->
                match DesktopGit.status localPath with
                | Error err -> do! writeBadRequest context err
                | Ok status -> do! writeJson context (statusJson status)
    }

    let private handleClone
        (ambitBase: string)
        (context: HttpContext)
        = task {
        let! body = readBody context
        match decodeLabelAndPath body with
        | Error message -> do! writeBadRequest context message
        | Ok (_, None, _) -> do! writeBadRequest context "path is required"
        | Ok (label, Some localPath, auth) ->
            let url = WorkspaceGitRemote.remoteUrl ambitBase label
            match DesktopGit.clone url localPath auth with
            | Error err -> do! writeBadRequest context err
            | Ok detail -> do! writeJson context (okDetail detail)
    }

    let tryHandle
        (workspaceMap: Map<string, WorkspaceMapping>)
        (ambitBase: string)
        (context: HttpContext)
        : Task<bool> =
        task {
            if not (HttpMethods.IsPost context.Request.Method) then
                return false
            else
                let path = context.Request.Path
                if path.Equals(PathString "/_desktop/git-remote") then
                    do! handleRemote workspaceMap ambitBase context
                    return true
                elif path.Equals(PathString "/_desktop/git-pull") then
                    do! handlePull workspaceMap context
                    return true
                elif path.Equals(PathString "/_desktop/git-push") then
                    do! handlePush workspaceMap context
                    return true
                elif path.Equals(PathString "/_desktop/git-status") then
                    do! handleStatus workspaceMap context
                    return true
                elif path.Equals(PathString "/_desktop/git-clone") then
                    do! handleClone ambitBase context
                    return true
                else
                    return false
        }
