namespace Gambol.Shared

open Thoth.Json.Core

type DesktopFileCapabilities =
    { canOpen: bool
      canImport: bool
      canExport: bool }

type DesktopCapabilities =
    { file: DesktopFileCapabilities }

[<RequireQualifiedAccess>]
module DesktopCapabilities =
    let disabledJson = """{"file":{"open":false,"import":false,"export":false}}"""

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
