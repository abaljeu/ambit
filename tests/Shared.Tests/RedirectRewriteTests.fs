module RedirectRewriteTests

open System
open Gambol.Shared
open Xunit

let private cloudAppUrl = Uri "https://collaborative-systems.org/ambit"
let private localUrl = Uri "http://localhost:54321"

[<Fact>]
let ``rewriteLocation replaces cloud app redirect origin with local origin`` () =
    let got =
        RedirectRewrite.rewriteLocation
            cloudAppUrl
            localUrl
            "https://collaborative-systems.org/ambit/login"

    Assert.Equal("http://localhost:54321/ambit/login", got)

[<Fact>]
let ``rewriteLocation preserves query and fragment`` () =
    let got =
        RedirectRewrite.rewriteLocation
            cloudAppUrl
            localUrl
            "https://collaborative-systems.org/ambit/login?error=1#top"

    Assert.Equal("http://localhost:54321/ambit/login?error=1#top", got)

[<Fact>]
let ``rewriteLocation leaves relative redirects local to the browser`` () =
    let got = RedirectRewrite.rewriteLocation cloudAppUrl localUrl "/ambit/login"

    Assert.Equal("/ambit/login", got)

[<Fact>]
let ``rewriteLocation leaves external redirects unchanged`` () =
    let got =
        RedirectRewrite.rewriteLocation
            cloudAppUrl
            localUrl
            "https://example.com/ambit/login"

    Assert.Equal("https://example.com/ambit/login", got)

[<Fact>]
let ``rewriteLocation leaves same host redirects outside app unchanged`` () =
    let got =
        RedirectRewrite.rewriteLocation
            cloudAppUrl
            localUrl
            "https://collaborative-systems.org/other"

    Assert.Equal("https://collaborative-systems.org/other", got)
