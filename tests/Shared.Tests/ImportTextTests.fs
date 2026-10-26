module Gambol.Shared.Tests.ImportTextTests

open Xunit
open Gambol.Shared

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

[<Fact>]
let ``DesktopFileStatusResponse serializes round-trip`` () =
    let source = System.DateTime(2024, 6, 1, 12, 0, 0, System.DateTimeKind.Utc)

    let response =
        { path = "note.txt"
          status = ExistingFile
          sourceModifiedUtc = Some source }

    let json = Enc.toString 0 (Serialization.encodeDesktopFileStatusResponse response)

    match Dec.fromString Serialization.decodeDesktopFileStatusResponse json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded ->
        Assert.Equal(response.path, decoded.path)
        Assert.Equal(response.status, decoded.status)
        Assert.Equal(response.sourceModifiedUtc, decoded.sourceModifiedUtc)
