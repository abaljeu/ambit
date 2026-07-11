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
