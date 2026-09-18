module Gambol.Shared.Tests.WorkspaceLocalMappingTests

open System
open System.IO
open Gambol.Shared
open Xunit

let private decodeOrFail (json: string) =
    match WorkspaceLocalMapping.decode json with
    | Ok mappings -> mappings
    | Error err -> failwith $"Expected decode success, got: {err}"

let private absRoot name =
    Path.Combine(Path.GetTempPath(), name) |> Path.GetFullPath

let private jsonEsc (s: string) = s.Replace("\\", "\\\\")

let private mappingJson (entries: (string * string) list) =
    let item (label, path) =
        sprintf """{"label":"%s","path":"%s"}""" label (jsonEsc path)
    let body = entries |> List.map item |> String.concat ","
    sprintf """{"workspaceMappings":[%s]}""" body

let private oneMap label root =
    { entries = [ { label = label; rootPath = root } ] }
    |> WorkspaceLocalMapping.toMap

[<Fact>]
let ``decode accepts empty object as empty mappings`` () =
    let mappings = decodeOrFail "{}"
    Assert.Empty(mappings.entries)

[<Fact>]
let ``decode rejects duplicate labels case-insensitive`` () =
    let json =
        mappingJson
            [ "Main", absRoot "gambol-map-a"
              "main", absRoot "gambol-map-b" ]

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
    let mappings = oneMap "main" (absRoot "gambol-map-main")

    match WorkspaceLocalMapping.resolvePath mappings "main" "../secret.txt" with
    | Ok _ -> Assert.Fail("Expected traversal rejection.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath rejects dotdot segment`` () =
    let mappings = oneMap "main" (absRoot "gambol-map-main")

    match WorkspaceLocalMapping.resolvePath mappings "main" "foo/../secret.txt" with
    | Ok _ -> Assert.Fail("Expected dotdot rejection.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath rejects empty segment from double slash`` () =
    let mappings = oneMap "main" (absRoot "gambol-map-main")

    match WorkspaceLocalMapping.resolvePath mappings "main" "foo//bar" with
    | Ok _ -> Assert.Fail("Expected empty-segment rejection.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath rejects absolute path as relative`` () =
    let mappings = oneMap "main" (absRoot "gambol-map-main")

    // Portable analogue of drive-relative `D:foo`: another root, not a child.
    match WorkspaceLocalMapping.resolvePath mappings "main" "/other" with
    | Ok _ -> Assert.Fail("Expected rooted-path rejection.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath rejects hash character`` () =
    let mappings = oneMap "main" (absRoot "gambol-map-main")

    match WorkspaceLocalMapping.resolvePath mappings "main" "foo#bar" with
    | Ok _ -> Assert.Fail("Expected hash rejection.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath rejects caret character`` () =
    let mappings = oneMap "main" (absRoot "gambol-map-main")

    match WorkspaceLocalMapping.resolvePath mappings "main" "foo^bar" with
    | Ok _ -> Assert.Fail("Expected caret rejection.")
    | Error err -> Assert.Equal("invalid_path", err)

[<Fact>]
let ``resolvePath accepts valid relative path`` () =
    let root = absRoot "gambol-map-main"
    let mappings = oneMap "main" root
    let rel = "src/lib/helpers.fs"

    match WorkspaceLocalMapping.resolvePath mappings "main" rel with
    | Error err -> Assert.Fail($"Expected success, got: {err}")
    | Ok resolved ->
        let expected = Path.Combine(root, rel) |> Path.GetFullPath
        Assert.Equal(expected, resolved)

[<Fact>]
let ``resolvePath accepts directory relative with trailing slash`` () =
    let root = absRoot "gambol-map-fambit"
    let mappings = oneMap "fambit" root

    // Desktop paths for directories are "//fambit/doc/" → relative "doc/".
    match WorkspaceLocalMapping.resolvePath mappings "fambit" "doc/" with
    | Error err -> Assert.Fail($"Expected success, got: {err}")
    | Ok resolved ->
        let expected = Path.Combine(root, "doc") |> Path.GetFullPath
        Assert.Equal(expected, resolved)

[<Fact>]
let ``resolvePath missing label is invalid_workspace`` () =
    let mappings =
        { entries = [] }
        |> WorkspaceLocalMapping.toMap
    match WorkspaceLocalMapping.resolvePath mappings "home" "" with
    | Ok _ -> Assert.Fail("expected missing mapping")
    | Error err -> Assert.Equal("invalid_workspace", err)

[<Fact>]
let ``tryFindMapping matches label case-insensitively`` () =
    let mappings =
        { entries =
            [ { label = "Alibre"; rootPath = "repo/Alibre" } ] }
        |> WorkspaceLocalMapping.toMap
    match WorkspaceLocalMapping.tryFindMapping mappings "Alibre" with
    | None -> Assert.Fail("expected mapping for Alibre")
    | Some m -> Assert.Equal("repo/Alibre", m.rootPath)
    match WorkspaceLocalMapping.tryFindMapping mappings "alibre" with
    | None -> Assert.Fail("expected mapping for alibre")
    | Some m -> Assert.Equal("Alibre", m.label)
    Assert.True(
        WorkspaceLocalMapping.tryFindMapping mappings "other"
        |> Option.isNone)

[<Fact>]
let ``missingMappingMessage names the workspace label`` () =
    Assert.Equal(
        "no local mapping for workspace 'home'",
        WorkspaceLocalMapping.missingMappingMessage "home")

[<Fact>]
let ``encode round-trips through decode`` () =
    let home = absRoot "gambol-map-home"
    let docs = absRoot "gambol-map-docs"
    let original =
        { entries =
            [ { label = "home"; rootPath = home }
              { label = "docs"; rootPath = docs } ] }
    let decoded = decodeOrFail (WorkspaceLocalMapping.encode original)
    Assert.Equal(2, decoded.entries.Length)
    Assert.Equal("home", decoded.entries.[0].label)
    Assert.Equal(home, decoded.entries.[0].rootPath)

[<Fact>]
let ``upsert replaces existing label case-insensitively`` () =
    let start =
        { entries = [ { label = "Home"; rootPath = absRoot "gambol-map-old" } ] }
    match WorkspaceLocalMapping.upsert start "home" (absRoot "gambol-map-new") with
    | Error err -> Assert.Fail(err)
    | Ok next ->
        Assert.Equal(1, next.entries.Length)
        Assert.Equal("home", next.entries.[0].label)
        Assert.Equal(absRoot "gambol-map-new", next.entries.[0].rootPath)

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
