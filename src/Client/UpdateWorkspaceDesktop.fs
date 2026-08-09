module Gambol.Client.UpdateWorkspaceDesktop

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Thoth.Json.Core

let private jsonHeaders () = jsonMutatingPostHeaders ()

let httpError (status: int) (body: string) : string =
    match decodePostChangeError body with
    | Some err -> LogText.summarizeHttpBody 200 err
    | None ->
        "HTTP " + string status + ": " + LogText.summarizeHttpBody 200 body

let encodeWorkspacePath (workspace: string) (path: string) : string =
    Encode.object
        [ "label", Encode.string workspace
          "path", Encode.string path ]
    |> Thoth.Json.JavaScript.Encode.toString 0

let encodeSyncScope (scope: WorkspaceSyncScope) : string =
    let kind =
        match scope.kind with
        | SyncScopeKind.Workspace -> "workspace"
        | SyncScopeKind.Directory -> "directory"
        | SyncScopeKind.File -> "file"
    Encode.object
        [ "label", Encode.string scope.label
          "relative", Encode.string scope.relative
          "kind", Encode.string kind ]
    |> Thoth.Json.JavaScript.Encode.toString 0

let postDesktop (url: string) (body: string) : Result<string, string> =
    let status, text = postJsonSync url body (jsonHeaders ())
    if status < 200 || status >= 300 then Error(httpError status text)
    else Ok text

let putDesktop (url: string) (body: string) : Result<string, string> =
    let status, text = putJsonSync url body (jsonHeaders ())
    if status < 200 || status >= 300 then Error(httpError status text)
    else Ok text

let pickFolder () : Result<string, string> =
    match postDesktop "/_desktop/pick-folder" "{}" with
    | Error e -> Error e
    | Ok text ->
        match decodeDesktopPickFolder text with
        | Error e -> Error e
        | Ok { cancelled = true } -> Error "cancelled"
        | Ok { path = Some path } -> Ok path
        | Ok _ -> Error "no folder selected"

let upsertMapping (workspace: string) (path: string) : Result<unit, string> =
    match putDesktop "/_desktop/workspace-mappings" (encodeWorkspacePath workspace path) with
    | Error e -> Error e
    | Ok _ -> Ok ()

let folderBasename (path: string) : string =
    let trimmed = path.TrimEnd('\\', '/')
    let i =
        max (trimmed.LastIndexOf('\\')) (trimmed.LastIndexOf('/'))
    if i < 0 then trimmed
    else trimmed.Substring(i + 1)

let lookupMappedPath (label: string) : Result<string option, string> =
    let status, text = getJsonSync "/_desktop/workspace-mappings"
    if status < 200 || status >= 300 then
        Error(httpError status text)
    else
        match decodeMappedRootPath text label with
        | Error e -> Error e
        | Ok pathOpt -> Ok pathOpt

/// GET mappings; pick-folder + PUT when label has no mapping.
let ensureMapped (label: string) : Result<string, string> =
    if System.String.IsNullOrWhiteSpace label then
        Error "ROOT and Workspaces cannot be mapped"
    else
        match lookupMappedPath label with
        | Error e -> Error e
        | Ok(Some path) -> Ok path
        | Ok None ->
            match pickFolder () with
            | Error e -> Error e
            | Ok path ->
                match upsertMapping label path with
                | Error e -> Error e
                | Ok () -> Ok path
