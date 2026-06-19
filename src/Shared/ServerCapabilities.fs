namespace Gambol.Shared

open Thoth.Json.Core

type ServerCapabilities =
    { canGitSave: bool }

type GitSaveResponse =
    { ok: bool
      detail: string
      error: string option }

[<RequireQualifiedAccess>]
module ServerCapabilities =
    let encode (capabilities: ServerCapabilities) : IEncodable =
        Encode.object [ "gitSave", Encode.bool capabilities.canGitSave ]

    let decoder: Decoder<ServerCapabilities> =
        Decode.object (fun get ->
            { canGitSave = get.Required.Field "gitSave" Decode.bool })

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
