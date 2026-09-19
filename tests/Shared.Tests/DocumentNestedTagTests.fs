module DocumentNestedTagTests

open Gambol.Shared
open Xunit

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private extracted (nodes: Node list) (rootId: NodeId) : Graph =
    let map =
        nodes
        |> List.map (fun n -> n.id, n)
        |> Map.ofList
    Graph.fromExtracted rootId map

[<Fact>]
let ``leaf node writes a single div line`` () =
    let id = NodeId.New()
    let node = Node.Create(id, text = "Hello")
    let graph = extracted [ node ] id
    Assert.Equal(None, graph.focus)
    let packed = DocumentNestedTag.writeExtract graph |> requireOk "write"
    Assert.Equal("<div>Hello</div>", packed)

[<Fact>]
let ``leaf Focus writes a single focus line`` () =
    let id = NodeId.New()
    let node = Node.Create(id, text = "Ask")
    let graph = Graph.withFocus id (extracted [ node ] id)
    let packed = DocumentNestedTag.writeExtract graph |> requireOk "write"
    Assert.Equal("<focus>Ask</focus>", packed)

[<Fact>]
let ``nested node writes text then child tags`` () =
    let childId = NodeId.New()
    let parentId = NodeId.New()
    let child = Node.Create(childId, text = "Child")
    let parent =
        Node.Create(
            parentId,
            text = "Parent",
            children = [ ChildNode.owner childId ])
    let graph = extracted [ parent; child ] parentId
    let packed = DocumentNestedTag.writeExtract graph |> requireOk "write"
    Assert.Equal("<div>Parent<div>Child</div></div>", packed)

[<Fact>]
let ``nested Focus wraps children in focus tags`` () =
    let childId = NodeId.New()
    let parentId = NodeId.New()
    let child = Node.Create(childId, text = "Child")
    let parent =
        Node.Create(
            parentId,
            text = "Parent",
            children = [ ChildNode.owner childId ])
    let graph =
        Graph.withFocus parentId (extracted [ parent; child ] parentId)
    let packed = DocumentNestedTag.writeExtract graph |> requireOk "write"
    Assert.Equal("<focus>Parent<div>Child</div></focus>", packed)

[<Fact>]
let ``write walks Ref children supplied in the extract`` () =
    let ownedId = NodeId.New()
    let refId = NodeId.New()
    let parentId = NodeId.New()
    let owned = Node.Create(ownedId, text = "Owned")
    let referred = Node.Create(refId, text = "Referred")
    let parent =
        Node.Create(
            parentId,
            text = "Zoom",
            children =
                [ ChildNode.owner ownedId
                  ChildNode.reference refId ])
    let graph = extracted [ parent; owned; referred ] parentId
    let packed = DocumentNestedTag.writeExtract graph |> requireOk "write"
    Assert.Equal("<div>Zoom<div>Owned</div><div>Referred</div></div>", packed)

[<Fact>]
let ``write omits a child id missing from the extract`` () =
    let missingId = NodeId.New()
    let parentId = NodeId.New()
    let parent =
        Node.Create(
            parentId,
            text = "Zoom",
            children = [ ChildNode.reference missingId ])
    let graph = extracted [ parent ] parentId
    let packed = DocumentNestedTag.writeExtract graph |> requireOk "write"
    Assert.Equal("<div>Zoom</div>", packed)

[<Fact>]
let ``round-trip parse recovers the child tree`` () =
    let childId = NodeId.New()
    let parentId = NodeId.New()
    let child = Node.Create(childId, text = "Child")
    let parent =
        Node.Create(
            parentId,
            text = "Parent",
            children = [ ChildNode.owner childId ])
    let graph =
        Graph.withFocus childId (extracted [ parent; child ] parentId)
    let packed = DocumentNestedTag.writeExtract graph |> requireOk "write"
    Assert.Equal("<div>Parent<focus>Child</focus></div>", packed)
    let tree =
        DocumentNestedTag.parseExtract packed |> requireOk "parse"
    let childTree: DocumentNestedTag.NestedTagNode =
        { tag = "focus"
          text = "Child"
          children = [] }
    let expected: DocumentNestedTag.NestedTagNode =
        { tag = "div"
          text = "Parent"
          children = [ childTree ] }
    Assert.Equal(expected, tree)

[<Fact>]
let ``round-trip escapes Node text that contains tags`` () =
    let id = NodeId.New()
    let node = Node.Create(id, text = "A <div>B")
    let graph = extracted [ node ] id
    let packed = DocumentNestedTag.writeExtract graph |> requireOk "write"
    Assert.Contains("&lt;", packed)
    let tree =
        DocumentNestedTag.parseExtract packed |> requireOk "parse"
    Assert.Equal("div", tree.tag)
    Assert.Equal("A <div>B", tree.text)
    Assert.Equal<DocumentNestedTag.NestedTagNode list>([], tree.children)

[<Fact>]
let ``parse rejects residue after a complete tree`` () =
    match DocumentNestedTag.parseExtract "<div>Hello</div>extra" with
    | Error msg -> Assert.Equal(DocumentNestedTag.residue, msg)
    | Ok _ -> Assert.Fail("residue must refuse")
    match DocumentNestedTag.parseExtract "<div>A</div><div>B</div>" with
    | Error msg -> Assert.Equal(DocumentNestedTag.residue, msg)
    | Ok _ -> Assert.Fail("second root must refuse")

[<Fact>]
let ``parse rejects an incomplete tree`` () =
    match DocumentNestedTag.parseExtract "<div>Hello" with
    | Error msg -> Assert.Equal(DocumentNestedTag.incomplete, msg)
    | Ok _ -> Assert.Fail("unclosed tag must refuse")
    match DocumentNestedTag.parseExtract "<div>Hello</focus>" with
    | Error msg -> Assert.Equal(DocumentNestedTag.incomplete, msg)
    | Ok _ -> Assert.Fail("mismatched close must refuse")
