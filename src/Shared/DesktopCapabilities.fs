namespace Gambol.Shared

open System
open Thoth.Json.Core

/// File-related endpoints served by the desktop local proxy under `/_desktop/*`.
type DesktopFileCapabilities =
    { canOpen: bool
      canImport: bool
      canExport: bool
      canStatus: bool
      canWorkspacePaths: bool }

/// `git` on PATH (ignore filtering / capability); not pack transport routes.
type DesktopGitCapabilities =
    { canGit: bool }

type DesktopCapabilities =
    { file: DesktopFileCapabilities
      git: DesktopGitCapabilities }

type FileReference =
    | NoFileReference
    | InvalidFileReference
    | FileReference of string

type DesktopFileStatus =
    | InvalidPath
    | CreateFile
    | MissingArtifact
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
        """{"file":{"open":false,"import":false,"export":false,"status":false,"workspacePaths":false},"git":{"git":false}}"""

    let desktopEnabledJson (canGit: bool) =
        let git = if canGit then "true" else "false"
        """{"file":{"open":false,"import":true,"export":true,"status":true,"workspacePaths":true},"git":{"git":"""
        + git
        + "}}"

    let disabled: DesktopCapabilities =
        { file =
            { canOpen = false
              canImport = false
              canExport = false
              canStatus = false
              canWorkspacePaths = false }
          git = { canGit = false } }

    let desktopEnabled (canGit: bool) : DesktopCapabilities =
        { file =
            { canOpen = false
              canImport = true
              canExport = true
              canStatus = true
              canWorkspacePaths = true }
          git = { canGit = canGit } }

    let private encodeFileCapabilities (capabilities: DesktopFileCapabilities) : IEncodable =
        Encode.object
            [ "open", Encode.bool capabilities.canOpen
              "import", Encode.bool capabilities.canImport
              "export", Encode.bool capabilities.canExport
              "status", Encode.bool capabilities.canStatus
              "workspacePaths", Encode.bool capabilities.canWorkspacePaths ]

    let private encodeGitCapabilities (capabilities: DesktopGitCapabilities) : IEncodable =
        Encode.object [ "git", Encode.bool capabilities.canGit ]

    let encode (capabilities: DesktopCapabilities) : IEncodable =
        Encode.object
            [ "file", encodeFileCapabilities capabilities.file
              "git", encodeGitCapabilities capabilities.git ]

    let private decodeFileCapabilities: Decoder<DesktopFileCapabilities> =
        Decode.object (fun get ->
            { canOpen = get.Required.Field "open" Decode.bool
              canImport = get.Required.Field "import" Decode.bool
              canExport = get.Required.Field "export" Decode.bool
              canStatus = get.Required.Field "status" Decode.bool
              canWorkspacePaths = get.Required.Field "workspacePaths" Decode.bool })

    let private decodeGitCapabilities: Decoder<DesktopGitCapabilities> =
        Decode.object (fun get ->
            { canGit = get.Required.Field "git" Decode.bool })

    let decoder: Decoder<DesktopCapabilities> =
        Decode.object (fun get ->
            { file = get.Required.Field "file" decodeFileCapabilities
              git = get.Required.Field "git" decodeGitCapabilities })

    /// True when desktop host reported git binary on PATH.
    let canGit (caps: DesktopCapabilities option) : bool =
        match caps with
        | Some { git = { canGit = true } } -> true
        | _ -> false

    /// Map / Pull: workspacePaths + file import/export (not pack transport).
    let canWorkspaceSync (caps: DesktopCapabilities option) : bool =
        match caps with
        | Some { file = f } ->
            f.canWorkspacePaths && f.canImport && f.canExport
        | _ -> false

    /// Push also needs git on PATH for check-ignore.
    let canWorkspacePush (caps: DesktopCapabilities option) : bool =
        canWorkspaceSync caps && canGit caps

    /// Desktop file import works but git is unavailable — Upload cannot push.
    let mappedWithoutGit (caps: DesktopCapabilities option) : bool =
        match caps with
        | Some { file = { canImport = true }; git = { canGit = false } } ->
            true
        | _ -> false

[<RequireQualifiedAccess>]
module NodeStatus =
    let label =
        function
        | InvalidPath -> "invalid"
        | CreateFile -> "create"
        | MissingArtifact -> "missing"
        | ExistingFile -> "file"
        | ExistingFolder -> "folder"
        | EvalError -> "error"
        | EvalOk-> "OK"

    let tryParse (text: string) : DesktopFileStatus option =
        match text with
        | "invalid" -> Some InvalidPath
        | "create" -> Some CreateFile
        | "missing" -> Some MissingArtifact
        | "file" -> Some ExistingFile
        | "folder" -> Some ExistingFolder
        | _ -> None
