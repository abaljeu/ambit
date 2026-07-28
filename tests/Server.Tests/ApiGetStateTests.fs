module Gambol.Server.Tests.ApiGetStateTests

open Microsoft.AspNetCore.Http.HttpResults
open Xunit
open Gambol.Server
open Gambol.Shared

let private handleWithGetState
    (getState: unit -> Async<Result<string, string>>)
    : AgentHandle =
    { getState = getState
      getRevision = fun () -> async.Return 0
      getChangesSince = fun _ -> async.Return []
      isReady = fun () -> true
      postChange = fun _ -> async.Return(Result.Error "unused")
      postGraphOnlyChange = fun _ -> async.Return(Result.Error "unused") }

[<Fact>]
let ``getState returns Problem 500 with detail when agent fails`` () = task {
    let err =
        "Internal server error in FileAgent GetState (dataDir=C:\\data)."
    let handle =
        handleWithGetState (fun () -> async.Return(Result.Error err))
    let! result = Api.getState handle |> Async.StartAsTask
    match box result with
    | :? ProblemHttpResult as problem ->
        Assert.Equal(500, problem.StatusCode)
        Assert.Contains(err, problem.ProblemDetails.Detail)
    | other ->
        Assert.Fail($"Expected ProblemHttpResult, got {other.GetType().FullName}")
}

[<Fact>]
let ``getState returns JSON content when agent succeeds`` () = task {
    let json =
        """{"revision":0,"graph":{"root":"00000000-0000-0000-0000-000000000000","nodes":[]},"ready":true}"""
    let handle =
        handleWithGetState (fun () -> async.Return(Result.Ok json))
    let! result = Api.getState handle |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        Assert.Equal(json, content.ResponseContent)
        Assert.Equal("application/json", content.ContentType)
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}
