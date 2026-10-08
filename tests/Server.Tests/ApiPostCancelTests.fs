module Gambol.Server.Tests.ApiPostCancelTests

open System.Net
open System.Net.Http
open System.Text
open Xunit
open Gambol.Server.Tests.TestBackend

let private jsonContent body =
    new StringContent(body, Encoding.UTF8, "application/json")

[<Fact>]
let ``POST cancel is absent`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDir dataDir
    use body = jsonContent "{}"
    let! response = client.PostAsync("/ambit/cancel", body)
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode)
}
