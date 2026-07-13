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
        Node.Create(
            docId,
            text = "doc",
            name = Filename.Ok "notes",
            owner = graph0.root,
            kind = Special File)
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

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private nl = Environment.NewLine

let private ownerLine (nodeId: NodeId) (body: string) =
    "^" + AmbDocument.formatStableId nodeId + " " + body + nl

let private refLine (nodeId: NodeId) =
    "-> ^" + AmbDocument.formatStableId nodeId + nl

[<Fact>]
let ``write unreferenced node uses plain line without stable id`` () =
    let nodeId = NodeId.New()
    let node =
        Node.Create(nodeId, text = "hello")
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
        Node.Create(
            parentId,
            text = "holder",
            children = [ { ref = Ownership.Ref; id = sharedId } ])
    let shared =
        Node.Create(sharedId, text = "hello")
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
        Node.Create(nodeId, text = "body text", name = Filename.Ok "anchor")
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
        Node.Create(
            parentId,
            text = "holder",
            children = [ { ref = Ownership.Ref; id = sharedId } ])
    let shared =
        Node.Create(sharedId, text = "body text", name = Filename.Ok "anchor")
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
        Node.Create(parentId, text = "parent")
    let shared =
        Node.Create(sharedId, text = "shared")
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
        Node.Create(localId, text = "local")
    let external =
        Node.Create(externalId, text = "external", name = Filename.Ok "target")
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
let ``read missing cross-document ref creates Broken link stub`` () =
    let localId = NodeId.New()
    let missingId = NodeId.New()
    let local = Node.Create(localId, text = "local")
    let graph, docId = graphWithDocument [ local ]
    let sid = AmbDocument.formatStableId missingId
    let outline =
        "^" + AmbDocument.formatStableId localId + " local" + Environment.NewLine
        + "\t-> //gone.txt^" + sid + Environment.NewLine
    let result =
        AmbDocument.read outline docId graph
        |> function
            | Ok r -> r
            | Error msg -> failwith msg
    Assert.Equal(missingId, result.nodes.[localId].children.[0].id)
    Assert.Equal(Ownership.Ref, result.nodes.[localId].children.[0].ref)
    Assert.Equal("Broken link.", result.nodes.[missingId].text)

[<Fact>]
let ``read same-document dangling ref creates Broken link stub`` () =
    let missingId = NodeId.New()
    let graph, docId = graphWithDocument []
    let sid = AmbDocument.formatStableId missingId
    let outline = "-> ^" + sid + Environment.NewLine
    let result =
        AmbDocument.read outline docId graph
        |> function
            | Ok r -> r
            | Error msg -> failwith msg
    Assert.Equal(missingId, result.nodes.[docId].children.[0].id)
    Assert.Equal("Broken link.", result.nodes.[missingId].text)

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
let ``read caret text without stable id as plain line`` () =
    let graph, docId = graphWithDocument []
    let outline = "^ff" + Environment.NewLine
    let result =
        AmbDocument.read outline docId graph
        |> function
            | Ok r -> r
            | Error msg -> failwith msg
    Assert.Equal("^ff", result.nodes.[result.nodes.[docId].children.[0].id].text)
    Assert.Equal(1, result.nodes.[docId].children.Length)

[<Fact>]
let ``round-trip preserves caret-prefixed plain text`` () =
    let nodeId = NodeId.New()
    let node = Node.Create(nodeId, text = "^ff")
    let graph, docId = graphWithDocument [ node ]
    let written =
        AmbDocument.write graph docId
        |> function
            | Ok s -> s
            | Error msg -> failwith msg
    Assert.Contains("^" + AmbDocument.formatStableId nodeId, written)
    let result =
        AmbDocument.read written docId graph
        |> function
            | Ok r -> r
            | Error msg -> failwith msg
    Assert.Equal(nodeId, result.nodes.[docId].children.[0].id)
    Assert.Equal("^ff", result.nodes.[nodeId].text)

[<Fact>]
let ``round-trip preserves stable ids and tree shape`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a =
        Node.Create(aId, text = "alpha")
    let b =
        Node.Create(bId, text = "beta", owner = aId)
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

[<Fact>]
let ``reconcile owner text edit keeps stable id`` () =
    let aId = NodeId.New()
    let a = Node.Create(aId, text = "alpha")
    let graph, docId = graphWithDocument [ a ]
    let previous = ownerLine aId "alpha"
    let edited = ownerLine aId "ALPHA"
    let result =
        AmbReconcile.reconcile previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal(aId, result.nodes.[docId].children.Head.id)
    Assert.Equal("ALPHA", result.nodes.[aId].text)

[<Fact>]
let ``reconcile external tab reindent keeps owner id`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a = Node.Create(aId, text = "parent")
    let b = Node.Create(bId, text = "child")
    let graph, docId = graphWithDocument [ a; b ]
    let previous = ownerLine aId "parent" + ownerLine bId "child"
    let edited = ownerLine aId "parent" + "\t" + ownerLine bId "child"
    let result =
        AmbReconcile.reconcile previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal(aId, result.nodes.[docId].children.Head.id)
    Assert.Equal(bId, result.nodes.[aId].children.Head.id)
    Assert.Equal("child", result.nodes.[bId].text)

[<Fact>]
let ``reconcile plain line add mints new id`` () =
    let aId = NodeId.New()
    let a = Node.Create(aId, text = "alpha")
    let graph, docId = graphWithDocument [ a ]
    let previous = "alpha" + nl
    let edited = "alpha" + nl + "gamma" + nl
    let result =
        AmbReconcile.reconcile previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal(2, result.nodes.[docId].children.Length)
    Assert.Equal(aId, result.nodes.[docId].children.Head.id)
    let gammaId = result.nodes.[docId].children.[1].id
    Assert.NotEqual(aId, gammaId)
    Assert.Equal("gamma", result.nodes.[gammaId].text)

[<Fact>]
let ``reconcile plain line delete drops node`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a = Node.Create(aId, text = "alpha")
    let b = Node.Create(bId, text = "beta")
    let graph, docId = graphWithDocument [ a; b ]
    let previous = "alpha" + nl + "beta" + nl
    let edited = "alpha" + nl
    let result =
        AmbReconcile.reconcile previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal(1, result.nodes.[docId].children.Length)
    Assert.Equal(aId, result.nodes.[docId].children.Head.id)

[<Fact>]
let ``reconcile ref line stable across reorder`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a = Node.Create(aId, text = "alpha")
    let b = Node.Create(bId, text = "beta")
    let graph0, docId = graphWithDocument [ a; b ]
    let graph =
        Graph.replace
            docId
            0
            []
            [ { ref = Ownership.Ref; id = aId }
              { ref = Ownership.Ref; id = bId } ]
            graph0
        |> requireOk "replace refs"
    let previous =
        AmbDocument.write graph docId |> requireOk "write previous"
    let edited =
        refLine bId + refLine aId
    let result =
        AmbReconcile.reconcile previous graph docId edited
        |> requireOk "reconcile"
    let kids = result.nodes.[docId].children
    Assert.Equal(2, kids.Length)
    Assert.Equal(Ownership.Ref, kids.[0].ref)
    Assert.Equal(Ownership.Ref, kids.[1].ref)
    Assert.Equal(bId, kids.[0].id)
    Assert.Equal(aId, kids.[1].id)
