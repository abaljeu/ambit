module WorkspaceGitRemoteTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``repoPath uses locked gateway shape`` () =
    Assert.Equal("/ambit/git/home.git", WorkspaceGitRemote.repoPath "home")

[<Fact>]
let ``remoteUrl joins ambit base and label`` () =
    Assert.Equal(
        "http://localhost:5115/ambit/git/home.git",
        WorkspaceGitRemote.remoteUrl "http://localhost:5115/ambit" "home")

[<Fact>]
let ``remoteUrlMatches accepts case-insensitive gateway URL`` () =
    Assert.True(
        WorkspaceGitRemote.remoteUrlMatches
            "http://localhost:5115/ambit"
            "home"
            "HTTP://LOCALHOST:5115/ambit/git/home.git")

[<Fact>]
let ``tryLabelFromRepoName accepts label.git`` () =
    Assert.Equal(Some "home", WorkspaceGitRemote.tryLabelFromRepoName "home.git")

[<Fact>]
let ``tryLabelFromRepoName rejects missing suffix and bad names`` () =
    Assert.Equal(None, WorkspaceGitRemote.tryLabelFromRepoName "home")
    Assert.Equal(None, WorkspaceGitRemote.tryLabelFromRepoName "../x.git")
    Assert.Equal(None, WorkspaceGitRemote.tryLabelFromRepoName ".git")

[<Fact>]
let ``service path literals are stock git-*-pack`` () =
    Assert.Equal("git-upload-pack", WorkspaceGitRemote.WorkspacePull)
    Assert.Equal("git-receive-pack", WorkspaceGitRemote.WorkspacePush)

[<Fact>]
let ``parseHeadRef reads attached branch from HEAD file`` () =
    match WorkspaceGitRemote.parseHeadRef "ref: refs/heads/master\n" with
    | Ok branch -> Assert.Equal("master", branch)
    | Error err -> Assert.Fail(err)

[<Fact>]
let ``parseHeadRef preserves nested branch names`` () =
    match WorkspaceGitRemote.parseHeadRef "ref: refs/heads/server/live\r\n" with
    | Ok branch -> Assert.Equal("server/live", branch)
    | Error err -> Assert.Fail(err)

[<Fact>]
let ``parseHeadRef rejects detached HEAD`` () =
    match WorkspaceGitRemote.parseHeadRef "ccf7cf0c9ba2eac44c791cfd31350f2217e9a8a3\n" with
    | Ok branch -> Assert.Fail($"expected detached HEAD error, got {branch}")
    | Error err ->
        Assert.Equal("Cannot use detached HEAD for workspace git.", err)
