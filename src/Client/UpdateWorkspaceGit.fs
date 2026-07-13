module Gambol.Client.UpdateWorkspaceGit

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.ViewModel
open Thoth.Json.Core

let private jsonHeaders () = jsonMutatingPostHeaders ()

let private withResult (model: VM) (result: CmdLastResult) : VM * Effect list =
    { model with lastCmdResult = Some result }, []

let private fail (model: VM) (msg: string) : VM * Effect list =
    consoleLog ("[Gambol git] " + msg)
    withResult model (CmdLastResult.Error (None, msg))

let private okDetail (model: VM) (msg: string) : VM * Effect list =
    consoleLog ("[Gambol git] " + msg)
    withResult model (CmdLastResult.Detail (None, msg))

let private httpError (status: int) (body: string) : string =
    match decodePostChangeError body with
    | Some err -> err
    | None ->
        "HTTP " + string status + ": " + LogText.truncateForLog 200 body

let private workspaceFromFocus (model: VM) : string option =
    let nodeId =
        match model.selectedNodes with
        | Some sel -> focusedNodeId model.graph sel
        | None -> viewRootNodeId model
    NodeDesktopPath.enclosingWorkspaceName model.graph nodeId

let private requireNamedWorkspace (model: VM) : Result<string, string> =
    match workspaceFromFocus model with
    | Some workspace -> Ok workspace
    | None -> Error "focus a node under a named workspace"

let private encodeWorkspace (workspace: string) : string =
    Encode.object [ "label", Encode.string workspace ]
    |> Thoth.Json.JavaScript.Encode.toString 0

let private encodeWorkspacePath (workspace: string) (path: string) : string =
    Encode.object
        [ "label", Encode.string workspace
          "path", Encode.string path ]
    |> Thoth.Json.JavaScript.Encode.toString 0

/// Workspace (+ optional Ambit git PAT) for desktop push/pull/clone.
let private encodeWorkspaceAuth
    (workspace: string)
    (auth: (string * string) option)
    : string =
    match auth with
    | None -> encodeWorkspace workspace
    | Some(user, token) ->
        Encode.object
            [ "label", Encode.string workspace
              "username", Encode.string user
              "token", Encode.string token ]
        |> Thoth.Json.JavaScript.Encode.toString 0

let private encodeWorkspacePathAuth
    (workspace: string)
    (path: string)
    (auth: (string * string) option)
    : string =
    match auth with
    | None -> encodeWorkspacePath workspace path
    | Some(user, token) ->
        Encode.object
            [ "label", Encode.string workspace
              "path", Encode.string path
              "username", Encode.string user
              "token", Encode.string token ]
        |> Thoth.Json.JavaScript.Encode.toString 0

let private postDesktop (url: string) (body: string) : Result<string, string> =
    let status, text = postJsonSync url body (jsonHeaders ())
    if status < 200 || status >= 300 then Error(httpError status text)
    else Ok text

let private putDesktop (url: string) (body: string) : Result<string, string> =
    let status, text = putJsonSync url body (jsonHeaders ())
    if status < 200 || status >= 300 then Error(httpError status text)
    else Ok text

let private pickFolder (requireGit: bool) : Result<string, string> =
    let body =
        Encode.object [ "requireGit", Encode.bool requireGit ]
        |> Thoth.Json.JavaScript.Encode.toString 0
    match postDesktop "/_desktop/pick-folder" body with
    | Error e -> Error e
    | Ok text ->
        match decodeDesktopPickFolder text with
        | Error e -> Error e
        | Ok { cancelled = true } -> Error "cancelled"
        | Ok { path = Some path; gitRoot = gitRoot } when requireGit ->
            match gitRoot with
            | Some root -> Ok root
            | None -> Ok path
        | Ok { path = Some path } -> Ok path
        | Ok _ -> Error "no folder selected"

let private upsertMapping (workspace: string) (path: string) : Result<unit, string> =
    match putDesktop "/_desktop/workspace-mappings" (encodeWorkspacePath workspace path) with
    | Error e -> Error e
    | Ok _ -> Ok ()

/// Ambit session → git PAT (or None when gateway auth disabled).
let private fetchGitAuth () : Result<(string * string) option, string> =
    let status, text = getJsonSync "/ambit/git-token"
    if status = 401 then
        Error "login required for git"
    elif status < 200 || status >= 300 then
        Error(httpError status text)
    else
        match decodeGitTokenIssue text with
        | Error e -> Error e
        | Ok GitAuthDisabled -> Ok None
        | Ok (GitToken (user, token)) -> Ok (Some(user, token))

let private setRemote (workspace: string) (path: string) : Result<string, string> =
    match postDesktop "/_desktop/git-remote" (encodeWorkspacePath workspace path) with
    | Error e -> Error e
    | Ok text ->
        match decodeDesktopGitOk text with
        | Ok { ok = true; detail = d } -> Ok d
        | Ok { error = Some e } -> Error e
        | Ok _ -> Error "git-remote failed"
        | Error e -> Error e

let gitPullOp (model: VM) : VM * Effect list =
    if not (WorkspaceGitRemote.canDesktopGit model.desktopCapabilities) then
        model, []
    else
        match requireNamedWorkspace model with
        | Error msg -> fail model msg
        | Ok workspace ->
            match fetchGitAuth () with
            | Error e -> fail model e
            | Ok auth ->
                match postDesktop "/_desktop/git-pull" (encodeWorkspaceAuth workspace auth) with
                | Error e -> fail model e
                | Ok text ->
                    match decodeDesktopGitOk text with
                    | Ok { ok = true; detail = path } ->
                        okDetail model (workspace + " → " + path)
                    | Ok { error = Some e } -> fail model e
                    | Ok _ -> fail model "request failed"
                    | Error e -> fail model e

let gitPushOp (model: VM) : VM * Effect list =
    if not (WorkspaceGitRemote.canDesktopGit model.desktopCapabilities) then
        model, []
    else
        match requireNamedWorkspace model with
        | Error msg -> fail model msg
        | Ok workspace ->
            match fetchGitAuth () with
            | Error e -> fail model e
            | Ok auth ->
                match postDesktop "/_desktop/git-push" (encodeWorkspaceAuth workspace auth) with
                | Error e -> fail model e
                | Ok text ->
                    match decodeDesktopGitOk text with
                    | Ok { ok = true; detail = d } ->
                        okDetail model ("pushed: " + d)
                    | Ok { error = Some e } -> fail model e
                    | Ok _ -> fail model "request failed"
                    | Error e -> fail model e


let gitStatusOp (model: VM) : VM * Effect list =
    if not (WorkspaceGitRemote.canDesktopGit model.desktopCapabilities) then
        model, []
    else
        match requireNamedWorkspace model with
        | Error msg -> fail model msg
        | Ok workspace ->
            match postDesktop "/_desktop/git-status" (encodeWorkspace workspace) with
            | Error e -> fail model e
            | Ok text ->
                match decodeWorkspaceGitStatus text with
                | Ok status ->
                    okDetail model (WorkspaceGitRemote.formatStatusLine status)
                | Error e -> fail model e

let private connectAtPath (model: VM) (workspace: string) (path: string) =
    match upsertMapping workspace path with
    | Error e -> fail model e
    | Ok () ->
        match setRemote workspace path with
        | Error e -> fail model e
        | Ok url -> okDetail model ("connected " + workspace + " → " + url)

let gitConnectOp (model: VM) : VM * Effect list =
    if not (WorkspaceGitRemote.canDesktopGit model.desktopCapabilities) then
        model, []
    else
        match requireNamedWorkspace model with
        | Error msg -> fail model msg
        | Ok workspace ->
            match pickFolder true with
            | Error "cancelled" -> model, []
            | Error e -> fail model e
            | Ok path -> connectAtPath model workspace path

let private cloneAtPath (model: VM) (workspace: string) (path: string) =
    match fetchGitAuth () with
    | Error e -> fail model e
    | Ok auth ->
        match
            postDesktop
                "/_desktop/git-clone"
                (encodeWorkspacePathAuth workspace path auth)
        with
        | Error e -> fail model e
        | Ok _ ->
            match upsertMapping workspace path with
            | Error e -> fail model e
            | Ok () ->
                match setRemote workspace path with
                | Error e -> fail model e
                | Ok url ->
                    okDetail model ("cloned " + workspace + " → " + url)

let gitCloneOp (model: VM) : VM * Effect list =
    if not (WorkspaceGitRemote.canDesktopGit model.desktopCapabilities) then
        model, []
    else
        match requireNamedWorkspace model with
        | Error msg -> fail model msg
        | Ok workspace ->
            match pickFolder false with
            | Error "cancelled" -> model, []
            | Error e -> fail model e
            | Ok path -> cloneAtPath model workspace path
