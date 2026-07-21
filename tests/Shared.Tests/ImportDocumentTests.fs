module Gambol.Shared.Tests.ImportDocumentTests

open System
open Xunit
open Gambol.Shared

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

let private replaceOps (ops: Op list) : Op list =
    ops
    |> List.choose (function
        | Op.Replace _ as op -> Some op
        | _ -> None)

let private applyChange (graph: Graph) (change: Change) : Graph =
    let state = { graph = graph; history = History.empty; revision = Revision.Zero }

    change.ops
    |> List.fold
        (fun acc op ->
            match acc with
            | Error msg -> Error msg
            | Ok state ->
                match Op.apply op state with
                | ApplyResult.Changed next
                | ApplyResult.Unchanged next -> Ok next
                | ApplyResult.Invalid(_, error) -> Error error)
        (Ok state)
    |> function
        | Ok state -> state.graph
        | Error msg -> failwith msg

[<Fact>]
let ``buildFilePackage md heading produces nested replace ops`` () =
    let text =
        "# Agent Instructions"
        + Environment.NewLine
        + Environment.NewLine
        + "## Workspace Purpose"
        + Environment.NewLine
        + "- item"
        + Environment.NewLine

    let package =
        ImportDocument.buildFilePackage "//life/AGENTS.md" text
        |> requireOk "build package"

    Assert.Equal("//life/AGENTS.md", package.sourcePath)
    Assert.False(package.isDirectory)
    Assert.Equal(1, package.topLevelIds.Length)
    Assert.NotEmpty(replaceOps package.ops)

    let h1Id = package.topLevelIds.Head
    let h1Children =
        package.ops
        |> List.tryPick (function
            | Op.Replace(parentId, _, _, children) when parentId = h1Id ->
                Some children
            | _ -> None)

    match h1Children with
    | None -> failwith "expected nested children under h1"
    | Some children ->
        Assert.NotEmpty(children)
        let h2Id = children.Head.id
        let h2Children =
            package.ops
            |> List.tryPick (function
                | Op.Replace(parentId, _, _, grandchildren) when parentId = h2Id ->
                    Some grandchildren
                | _ -> None)

        match h2Children with
        | None -> failwith "expected list item under h2"
        | Some grandchildren -> Assert.NotEmpty(grandchildren)

[<Fact>]
let ``buildFilePackage md differs from paste flat siblings`` () =
    let text = "# one" + Environment.NewLine + "plain" + Environment.NewLine + "## two" + Environment.NewLine

    let documentPackage =
        ImportDocument.buildFilePackage "//life/notes.md" text
        |> requireOk "document package"

    let pastePackage =
        ImportText.buildPackage "//life/notes.md" text
        |> requireOk "paste package"

    Assert.Equal(1, documentPackage.topLevelIds.Length)
    Assert.True(documentPackage.topLevelIds.Length < pastePackage.topLevelIds.Length)
    Assert.True(replaceOps documentPackage.ops |> List.length > 0)

[<Fact>]
let ``buildFilePackage integrates with buildImportChange for md`` () =
    let text = "# section" + Environment.NewLine + "- item" + Environment.NewLine
    let package =
        ImportDocument.buildFilePackage "//life/AGENTS.md" text
        |> requireOk "build package"

    let focusId = NodeId.New()
    let graph0 = Graph.create ()
    let file =
        Node.Create(
            focusId,
            text = "AGENTS.md",
            name = Filename.create "AGENTS.md",
            owner = graph0.root,
            kind = Special File,
            documentState = Unparsed)

    let graph =
        graph0.nodes
        |> Map.add focusId file
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let change =
        ImportText.buildImportChange graph focusId [] package 1 (Guid.NewGuid())

    let after = applyChange graph change
    let sectionId = after.nodes.[focusId].children.Head.id

    Assert.Equal("section", after.nodes.[sectionId].text)
    Assert.Equal(1, after.nodes.[sectionId].children.Length)
    Assert.Equal("item", after.nodes.[after.nodes.[sectionId].children.Head.id].text)

[<Fact>]
let ``buildFilePackage rejects blank input`` () =
    match ImportDocument.buildFilePackage "//life/empty.md" "  \n" with
    | Ok _ -> failwith "expected blank import to fail"
    | Error err -> Assert.Equal("import text is empty", err)

[<Fact>]
let ``buildTextPackage Plain indent nesting under paste path`` () =
    let text = "alpha" + Environment.NewLine + "\tbeta" + Environment.NewLine

    let package =
        ImportDocument.buildTextPackage "//paste" text None
        |> requireOk "buildTextPackage"

    Assert.False(package.isDirectory)
    Assert.Equal(1, package.topLevelIds.Length)

    let alphaId = package.topLevelIds.Head
    let betaChildren =
        package.ops
        |> List.tryPick (function
            | Op.Replace(parentId, _, _, children) when parentId = alphaId ->
                Some children
            | _ -> None)

    match betaChildren with
    | None -> failwith "expected nested child under alpha"
    | Some children ->
        Assert.Equal(1, children.Length)

