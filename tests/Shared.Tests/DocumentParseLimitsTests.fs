module Gambol.Shared.Tests.DocumentParseLimitsTests

open System
open Gambol.Shared
open Xunit

[<Fact>]
let ``parse input limit is 50000 UTF16 code units and 100000 bytes`` () =
    Assert.Equal(50_000, DocumentParseLimits.maxInputCodeUnits)
    Assert.Equal(100_000L, DocumentParseLimits.maxInputUtf16Bytes)

[<Fact>]
let ``parse text accepts exactly 50000 ASCII code units`` () =
    let text = String.replicate 50_000 "a"
    Assert.Equal(50_000, text.Length)
    Assert.Equal(Ok (), DocumentParseLimits.refuseText text)

[<Fact>]
let ``parse text rejects blank import input`` () =
    Assert.Equal(
        Error "parse limits: text is empty",
        DocumentParseLimits.refuseEmptyText " \r\n\t")

[<Fact>]
let ``parse text rejects one code unit over with actual and limit`` () =
    let text = String.replicate 50_001 "a"
    let actualCodeUnits = 50_001

    Assert.Equal(
        Error(
            "parse input is too large: 50001 UTF-16 code units "
            + "(100002 UTF-16 bytes); limit is 50000 code units "
            + "(100000 UTF-16 bytes)"),
        DocumentParseLimits.refuseText text)

[<Fact>]
let ``parse text treats each BMP character as one UTF16 code unit`` () =
    let text = String.replicate 50_000 "é"
    Assert.Equal(50_000, text.Length)
    Assert.Equal(Ok (), DocumentParseLimits.refuseText text)

[<Fact>]
let ``parse text treats each surrogate pair as two UTF16 code units`` () =
    let text = String.replicate 25_000 "😀"
    Assert.Equal(50_000, text.Length)
    Assert.Equal(Ok (), DocumentParseLimits.refuseText text)

[<Fact>]
let ``parse text rejects one code unit over surrogate-pair boundary`` () =
    let text = String.replicate 25_000 "😀" + "a"
    Assert.Equal(50_001, text.Length)

    Assert.Equal(
        Error(DocumentParseLimits.errorForCodeUnits text.Length),
        DocumentParseLimits.refuseText text)
