module Gambol.Shared.Tests.ImportTextTests

open Xunit
open Gambol.Shared

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private owned = ChildNode.owners

let private requirePackage = function
    | Ok package -> package
    | Error err -> failwith $"Expected import package: {err}"

let private newNodeTexts (ops: Op list) : string list =
    ops
    |> List.choose (function
        | Op.NewNode(_, text) -> Some text
        | _ -> None)

let private replaceOps (ops: Op list) : Op list =
    ops
    |> List.choose (function
        | Op.Replace _ as op -> Some op
        | _ -> None)

[<Fact>]
let ``buildPackage imports single line`` () =
    let package = ImportText.buildPackage "note.txt" "alpha" |> requirePackage

    Assert.Equal("note.txt", package.sourcePath)
    Assert.Equal(1, package.topLevelIds.Length)
    Assert.Equal<string list>(["alpha"], newNodeTexts package.ops)
    Assert.Empty(replaceOps package.ops)

[<Fact>]
let ``buildPackage imports multi-line siblings`` () =
    let package = ImportText.buildPackage "note.txt" "alpha\nbeta" |> requirePackage

    Assert.Equal(2, package.topLevelIds.Length)
    Assert.Equal<string list>(["alpha"; "beta"], newNodeTexts package.ops)
    Assert.Empty(replaceOps package.ops)

[<Fact>]
let ``buildPackage imports tab-indented child`` () =
    let package = ImportText.buildPackage "note.txt" "parent\n\tchild" |> requirePackage
    let parentId = package.topLevelIds.[0]
    let childId =
        package.ops
        |> List.choose (function
            | Op.NewNode(nodeId, "child") -> Some nodeId
            | _ -> None)
        |> List.exactlyOne

    Assert.Equal<string list>(["parent"; "child"], newNodeTexts package.ops)
    Assert.Equal<Op list>(
        [ Op.Replace(parentId, 0, [], owned [childId]) ],
        replaceOps package.ops)

[<Fact>]
let ``buildPackage rejects blank input`` () =
    match ImportText.buildPackage "blank.txt" "\r\n  \n" with
    | Ok _ -> failwith "Expected blank import to fail"
    | Error err -> Assert.Equal("directory import parser: text is empty", err)

[<Fact>]
let ``parseFirstFileReference extracts first trimmed reference`` () =
    let result = ImportText.parseFirstFileReference "import [[ ./doc/file.md ]] now"

    Assert.Equal(FileReference "./doc/file.md", result)

[<Fact>]
let ``parseFirstFileReference separates missing from invalid references`` () =
    Assert.Equal(NoFileReference, ImportText.parseFirstFileReference "none")
    Assert.Equal(InvalidFileReference, ImportText.parseFirstFileReference "[[ ]]")
    Assert.Equal(InvalidFileReference, ImportText.parseFirstFileReference "before [[open")

[<Fact>]
let ``tryFindFirstFileReference keeps result compatibility`` () =
    Assert.Equal(Ok "./doc/file.md", ImportText.tryFindFirstFileReference "[[./doc/file.md]]")
    Assert.Equal(Error "file reference not found", ImportText.tryFindFirstFileReference "none")
    Assert.Equal(Error "file reference is invalid", ImportText.tryFindFirstFileReference "[[ ]]")

[<Fact>]
let ``buildImportChange single line attaches to focus`` () =
    let package = ImportText.buildPackage "note.txt" "alpha" |> requirePackage
    let focusId = NodeId.New()
    let existing = owned [ NodeId.New() ]
    let change =
        ImportText.buildImportChange
            (Graph.create ())
            focusId
            existing
            package
            1
            (System.Guid.NewGuid())

    Assert.Equal(1, change.id)
    Assert.Equal<Op list>(
        package.ops
        @ [ Op.Replace(focusId, 0, existing, owned package.topLevelIds) ],
        change.ops)

[<Fact>]
let ``buildImportChange nested package attaches top level only`` () =
    let package = ImportText.buildPackage "note.txt" "parent\n\tchild" |> requirePackage
    let focusId = NodeId.New()
    let change =
        ImportText.buildImportChange
            (Graph.create ())
            focusId
            []
            package
            2
            (System.Guid.NewGuid())

    Assert.Equal<Op list>(
        package.ops @ [ Op.Replace(focusId, 0, [], owned package.topLevelIds) ],
        change.ops)

[<Fact>]
let ``DesktopImportPackage serializes round-trip`` () =
    let package =
        ImportText.buildPackage "note.txt" "parent\n\tchild"
        |> requirePackage
        |> fun p -> { p with isDirectory = true }

    let json = Enc.toString 0 (Serialization.encodeDesktopImportPackage package)

    match Dec.fromString Serialization.decodeDesktopImportPackage json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded ->
        Assert.Equal(package.sourcePath, decoded.sourcePath)
        Assert.True(decoded.isDirectory)
        Assert.Equal<NodeId list>(package.topLevelIds, decoded.topLevelIds)
        Assert.Equal<Op list>(package.ops, decoded.ops)

let private directoryPackage (text: string) =
    ImportText.buildPackage "dir" text
    |> requirePackage
    |> fun p -> { p with isDirectory = true }

let private normalNode (id: NodeId) text children =
    Node.Create(id, text = text, children = children)

let private specialFileNode (id: NodeId) (name: string) (owner: NodeId) =
    Node.Create(
        id,
        text = name,
        name = Filename.Ok name,
        owner = owner,
        kind = Special File)

let private graphWithFocus (focusId: NodeId) (focusChildren: ChildNode list) (extraNodes: Node list) =
    let graph0 = Graph.create ()
    let focus = normalNode focusId "dir" focusChildren

    let nodes =
        extraNodes
        |> List.fold (fun acc n -> Map.add n.id n acc) graph0.nodes
        |> Map.add focusId focus

    Graph.fromNodes graph0.root nodes

[<Fact>]
let ``build import marks unparsed file current before tree operations`` () =
    let package = ImportText.buildPackage "note.txt" "alpha" |> requirePackage
    let focusId = NodeId.New()
    let file =
        Node.Create(
            focusId,
            text = "note.txt",
            name = Filename.create "note.txt",
            kind = Special File,
            documentState = Unparsed)
    let graph0 = Graph.create ()
    let root = graph0.nodes.[Graph.rootId]
    let fileOccurrence = ChildNode.owner focusId
    let graph =
        graph0.nodes
        |> Map.add Graph.rootId { root with children = root.children @ [ fileOccurrence ] }
        |> Map.add focusId file
        |> Graph.fromNodes graph0.root
    let change =
        ImportText.buildImportChange graph focusId [] package 1 (System.Guid.NewGuid())
    Assert.Equal(
        Op.SetDocumentState(focusId, Unparsed, Current),
        change.ops.Head)
    Assert.Equal(
        Op.Replace(focusId, 0, [], owned package.topLevelIds),
        change.ops |> List.last)

[<Fact>]
let ``buildDirectoryMergeChange with empty existing adds all entries`` () =
    let package = directoryPackage "[[alpha.txt]] ts\n[[beta.txt]] ts"
    let focusId = NodeId.New()
    let graph = graphWithFocus focusId [] []

    let change =
        ImportText.buildDirectoryMergeChange graph focusId [] package 1 (System.Guid.NewGuid())

    Assert.Equal(2, package.topLevelIds.Length)

    Assert.Equal<Op list>(
        [ Op.Replace(focusId, 0, [], owned package.topLevelIds) ],
        replaceOps change.ops)

[<Fact>]
let ``buildDirectoryMergeChange skips existing Normal child by file reference`` () =
    let package = directoryPackage "[[readme.md]] ts\n[[beta.txt]] ts"
    let focusId = NodeId.New()
    let existingId = NodeId.New()
    let existing = owned [ existingId ]
    let graph =
        graphWithFocus focusId existing
            [ normalNode existingId "[[readme.md]] ts" [] ]

    let change =
        ImportText.buildDirectoryMergeChange graph focusId existing package 1 (System.Guid.NewGuid())

    let betaId = List.last package.topLevelIds

    Assert.Equal<Op list>(
        [ Op.Replace(focusId, 1, [], owned [ betaId ]) ],
        replaceOps change.ops)

[<Fact>]
let ``buildDirectoryMergeChange skips existing Special File child by name`` () =
    let package = directoryPackage "[[script.sh]] ts\n[[beta.txt]] ts"
    let focusId = NodeId.New()
    let existingId = NodeId.New()
    let existing = owned [ existingId ]
    let graph =
        graphWithFocus focusId existing
            [ specialFileNode existingId "script.sh" focusId ]

    let change =
        ImportText.buildDirectoryMergeChange graph focusId existing package 1 (System.Guid.NewGuid())

    let betaId = List.last package.topLevelIds

    Assert.Equal<Op list>(
        [ Op.Replace(focusId, 1, [], owned [ betaId ]) ],
        replaceOps change.ops)

[<Fact>]
let ``buildDirectoryMergeChange appends only new entries at end`` () =
    let package = directoryPackage "[[alpha.txt]] ts\n[[beta.txt]] ts"
    let focusId = NodeId.New()
    let existingId = NodeId.New()
    let existing = owned [ existingId ]
    let graph =
        graphWithFocus focusId existing
            [ normalNode existingId "[[alpha.txt]] ts" [] ]

    let change =
        ImportText.buildDirectoryMergeChange graph focusId existing package 1 (System.Guid.NewGuid())

    let betaId = List.last package.topLevelIds

    Assert.Equal<Op list>(
        [ Op.Replace(focusId, 1, [], owned [ betaId ]) ],
        replaceOps change.ops)

[<Fact>]
let ``DesktopFileStatusResponse serializes round-trip`` () =
    let source = System.DateTime(2024, 6, 1, 12, 0, 0, System.DateTimeKind.Utc)

    let response =
        { path = "note.txt"
          status = ExistingFile
          sourceModifiedUtc = Some source }

    let json = Enc.toString 0 (Serialization.encodeDesktopFileStatusResponse response)

    match Dec.fromString Serialization.decodeDesktopFileStatusResponse json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded ->
        Assert.Equal(response.path, decoded.path)
        Assert.Equal(response.status, decoded.status)
        Assert.Equal(response.sourceModifiedUtc, decoded.sourceModifiedUtc)
