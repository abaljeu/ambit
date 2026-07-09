namespace Gambol.Shared

open System
open Thoth.Json.Core

/// File-related endpoints served by the desktop local proxy under `/_desktop/*`.
type DesktopFileCapabilities =
    { canOpen: bool
      canImport: bool
      canExport: bool
      canStatus: bool
      canWorkspacePaths: bool
      canPickFolder: bool }

type DesktopPickFolderResponse =
    { path: string
      gitRoot: string }

type DesktopDetectGitResponse =
    { gitRoot: string }

type DesktopGitLabelRequest =
    { label: string }

type DesktopGitRemoteSetupRequest =
    { label: string
      url: string }

type DesktopGitOpResponse =
    { ok: bool
      detail: string option }

type DesktopGitPullResponse =
    { ok: bool
      changedPaths: string list
      detail: string option }

type DesktopCapabilities =
    { file: DesktopFileCapabilities }

type FileReference =
    | NoFileReference
    | InvalidFileReference
    | FileReference of string

type DesktopFileStatus =
    | InvalidPath
    | CreateFile
    | ExistingFile
    | ExistingFolder
    | EvalError
    | EvalOk

type DesktopFileStatusResponse =
    { path: string
      status: DesktopFileStatus
      sourceModifiedUtc: System.DateTime option }

[<RequireQualifiedAccess>]
module FileReference =
    let parseFirst (text: string) : FileReference =
        if isNull text then
            NoFileReference
        else
            let startIndex = text.IndexOf("[[", StringComparison.Ordinal)

            if startIndex < 0 then
                NoFileReference
            else
                let pathStart = startIndex + 2
                let endIndex = text.IndexOf("]]", pathStart, StringComparison.Ordinal)

                if endIndex < 0 then
                    InvalidFileReference
                else
                    let path = text.Substring(pathStart, endIndex - pathStart).Trim()

                    if path.Length = 0 then
                        InvalidFileReference
                    else
                        FileReference path

[<RequireQualifiedAccess>]
module DesktopCapabilities =
    let disabledJson =
        """{"file":{"open":false,"import":false,"export":false,"status":false,"workspacePaths":false,"pickFolder":false}}"""

    let desktopEnabledJson =
        """{"file":{"open":false,"import":true,"export":true,"status":true,"workspacePaths":true,"pickFolder":true}}"""

    let disabled: DesktopCapabilities =
        { file =
            { canOpen = false
              canImport = false
              canExport = false
              canStatus = false
              canWorkspacePaths = false
              canPickFolder = false } }

    let desktopEnabled: DesktopCapabilities =
        { file =
            { canOpen = false
              canImport = true
              canExport = true
              canStatus = true
              canWorkspacePaths = true
              canPickFolder = true } }

    let private encodeFileCapabilities (capabilities: DesktopFileCapabilities) : IEncodable =
        Encode.object
            [ "open", Encode.bool capabilities.canOpen
              "import", Encode.bool capabilities.canImport
              "export", Encode.bool capabilities.canExport
              "status", Encode.bool capabilities.canStatus
              "workspacePaths", Encode.bool capabilities.canWorkspacePaths
              "pickFolder", Encode.bool capabilities.canPickFolder ]

    let encode (capabilities: DesktopCapabilities) : IEncodable =
        Encode.object [ "file", encodeFileCapabilities capabilities.file ]

    let private decodeFileCapabilities: Decoder<DesktopFileCapabilities> =
        Decode.object (fun get ->
            { canOpen = get.Required.Field "open" Decode.bool
              canImport = get.Required.Field "import" Decode.bool
              canExport = get.Required.Field "export" Decode.bool
              canStatus = get.Required.Field "status" Decode.bool
              canWorkspacePaths = get.Required.Field "workspacePaths" Decode.bool
              canPickFolder =
                  get.Optional.Field "pickFolder" Decode.bool
                  |> Option.defaultValue false })

    let decoder: Decoder<DesktopCapabilities> =
        Decode.object (fun get ->
            { file = get.Required.Field "file" decodeFileCapabilities })

[<RequireQualifiedAccess>]
module NodeStatus =
    let label =
        function
        | InvalidPath -> "invalid"
        | CreateFile -> "create"
        | ExistingFile -> "file"
        | ExistingFolder -> "folder"
        | EvalError -> "error"
        | EvalOk-> "OK"

    let tryParse (text: string) : DesktopFileStatus option =
        match text with
        | "invalid" -> Some InvalidPath
        | "create" -> Some CreateFile
        | "file" -> Some ExistingFile
        | "folder" -> Some ExistingFolder
        | _ -> None
