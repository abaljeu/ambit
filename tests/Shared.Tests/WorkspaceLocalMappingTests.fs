module Gambol.Shared.Tests.WorkspaceLocalMappingTests

open System
open System.IO
open Gambol.Shared
open Xunit

let private decodeOrFail (json: string) =
    match WorkspaceLocalMapping.decode json with
    | Ok mappings -> mappings
    | Error err -> failwith $"Expected decode success, got: {err}"

[<Fact>]
let ``decode accepts empty object as empty mappings`` () =
    let mappings = decodeOrFail "{}"
    Assert.Empty(mappings.entries)

[<Fact>]
let ``decode rejects duplicate labels case-insensitive`` () =
    let json =
        """{"workspaceMappings":[{"label":"Main","path":"C:\\repo"},{"label":"main","path":"D:\\repo"}]}"""

    match WorkspaceLocalMapping.decode json with
    | Ok _ -> Assert.Fail("Expected duplicate label validation error.")
    | Error err -> Assert.Equal("duplicate_workspace", err)

[<Fact>]
let ``decode rejects non-absolute path`` () =
    let json =
        """{"workspaceMappings":[{"label":"Main","path":"../repo"}]}"""

    match WorkspaceLocalMapping.decode json with
    | Ok _ -> Assert.Fail("Expected absolute-path validation error.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath rejects upward traversal`` () =
    let mappings =
        { entries = [ { label = "main"; rootPath = "C:\\repo" } ] }
        |> WorkspaceLocalMapping.toMap

    match WorkspaceLocalMapping.resolvePath mappings "main" "..\\secret.txt" with
    | Ok _ -> Assert.Fail("Expected traversal rejection.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath rejects dotdot segment`` () =
    let mappings =
        { entries = [ { label = "main"; rootPath = "C:\\repo" } ] }
        |> WorkspaceLocalMapping.toMap

    match WorkspaceLocalMapping.resolvePath mappings "main" "foo/../secret.txt" with
    | Ok _ -> Assert.Fail("Expected dotdot rejection.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath rejects empty segment from double slash`` () =
    let mappings =
        { entries = [ { label = "main"; rootPath = "C:\\repo" } ] }
        |> WorkspaceLocalMapping.toMap

    match WorkspaceLocalMapping.resolvePath mappings "main" "foo//bar" with
    | Ok _ -> Assert.Fail("Expected empty-segment rejection.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath rejects drive-relative path`` () =
    let mappings =
        { entries = [ { label = "main"; rootPath = "C:\\repo" } ] }
        |> WorkspaceLocalMapping.toMap

    match WorkspaceLocalMapping.resolvePath mappings "main" "D:foo" with
    | Ok _ -> Assert.Fail("Expected drive-relative rejection.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath rejects hash character`` () =
    let mappings =
        { entries = [ { label = "main"; rootPath = "C:\\repo" } ] }
        |> WorkspaceLocalMapping.toMap

    match WorkspaceLocalMapping.resolvePath mappings "main" "foo#bar" with
    | Ok _ -> Assert.Fail("Expected hash rejection.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath rejects caret character`` () =
    let mappings =
        { entries = [ { label = "main"; rootPath = "C:\\repo" } ] }
        |> WorkspaceLocalMapping.toMap

    match WorkspaceLocalMapping.resolvePath mappings "main" "foo^bar" with
    | Ok _ -> Assert.Fail("Expected caret rejection.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath accepts valid relative path`` () =
    let mappings =
        { entries = [ { label = "main"; rootPath = "C:\\repo" } ] }
        |> WorkspaceLocalMapping.toMap

    match WorkspaceLocalMapping.resolvePath mappings "main" "src/lib/helpers.fs" with
    | Error err -> Assert.Fail($"Expected success, got: {err}")
    | Ok resolved -> Assert.StartsWith("C:\\repo", resolved)

[<Fact>]
let ``encode round-trips through decode`` () =
    let original =
        { entries =
            [ { label = "home"; rootPath = "C:\\dev\\home" }
              { label = "docs"; rootPath = "D:\\docs" } ] }
    let decoded = decodeOrFail (WorkspaceLocalMapping.encode original)
    Assert.Equal(2, decoded.entries.Length)
    Assert.Equal("home", decoded.entries.[0].label)
    Assert.Equal("C:\\dev\\home", decoded.entries.[0].rootPath)

[<Fact>]
let ``upsert replaces existing label case-insensitively`` () =
    let start =
        { entries = [ { label = "Home"; rootPath = "C:\\old" } ] }
    match WorkspaceLocalMapping.upsert start "home" "D:\\new" with
    | Error err -> Assert.Fail(err)
    | Ok next ->
        Assert.Equal(1, next.entries.Length)
        Assert.Equal("home", next.entries.[0].label)
        Assert.Equal("D:\\new", next.entries.[0].rootPath)

[<Fact>]
let ``upsert rejects relative path`` () =
    match WorkspaceLocalMapping.upsert { entries = [] } "home" "relative" with
    | Ok _ -> Assert.Fail("Expected invalid_path")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``tryGitRoot finds repo when path contains .git`` () =
    let dir =
        Path.Combine(
            Path.GetTempPath(),
            $"gambol-map-git-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    Directory.CreateDirectory(Path.Combine(dir, ".git")) |> ignore
    match WorkspaceLocalMapping.tryGitRoot dir with
    | Error err -> Assert.Fail(err)
    | Ok root -> Assert.Equal(Path.GetFullPath(dir), root)

[<Fact>]
let ``tryGitRoot accepts .git directory itself`` () =
    let dir =
        Path.Combine(
            Path.GetTempPath(),
            $"gambol-map-gitdir-{Guid.NewGuid()}")
    let gitDir = Path.Combine(dir, ".git")
    Directory.CreateDirectory(gitDir) |> ignore
    match WorkspaceLocalMapping.tryGitRoot gitDir with
    | Error err -> Assert.Fail(err)
    | Ok root -> Assert.Equal(Path.GetFullPath(dir), root)

[<Fact>]
let ``tryGitRoot rejects non-repo folder`` () =
    let dir =
        Path.Combine(
            Path.GetTempPath(),
            $"gambol-map-nogit-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    match WorkspaceLocalMapping.tryGitRoot dir with
    | Ok _ -> Assert.Fail("Expected not_a_git_repo")
    | Error err -> Assert.Equal("not_a_git_repo", err)

