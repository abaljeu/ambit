module LoginRedirectTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``isSuccess accepts redirect to ambit root`` () =
    Assert.True(LoginRedirect.isSuccess 302 [ "/ambit" ])

[<Fact>]
let ``isSuccess accepts absolute ambit redirect`` () =
    Assert.True(
        LoginRedirect.isSuccess
            302
            [ "https://collaborative-systems.org/ambit" ])

[<Fact>]
let ``isSuccess rejects redirect back to login`` () =
    Assert.False(LoginRedirect.isSuccess 302 [ "/ambit/login?error=1" ])

[<Fact>]
let ``isSuccess rejects non-redirect status`` () =
    Assert.False(LoginRedirect.isSuccess 200 [ "/ambit" ])
