namespace Gambol.Shared.Tests

open Gambol.Shared
open Xunit

module WorkspaceConnectTests =

    [<Fact>]
    let ``defaultLabelFromRoot uses folder name`` () =
        Assert.Equal("gambol", WorkspaceConnect.defaultLabelFromRoot "D:\\dev\\gambol")

    [<Fact>]
    let ``validateLabel rejects empty`` () =
        match WorkspaceConnect.validateLabel "  " with
        | Ok _ -> Assert.Fail "expected error"
        | Error err -> Assert.Equal("Label is required", err)

    [<Fact>]
    let ``mergeMapping replaces same label case insensitive`` () =
        let mappings =
            { entries =
                [ { label = "Home"; rootPath = "C:\\old" }
                  { label = "docs"; rootPath = "C:\\docs" } ] }

        let merged = WorkspaceLocalMapping.mergeMapping mappings "home" "C:\\new"
        Assert.Equal(2, merged.entries.Length)

        match merged.entries |> List.tryFind (fun e -> e.label.ToLowerInvariant() = "home") with
        | Some entry -> Assert.Equal("C:\\new", entry.rootPath)
        | None -> Assert.Fail "expected home entry"

module WorkspaceGitRemoteTests =

    [<Fact>]
    let ``gatewayUrl builds smart http path`` () =
        let url = WorkspaceGitRemote.gatewayUrl "https://example.org/ambit" "home"
        Assert.Equal("https://example.org/ambit/git/home.git", url)

    [<Fact>]
    let ``gatewayUrl preserves at prefix in label`` () =
        let url = WorkspaceGitRemote.gatewayUrl "http://localhost:5115/ambit" "@docs"
        Assert.Equal("http://localhost:5115/ambit/git/@docs.git", url)
