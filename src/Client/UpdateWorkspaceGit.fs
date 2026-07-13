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

let private focusLabel (model: VM) : string option =
    let nodeId =
        match model.selectedNodes with
        | Some sel -> focusedNodeId model.graph sel
        | None -> viewRootNodeId model
    NodeDesktopPath.tryWorkspaceGitLabel model.graph nodeId

let private requireLabel (model: VM) : Result<string, string> =
    match focusLabel model with
    | Some label -> Ok label
    | None -> Error "focus a node under a named workspace"

let private encodeLabel (label: string) : string =
    Encode.object [ "label", Encode.string label ]
    |> Thoth.Json.JavaScript.Encode.toString 0

let private encodeLabelPath (label: string) (path: string) : string =
    Encode.object
        [ "label", Encode.string label
          "path", Encode.string path ]
    |> Thoth.Json.JavaScript.Encode.toString 0

/// Label (+ optional Ambit git PAT) for desktop push/pull/clone.
let private encodeLabelAuth
    (label: string)
    (auth: (string * string) option)
    : string =
    match auth with
    | None -> encodeLabel label
    | Some(user, token) ->
        Encode.object
            [ "label", Encode.string label
              "username", Encode.string user
              "token", Encode.string token ]
        |> Thoth.Json.JavaScript.Encode.toString 0

let private encodeLabelPathAuth
    (label: string)
    (path: string)
    (auth: (string * string) option)
    : string =
    match auth with
    | None -> encodeLabelPath label path
    | Some(user, token) ->
        Encode.object
            [ "label", Encode.string label
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

let private upsertMapping (label: string) (path: string) : Result<unit, string> =
    match putDesktop "/_desktop/workspace-mappings" (encodeLabelPath label path) with
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

let private setRemote (label: string) (path: string) : Result<string, string> =
    match postDesktop "/_desktop/git-remote" (encodeLabelPath label path) with
    | Error e -> Error e
    | Ok text ->
        match decodeDesktopGitOk text with
        | Ok { ok = true; detail = d } -> Ok d
        | Ok { error = Some e } -> Error e
        | Ok _ -> Error "git-remote failed"
        | Error e -> Error e

let private runLabeled
    (model: VM)
    (url: string)
    (onOk: string -> string)
    : VM * Effect list =
    if not (WorkspaceGitRemote.canDesktopGit model.desktopCapabilities) then
        model, []
    else
        match requireLabel model with
        | Error msg -> fail model msg
        | Ok label ->
            match fetchGitAuth () with
            | Error e -> fail model e
            | Ok auth ->
                match postDesktop url (encodeLabelAuth label auth) with
                | Error e -> fail model e
                | Ok text ->
                    match decodeDesktopGitOk text with
                    | Ok { ok = true; detail = d } ->
                        okDetail model (onOk d)
                    | Ok { error = Some e } -> fail model e
                    | Ok _ -> fail model "request failed"
                    | Error e -> fail model e

let gitPullOp (model: VM) : VM * Effect list =
    // Detail body is the mapped local path (from desktop ok.detail).
    runLabeled model "/_desktop/git-pull" (fun path -> path)

let gitPushOp (model: VM) : VM * Effect list =
    runLabeled model "/_desktop/git-push" (fun d -> "pushed: " + d)

let gitStatusOp (model: VM) : VM * Effect list =
    if not (WorkspaceGitRemote.canDesktopGit model.desktopCapabilities) then
        model, []
    else
        match requireLabel model with
        | Error msg -> fail model msg
        | Ok label ->
            match postDesktop "/_desktop/git-status" (encodeLabel label) with
            | Error e -> fail model e
            | Ok text ->
                match decodeWorkspaceGitStatus text with
                | Ok status ->
                    okDetail model (WorkspaceGitRemote.formatStatusLine status)
                | Error e -> fail model e

let private connectAtPath (model: VM) (label: string) (path: string) =
    match upsertMapping label path with
    | Error e -> fail model e
    | Ok () ->
        match setRemote label path with
        | Error e -> fail model e
        | Ok url -> okDetail model ("connected " + label + " → " + url)

let gitConnectOp (model: VM) : VM * Effect list =
    if not (WorkspaceGitRemote.canDesktopGit model.desktopCapabilities) then
        model, []
    else
        match requireLabel model with
        | Error msg -> fail model msg
        | Ok label ->
            match pickFolder true with
            | Error "cancelled" -> model, []
            | Error e -> fail model e
            | Ok path -> connectAtPath model label path

let private cloneAtPath (model: VM) (label: string) (path: string) =
    match fetchGitAuth () with
    | Error e -> fail model e
    | Ok auth ->
        match
            postDesktop
                "/_desktop/git-clone"
                (encodeLabelPathAuth label path auth)
        with
        | Error e -> fail model e
        | Ok _ ->
            match upsertMapping label path with
            | Error e -> fail model e
            | Ok () ->
                match setRemote label path with
                | Error e -> fail model e
                | Ok url ->
                    okDetail model ("cloned " + label + " → " + url)

let gitCloneOp (model: VM) : VM * Effect list =
    if not (WorkspaceGitRemote.canDesktopGit model.desktopCapabilities) then
        model, []
    else
        match requireLabel model with
        | Error msg -> fail model msg
        | Ok label ->
            match pickFolder false with
            | Error "cancelled" -> model, []
            | Error e -> fail model e
            | Ok path -> cloneAtPath model label path
