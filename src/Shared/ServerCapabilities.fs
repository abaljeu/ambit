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

type DesktopGitOkResponse =
    { ok: bool
      detail: string
      error: string option }

type DesktopPickFolderResponse =
    { cancelled: bool
      path: string option
      gitRoot: string option }

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
module DesktopGitOkResponse =
    let decoder: Decoder<DesktopGitOkResponse> =
        Decode.object (fun get ->
            { ok = get.Optional.Field "ok" Decode.bool |> Option.defaultValue false
              detail = get.Optional.Field "detail" Decode.string |> Option.defaultValue ""
              error = get.Optional.Field "error" Decode.string })

[<RequireQualifiedAccess>]
module DesktopPickFolderResponse =
    let decoder: Decoder<DesktopPickFolderResponse> =
        Decode.object (fun get ->
            { cancelled =
                get.Optional.Field "cancelled" Decode.bool
                |> Option.defaultValue false
              path = get.Optional.Field "path" Decode.string
              gitRoot = get.Optional.Field "gitRoot" Decode.string })

[<RequireQualifiedAccess>]
module WorkspaceGitStatusJson =
    let decoder: Decoder<WorkspaceGitStatus> =
        Decode.object (fun get ->
            { branch = get.Optional.Field "branch" Decode.string
              ahead = get.Optional.Field "ahead" Decode.int |> Option.defaultValue 0
              behind = get.Optional.Field "behind" Decode.int |> Option.defaultValue 0
              dirty =
                get.Optional.Field "dirty" Decode.bool
                |> Option.defaultValue false })
