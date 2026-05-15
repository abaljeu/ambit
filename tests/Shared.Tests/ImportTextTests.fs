module Gambol.Shared.Tests.ImportTextTests

open Xunit
open Gambol.Shared

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

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
    | Error err -> Assert.Equal("import text is empty", err)

[<Fact>]
let ``tryFindFirstFileReference extracts first trimmed reference`` () =
    let result = ImportText.tryFindFirstFileReference "import [[ ./doc/file.md ]] now"

    Assert.Equal(Ok "./doc/file.md", result)

[<Fact>]
let ``tryFindFirstFileReference rejects missing or empty references`` () =
    Assert.Equal(Error "file reference not found", ImportText.tryFindFirstFileReference "none")
    Assert.Equal(Error "file reference is empty", ImportText.tryFindFirstFileReference "[[ ]]")

[<Fact>]
let ``DesktopImportPackage serializes round-trip`` () =
    let package = ImportText.buildPackage "note.txt" "parent\n\tchild" |> requirePackage
    let json = Enc.toString 0 (Serialization.encodeDesktopImportPackage package)

    match Dec.fromString Serialization.decodeDesktopImportPackage json with
    | Error err -> failwith $"Decode failed: {err}"
    | Ok decoded ->
        Assert.Equal(package.sourcePath, decoded.sourcePath)
        Assert.Equal<NodeId list>(package.topLevelIds, decoded.topLevelIds)
        Assert.Equal<Op list>(package.ops, decoded.ops)
