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
let ``saveToFile and loadFromFile round-trip`` () =
    let tempRoot =
        Path.Combine(Path.GetTempPath(), "gambol-workspaces-" + Guid.NewGuid().ToString("N"))

    let path = Path.Combine(tempRoot, "workspace-mappings.json")

    try
        let input =
            { entries =
                [ { label = "main"
                    rootPath = "C:\\repo" } ] }

        match WorkspaceLocalMapping.saveToFile path input with
        | Error err -> Assert.Fail($"saveToFile failed: {err}")
        | Ok () -> ()

        match WorkspaceLocalMapping.loadFromFile path with
        | Error err -> Assert.Fail($"loadFromFile failed: {err}")
        | Ok loaded ->
            Assert.Equal(input.entries.Length, loaded.entries.Length)
            Assert.Equal(input.entries.Head.label, loaded.entries.Head.label)
            Assert.Equal(input.entries.Head.rootPath, loaded.entries.Head.rootPath)
    finally
        if Directory.Exists(tempRoot) then
            Directory.Delete(tempRoot, true)
