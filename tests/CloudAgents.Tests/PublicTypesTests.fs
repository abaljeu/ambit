module Gambol.CloudAgents.Tests.PublicTypesTests

open Xunit
open Gambol.CloudAgents

[<Fact>]
let ``RepoConfig should hold URL and optional ref`` () =
    let config =
        { RepoConfig.Url = "https://github.com/test/repo"
          StartingRef = Some "main" }

    Assert.Equal("https://github.com/test/repo", config.Url)
    Assert.Equal(Some "main", config.StartingRef)

[<Fact>]
let ``AgentOptions can be empty`` () =
    let options =
        { AgentOptions.DisplayName = None
          ModelHint = None }

    Assert.Equal(None, options.DisplayName)
    Assert.Equal(None, options.ModelHint)

[<Fact>]
let ``AgentResult contains text and git info`` () =
    let git =
        [ { GitResult.RepoUrl = "github.com/test/repo"
            Branch = Some "cursor/test-branch"
            PullRequestUrl = None } ]

    let result =
        { AgentResult.Text = "Task completed"
          Git = git }

    Assert.Equal("Task completed", result.Text)
    Assert.Single(result.Git) |> ignore
    Assert.Equal("github.com/test/repo", result.Git.[0].RepoUrl)

[<Fact>]
let ``AgentStatus can represent all states`` () =
    let creating = Creating
    let running = Running
    let cancelled = Cancelled
    let failed = Failed "error message"

    let result =
        { AgentResult.Text = "Done"
          Git = [] }
    let finished = Finished result

    Assert.True(
        match creating with
        | Creating -> true
        | _ -> false
    )

    Assert.True(
        match running with
        | Running -> true
        | _ -> false
    )

    Assert.True(
        match finished with
        | Finished _ -> true
        | _ -> false
    )
