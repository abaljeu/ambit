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
let ``truncateForDisplay caps at maxLen`` () =
    Assert.Equal("verylongna", truncateForDisplay 10 "verylongname")

[<Fact>]
let ``summarizeHttpBody maps Azure unavailable HTML`` () =
    let body =
        """<!DOCTYPE html><html><head><title>Web App - Unavailable</title><style type="text/css">html{height:100%;width:100%;}#feature{width:960px;margin:95px auto 0 auto;overflow:auto;}#content{font-family:"Seg..."""
    Assert.Equal("Azure web app unavailable", summarizeHttpBody 200 body)

[<Fact>]
let ``summarizeHttpBody keeps status prefix before Azure HTML`` () =
    let body =
        """HTTP 403: <!DOCTYPE html><html><head><title>Web App - Unavailable</title></html>"""
    Assert.Equal(
        "HTTP 403: Azure web app unavailable",
        summarizeHttpBody 200 body)

[<Fact>]
let ``summarizeHttpBody maps other HTML to short label`` () =
    Assert.Equal(
        "HTML error page",
        summarizeHttpBody 200 "<html><body>nope</body></html>")

[<Fact>]
let ``summarizeHttpBody truncates plain text`` () =
    let long = System.String('x', 500)
    Assert.Equal(truncateForLog 200 long, summarizeHttpBody 200 long)
