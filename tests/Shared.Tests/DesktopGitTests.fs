module DesktopGitTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``remoteUrl appends locked gateway path to ambit base`` () =
    Assert.Equal(
        "https://host/ambit/git/home.git",
        WorkspaceGitRemote.remoteUrl "https://host/ambit/" "home")

[<Fact>]
let ``parseShortStatus reads ahead behind and dirty`` () =
    let text =
        "## main...ambit/main [ahead 2, behind 1]\n M note.txt\n"
    let status = WorkspaceGitRemote.parseShortStatus text
    Assert.Equal(Some "main", status.branch)
    Assert.Equal(2, status.ahead)
    Assert.Equal(1, status.behind)
    Assert.True(status.dirty)

[<Fact>]
let ``parseShortStatus clean tracking branch`` () =
    let status =
        WorkspaceGitRemote.parseShortStatus "## main...ambit/main\n"
    Assert.Equal(Some "main", status.branch)
    Assert.Equal(0, status.ahead)
    Assert.Equal(0, status.behind)
    Assert.False(status.dirty)

[<Fact>]
let ``formatStatusLine shows ahead behind dirty`` () =
    let line =
        WorkspaceGitRemote.formatStatusLine
            { branch = Some "main"; ahead = 2; behind = 1; dirty = true }
    Assert.Equal("main ↑2 ↓1 *", line)

[<Fact>]
let ``formatStatusLine clean branch`` () =
    let line =
        WorkspaceGitRemote.formatStatusLine
            { branch = Some "main"; ahead = 0; behind = 0; dirty = false }
    Assert.Equal("main", line)

[<Fact>]
let ``canGit requires git capability`` () =
    Assert.False(DesktopCapabilities.canGit None)
    Assert.False(
        DesktopCapabilities.canGit (Some DesktopCapabilities.disabled))
    Assert.True(
        DesktopCapabilities.canGit
            (Some (DesktopCapabilities.desktopEnabled true)))
    Assert.False(
        DesktopCapabilities.canGit
            (Some (DesktopCapabilities.desktopEnabled false)))

[<Fact>]
let ``mappedWithoutGit when import works but git does not`` () =
    Assert.False(DesktopCapabilities.mappedWithoutGit None)
    Assert.False(
        DesktopCapabilities.mappedWithoutGit (Some DesktopCapabilities.disabled))
    Assert.False(
        DesktopCapabilities.mappedWithoutGit
            (Some (DesktopCapabilities.desktopEnabled true)))
    Assert.True(
        DesktopCapabilities.mappedWithoutGit
            (Some (DesktopCapabilities.desktopEnabled false)))

[<Fact>]
let ``canWorkspaceSync needs file path caps`` () =
    Assert.False(DesktopCapabilities.canWorkspaceSync None)
    Assert.False(
        DesktopCapabilities.canWorkspaceSync
            (Some DesktopCapabilities.disabled))
    Assert.True(
        DesktopCapabilities.canWorkspaceSync
            (Some (DesktopCapabilities.desktopEnabled false)))
    Assert.True(
        DesktopCapabilities.canWorkspaceSync
            (Some (DesktopCapabilities.desktopEnabled true)))

[<Fact>]
let ``canWorkspacePush needs sync caps and git`` () =
    Assert.False(
        DesktopCapabilities.canWorkspacePush
            (Some (DesktopCapabilities.desktopEnabled false)))
    Assert.True(
        DesktopCapabilities.canWorkspacePush
            (Some (DesktopCapabilities.desktopEnabled true)))
