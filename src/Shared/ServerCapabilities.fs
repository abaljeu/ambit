namespace Gambol.Shared

open Thoth.Json.Core

type ServerCapabilities =
    { canGitSave: bool
      canFileStatus: bool }

type GitSaveResponse =
    { ok: bool
      detail: string
      error: string option }

/// `GET /ambit/git-token` after cookie login (G4).
type GitTokenIssue =
    | GitAuthDisabled
    | GitToken of username: string * token: string

type DesktopWorkspaceSyncResponse =
    { ok: bool
      uploaded: int
      downloaded: int
      detail: string
      error: string option
      jobId: string option
      state: string option }

type DesktopWorkspaceDownloadJob =
    { id: string
      state: string
      detail: string
      started: string option
      finished: string option }

type DesktopPickFolderResponse =
    { cancelled: bool
      path: string option }

[<RequireQualifiedAccess>]
module ServerCapabilities =
    let encode (capabilities: ServerCapabilities) : IEncodable =
        Encode.object
            [ "gitSave", Encode.bool capabilities.canGitSave
              "fileStatus", Encode.bool capabilities.canFileStatus ]

    let decoder: Decoder<ServerCapabilities> =
        Decode.object (fun get ->
            { canGitSave = get.Required.Field "gitSave" Decode.bool
              canFileStatus =
                get.Optional.Field "fileStatus" Decode.bool
                |> Option.defaultValue false })

[<RequireQualifiedAccess>]
module GitSaveResponse =
    let encode (response: GitSaveResponse) : IEncodable =
        let fields =
            [ "ok", Encode.bool response.ok
              "detail", Encode.string response.detail ]
        let fields =
            match response.error with
            | None -> fields
            | Some err -> fields @ [ "error", Encode.string err ]
        Encode.object fields

    let decoder: Decoder<GitSaveResponse> =
        Decode.object (fun get ->
            { ok = get.Required.Field "ok" Decode.bool
              detail = get.Optional.Field "detail" Decode.string |> Option.defaultValue ""
              error = get.Optional.Field "error" Decode.string })

[<RequireQualifiedAccess>]
module GitTokenIssue =
    let decoder: Decoder<GitTokenIssue> =
        Decode.object (fun get ->
            match get.Optional.Field "disabled" Decode.bool with
            | Some true -> GitAuthDisabled
            | _ ->
                let user = get.Required.Field "username" Decode.string
                let token = get.Required.Field "token" Decode.string
                GitToken(user, token))

[<RequireQualifiedAccess>]
module DesktopWorkspaceSyncResponse =
    let decoder: Decoder<DesktopWorkspaceSyncResponse> =
        Decode.object (fun get ->
            { ok =
                get.Optional.Field "ok" Decode.bool
                |> Option.defaultValue false
              uploaded =
                get.Optional.Field "uploaded" Decode.int
                |> Option.defaultValue 0
              downloaded =
                get.Optional.Field "downloaded" Decode.int
                |> Option.defaultValue 0
              detail =
                get.Optional.Field "detail" Decode.string
                |> Option.defaultValue ""
              error = get.Optional.Field "error" Decode.string
              jobId = get.Optional.Field "jobId" Decode.string
              state = get.Optional.Field "state" Decode.string })

[<RequireQualifiedAccess>]
module DesktopWorkspaceDownloadJob =
    let decoder: Decoder<DesktopWorkspaceDownloadJob> =
        Decode.object (fun get ->
            { id = get.Required.Field "id" Decode.string
              state = get.Required.Field "state" Decode.string
              detail =
                get.Optional.Field "detail" Decode.string
                |> Option.defaultValue ""
              started = get.Optional.Field "started" Decode.string
              finished = get.Optional.Field "finished" Decode.string })

[<RequireQualifiedAccess>]
module DesktopPickFolderResponse =
    let decoder: Decoder<DesktopPickFolderResponse> =
        Decode.object (fun get ->
            { cancelled =
                get.Optional.Field "cancelled" Decode.bool
                |> Option.defaultValue false
              path = get.Optional.Field "path" Decode.string })
