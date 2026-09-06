module Gambol.Server.Tests.BrowserCredentialTests

open System.Net
open System.Net.Http
open System.Text
open Xunit
open Gambol.Server
open Gambol.Server.Tests.TestBackend

let private jsonContent body =
    new StringContent(body, Encoding.UTF8, "application/json")

let private addLiveCookie (client: HttpClient) =
    client.DefaultRequestHeaders.Add(
        "Cookie",
        AuthToken.cookieHeaderValue "alice" "secret")

[<Fact>]
let ``Browser message without a live cookie is the same auth refuse as inactive Actor``
    () =
    task {
        let dataDir = newTempDir ()
        use client = createClientForDirWithAuth dataDir "alice" "secret"
        let! state = client.GetAsync("/ambit/state")
        let! poll = client.GetAsync("/ambit/poll")
        use changesBody = jsonContent "{}"
        let! changes = client.PostAsync("/ambit/changes", changesBody)
        use loadBody = jsonContent "{}"
        let! load = client.PostAsync("/ambit/load", loadBody)
        Assert.Equal(HttpStatusCode.Unauthorized, state.StatusCode)
        Assert.Equal(HttpStatusCode.Unauthorized, poll.StatusCode)
        Assert.Equal(HttpStatusCode.Unauthorized, changes.StatusCode)
        Assert.Equal(HttpStatusCode.Unauthorized, load.StatusCode)
        Assert.Equal("Unauthorized", CoreAuth.refuse)
    }

[<Fact>]
let ``Browser message with live cookie is not auth-refused`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDirWithAuth dataDir "alice" "secret"
    addLiveCookie client
    let! state = client.GetAsync("/ambit/state")
    let! poll = client.GetAsync("/ambit/poll")
    Assert.Equal(HttpStatusCode.OK, state.StatusCode)
    Assert.Equal(HttpStatusCode.OK, poll.StatusCode)
    use changesBody = jsonContent "{}"
    let! changes = client.PostAsync("/ambit/changes", changesBody)
    use loadBody = jsonContent "{}"
    let! load = client.PostAsync("/ambit/load", loadBody)
    Assert.NotEqual(HttpStatusCode.Unauthorized, changes.StatusCode)
    Assert.NotEqual(HttpStatusCode.Unauthorized, load.StatusCode)
}
