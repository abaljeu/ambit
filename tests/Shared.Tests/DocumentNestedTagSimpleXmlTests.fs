module DocumentNestedTagSimpleXmlTests

open Fable.SimpleXml
open Fable.SimpleXml.Generator
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
let ``Generator writes a leaf div without declaration`` () =
    let packed =
        node "div" [] [ text "Hello" ]
        |> serializeXml
    Assert.Equal("<div>Hello</div>", packed)
    Assert.DoesNotContain("<?xml", packed)

[<Fact>]
let ``leaf node writes a single div line`` () =
    let id = NodeId.New()
    let node = Node.Create(id, text = "Hello")
    let graph = extracted [ node ] id
    Assert.Equal(None, graph.focus)
    let packed =
        DocumentNestedTagSimpleXml.writeExtract graph
        |> requireOk "write"
    Assert.Equal("<div>Hello</div>", packed)

[<Fact>]
let ``nested Focus writes text then child tags`` () =
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
    let packed =
        DocumentNestedTagSimpleXml.writeExtract graph
        |> requireOk "write"
    Assert.Equal("<focus>Parent<div>Child</div></focus>", packed)

[<Fact>]
let ``round-trip parse recovers nested Focus`` () =
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
    let packed =
        DocumentNestedTagSimpleXml.writeExtract graph
        |> requireOk "write"
    Assert.Equal("<div>Parent<focus>Child</focus></div>", packed)
    let tree =
        DocumentNestedTagSimpleXml.parseExtract packed
        |> requireOk "parse"
    let childTree: DocumentNestedTagSimpleXml.NestedTagNode =
        { tag = "focus"
          text = "Child"
          children = [] }
    let expected: DocumentNestedTagSimpleXml.NestedTagNode =
        { tag = "div"
          text = "Parent"
          children = [ childTree ] }
    Assert.Equal(expected, tree)

[<Fact>]
let ``write does not escape angle brackets unlike XElement`` () =
    let id = NodeId.New()
    let node = Node.Create(id, text = "A <div>B")
    let graph = extracted [ node ] id
    let simple =
        DocumentNestedTagSimpleXml.writeExtract graph
        |> requireOk "simple"
    let xlinq =
        DocumentNestedTag.writeExtract graph
        |> requireOk "xlinq"
    Assert.Equal("<div>A <div>B</div>", simple)
    Assert.Contains("&lt;", xlinq)
    Assert.DoesNotContain("&lt;", simple)

[<Fact>]
let ``parse cannot run on NET because Parsimmon is a Fable binding`` () =
    let ex =
        Assert.Throws<System.TypeInitializationException>(fun () ->
            DocumentNestedTagSimpleXml.parseExtract "<div>Hello</div>"
            |> ignore)
    Assert.NotNull(ex.InnerException)
    Assert.Contains("Fable bindings", ex.InnerException.Message)
