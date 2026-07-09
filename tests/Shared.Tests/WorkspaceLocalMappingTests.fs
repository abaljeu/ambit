module Gambol.Shared.Tests.WorkspaceLocalMappingTests

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
let ``encode roundtrips through decode`` () =
    let original =
        { entries =
            [ { label = "home"; rootPath = "D:\\dev\\myproject" }
              { label = "docs"; rootPath = "C:\\Users\\me\\docs" } ] }

    let json = WorkspaceLocalMapping.encode original
    let decoded = decodeOrFail json
    Assert.Equal(original.entries.Length, decoded.entries.Length)
    Assert.Equal(original.entries.[0].label, decoded.entries.[0].label)
    Assert.Equal(original.entries.[0].rootPath, decoded.entries.[0].rootPath)

[<Fact>]
let ``saveToFile writes readable mappings`` () =
    let path =
        System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "gambol-mapping-save-" + System.Guid.NewGuid().ToString("N") + ".json")

    let mappings =
        { entries = [ { label = "home"; rootPath = "C:\\repo" } ] }

    try
        match WorkspaceLocalMapping.saveToFile path mappings with
        | Error err -> Assert.Fail err
        | Ok () ->
            match WorkspaceLocalMapping.loadFromFile path with
            | Error err -> Assert.Fail err
            | Ok loaded -> Assert.Equal(mappings.entries.[0].label, loaded.entries.[0].label)
    finally
        try
            System.IO.File.Delete path
        with
        | :? System.IO.IOException -> ()

