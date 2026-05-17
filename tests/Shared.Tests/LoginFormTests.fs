module LoginFormTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``tryParse reads username and password from form body`` () =
    match LoginForm.tryParse "username=alice&password=secret" with
    | Some { Username = u; Password = p } ->
        Assert.Equal("alice", u)
        Assert.Equal("secret", p)
    | None -> Assert.Fail "expected credentials"

[<Fact>]
let ``tryParse decodes url-encoded values`` () =
    match LoginForm.tryParse "username=al%20ice&password=p%2Bq" with
    | Some { Username = u; Password = p } ->
        Assert.Equal("al ice", u)
        Assert.Equal("p+q", p)
    | None -> Assert.Fail "expected credentials"

[<Fact>]
let ``tryParse rejects empty username`` () =
    Assert.True(LoginForm.tryParse "username=&password=x" |> Option.isNone)
