module Gambol.Shared.Tests.LogTextTests

open Xunit
open Gambol.Shared.LogText

[<Fact>]
let ``truncateForLog leaves short text unchanged`` () =
    Assert.Equal("abc", truncateForLog 10 "abc")

[<Fact>]
let ``truncateForLog adds ellipsis when over max`` () =
    let long = System.String('x', 500)
    let got = truncateForLog 400 long
    Assert.Equal(403, got.Length)
    Assert.True(got.EndsWith("..."))
    Assert.Equal('x', got.[399])

[<Fact>]
let ``truncateForDisplay leaves short text unchanged`` () =
    Assert.Equal("short", truncateForDisplay 10 "short")

[<Fact>]
let ``truncateForDisplay caps at maxLen with ellipsis`` () =
    Assert.Equal("verylongn\u2026", truncateForDisplay 10 "verylongname")
