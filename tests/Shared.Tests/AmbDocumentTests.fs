module Gambol.Shared.Tests.AmbDocumentTests

open System
open Xunit
open Gambol.Shared

let private owned (ids: NodeId list) =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private graphWithDocument (childNodes: Node list) : Graph * NodeId =
    let graph0 = Graph.create ()
    let docId = NodeId.New()
    let docNode =
        { id = docId
          text = "doc"
          name = Filename.Ok "notes"
          children = []
          cssClasses = CssClass.empty
          owner = graph0.root
          kind = Special File
          updateTime = NodeUpdateTime.missing }
    let graph1 =
        graph0.nodes
        |> Map.add docId docNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let graph2 =
        childNodes
        |> List.fold
            (fun graph node ->
                graph.nodes |> Map.add node.id node |> fun nodes -> { graph with nodes = nodes })
            graph1
    let childIds = childNodes |> List.map (fun node -> node.id)
    Graph.replace docId 0 [] (owned childIds) graph2
    |> function
        | Ok graph -> graph, docId
        | Error msg -> failwith msg

let private childTexts (graph: Graph) (docId: NodeId) : string list =
    graph.nodes.[docId].children
    |> List.map (fun c -> graph.nodes.[c.id].text)

[<Fact>]
let ``write unreferenced node uses plain line without stable id`` () =
    let nodeId = NodeId.New()
    let node =
        { id = nodeId
          text = "hello"
          name = Filename.Empty
          children = []
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    let graph, docId = graphWithDocument [ node ]
    let text =
        AmbDocument.write graph docId
        |> function
            | Ok s -> s
            | Error msg -> failwith msg
    Assert.Equal("hello" + Environment.NewLine, text)
    Assert.DoesNotContain("^" + AmbDocument.formatStableId nodeId, text)

[<Fact>]
let ``write referenced node uses caret stable id on ref line`` () =
    let sharedId = NodeId.New()
    let parentId = NodeId.New()
    let parent =
        { id = parentId
          text = "holder"
          name = Filename.Empty
          children = [ { ref = Ownership.Ref; id = sharedId } ]
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    let shared =
        { id = sharedId
          text = "hello"
          name = Filename.Empty
          children = []
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    let graph, docId = graphWithDocument [ parent; shared ]
    let text =
        AmbDocument.write graph docId
        |> function
            | Ok s -> s
            | Error msg -> failwith msg
    let sid = AmbDocument.formatStableId sharedId
    Assert.Contains("-> ^" + sid, text)
    Assert.Contains("^" + sid + " hello", text)
    Assert.StartsWith("holder", text)

[<Fact>]
let ``write unreferenced named node uses plain body only`` () =
    let nodeId = NodeId.New()
    let node =
        { id = nodeId
          text = "body text"
          name = Filename.Ok "anchor"
          children = []
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    let graph, docId = graphWithDocument [ node ]
    let text =
        AmbDocument.write graph docId
        |> function
            | Ok s -> s
            | Error msg -> failwith msg
    let sid = AmbDocument.formatStableId nodeId
    Assert.Equal("body text" + Environment.NewLine, text)
    Assert.DoesNotContain("^" + sid, text)

[<Fact>]
let ``write referenced named node uses caret stable id and tab before body`` () =
    let sharedId = NodeId.New()
    let parentId = NodeId.New()
    let parent =
        { id = parentId
          text = "holder"
          name = Filename.Empty
          children = [ { ref = Ownership.Ref; id = sharedId } ]
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    let shared =
        { id = sharedId
          text = "body text"
          name = Filename.Ok "anchor"
          children = []
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    let graph, docId = graphWithDocument [ parent; shared ]
    let text =
        AmbDocument.write graph docId
        |> function
            | Ok s -> s
            | Error msg -> failwith msg
    let sid = AmbDocument.formatStableId sharedId
    Assert.Contains("-> ^" + sid, text)
    Assert.Contains("^" + sid + " anchor\tbody text", text)

[<Fact>]
let ``read same-document ref resolves stable id`` () =
    let sharedId = NodeId.New()
    let parentId = NodeId.New()
    let parent =
        { id = parentId
          text = "parent"
          name = Filename.Empty
          children = []
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    let shared =
        { id = sharedId
          text = "shared"
          name = Filename.Empty
          children = []
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    let graph, docId = graphWithDocument [ parent ]
    let graph =
        graph.nodes |> Map.add sharedId shared |> fun nodes -> { graph with nodes = nodes }
    let sid = AmbDocument.formatStableId sharedId
    let outline =
        "^" + AmbDocument.formatStableId parentId + " parent" + Environment.NewLine
        + "\t-> ^" + sid + Environment.NewLine
        + "^" + sid + " shared" + Environment.NewLine
    let result =
        AmbDocument.read outline docId graph
        |> function
            | Ok r -> r
            | Error msg -> failwith msg
    let parentNode = result.nodes.[parentId]
    Assert.Equal(1, parentNode.children.Length)
    Assert.Equal(sharedId, parentNode.children.[0].id)
    Assert.Equal(Ownership.Ref, parentNode.children.[0].ref)

[<Fact>]
let ``read cross-document ref resolves against context graph`` () =
    let localId = NodeId.New()
    let externalId = NodeId.New()
    let local =
        { id = localId
          text = "local"
          name = Filename.Empty
          children = []
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    let external =
        { id = externalId
          text = "external"
          name = Filename.Ok "target"
          children = []
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    let graph, docId = graphWithDocument [ local ]
    let graph =
        graph.nodes |> Map.add externalId external |> fun nodes -> { graph with nodes = nodes }
    let sid = AmbDocument.formatStableId externalId
    let outline =
        "^" + AmbDocument.formatStableId localId + " local" + Environment.NewLine
        + "\t-> //peer.txt^" + sid + Environment.NewLine
    let result =
        AmbDocument.read outline docId graph
        |> function
            | Ok r -> r
            | Error msg -> failwith msg
    let localNode = result.nodes.[localId]
    Assert.Equal(1, localNode.children.Length)
    Assert.Equal(externalId, localNode.children.[0].id)
    Assert.Equal(Ownership.Ref, localNode.children.[0].ref)

[<Fact>]
let ``read owner line with caret stable id`` () =
    let nodeId = NodeId.New()
    let sid = AmbDocument.formatStableId nodeId
    let graph, docId = graphWithDocument []
    let outline = "^" + sid + " plain body" + Environment.NewLine
    let result =
        AmbDocument.read outline docId graph
        |> function
            | Ok r -> r
            | Error msg -> failwith msg
    Assert.Equal("plain body", result.nodes.[nodeId].text)
    Assert.Equal(1, result.nodes.[docId].children.Length)

[<Fact>]
let ``round-trip preserves stable ids and tree shape`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a =
        { id = aId
          text = "alpha"
          name = Filename.Empty
          children = []
          cssClasses = CssClass.empty
          owner = Graph.rootId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    let b =
        { id = bId
          text = "beta"
          name = Filename.Empty
          children = []
          cssClasses = CssClass.empty
          owner = aId
          kind = Normal
          updateTime = NodeUpdateTime.missing }
    let graph, docId = graphWithDocument [ { a with children = owned [ bId ] }; b ]
    let written =
        AmbDocument.write graph docId
        |> function
            | Ok s -> s
            | Error msg -> failwith msg
    let result =
        AmbDocument.read written docId graph
        |> function
            | Ok r -> r
            | Error msg -> failwith msg
    Assert.Equal(aId, result.nodes.[docId].children.[0].id)
    Assert.Equal(bId, result.nodes.[aId].children.[0].id)
    Assert.Equal("alpha", result.nodes.[aId].text)
    Assert.Equal("beta", result.nodes.[bId].text)
    let rewritten =
        AmbDocument.write { graph with nodes = result.nodes } docId
        |> function
            | Ok s -> s
            | Error msg -> failwith msg
    Assert.Equal(
        AmbDocument.normalizeForCompare written,
        AmbDocument.normalizeForCompare rewritten
    )
