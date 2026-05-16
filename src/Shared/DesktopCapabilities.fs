namespace Gambol.Shared

open System
open Thoth.Json.Core

type DesktopFileCapabilities =
    { canOpen: bool
      canImport: bool
      canExport: bool }

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

type DesktopFileStatusResponse =
    { path: string
      status: DesktopFileStatus }

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
    let disabledJson = """{"file":{"open":false,"import":false,"export":false}}"""
    let importEnabledJson = """{"file":{"open":false,"import":true,"export":false}}"""

    let disabled: DesktopCapabilities =
        { file =
            { canOpen = false
              canImport = false
              canExport = false } }

    let private encodeFileCapabilities (capabilities: DesktopFileCapabilities) : IEncodable =
        Encode.object
            [ "open", Encode.bool capabilities.canOpen
              "import", Encode.bool capabilities.canImport
              "export", Encode.bool capabilities.canExport ]

    let encode (capabilities: DesktopCapabilities) : IEncodable =
        Encode.object [ "file", encodeFileCapabilities capabilities.file ]

    let private decodeFileCapabilities: Decoder<DesktopFileCapabilities> =
        Decode.object (fun get ->
            { canOpen = get.Required.Field "open" Decode.bool
              canImport = get.Required.Field "import" Decode.bool
              canExport = get.Required.Field "export" Decode.bool })

    let decoder: Decoder<DesktopCapabilities> =
        Decode.object (fun get ->
            { file = get.Required.Field "file" decodeFileCapabilities })

[<RequireQualifiedAccess>]
module DesktopFileStatus =
    let label =
        function
        | InvalidPath -> "invalid"
        | CreateFile -> "create"
        | ExistingFile -> "file"
        | ExistingFolder -> "folder"

    let tryParse (text: string) : DesktopFileStatus option =
        match text with
        | "invalid" -> Some InvalidPath
        | "create" -> Some CreateFile
        | "file" -> Some ExistingFile
        | "folder" -> Some ExistingFolder
        | _ -> None
