module Gambol.Shared.Tests.AmbDocumentTests

open System
open Xunit
open Gambol.Shared
open GraphChildMapHelpers

let private owned = ChildNode.owners

let private kids childMap parentId =
    Map.tryFind parentId childMap |> Option.defaultValue []

let private withRead (graph: Graph) nodes childMap =
    let pairs =
        childMap
        |> Map.toList
        |> List.filter (fun (id, _) ->
            id <> graph.root && not (Graph.isSystemFolderNode id))
    fromExisting graph nodes |> setChildMap pairs

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
        Graph.addDetachedNode docNode graph0
        |> appendKids graph0.root [ ChildNode.owner docId ]
    let graph2 = addDetachedMany childNodes graph1
    let childIds = childNodes |> List.map (fun node -> node.id)
    Graph.replace docId 0 [] (owned childIds) graph2
    |> function
        | Ok graph -> graph, docId
        | Error msg -> failwith msg

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
        Node.Create(parentId, text = "holder")
    let shared =
        Node.Create(sharedId, text = "hello")
    let graph0, docId = graphWithDocument [ parent; shared ]
    let graph =
        setChildren parentId [ ChildNode.reference sharedId ] graph0
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
        Node.Create(parentId, text = "holder")
    let shared =
        Node.Create(sharedId, text = "body text", name = Filename.Ok "anchor")
    let graph0, docId = graphWithDocument [ parent; shared ]
    let graph =
        setChildren parentId [ ChildNode.reference sharedId ] graph0
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
    let graph = Graph.addDetachedNode shared graph
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
    let parentKids = kids result.childMap parentId
    Assert.Equal(1, parentKids.Length)
    Assert.Equal(sharedId, parentKids.[0].id)
    Assert.Equal(Ownership.Ref, parentKids.[0].ref)

[<Fact>]
let ``read cross-document ref resolves against context graph`` () =
    let localId = NodeId.New()
    let externalId = NodeId.New()
    let local =
        Node.Create(localId, text = "local")
    let external =
        Node.Create(externalId, text = "external", name = Filename.Ok "target")
    let graph, docId = graphWithDocument [ local ]
    let graph = Graph.addDetachedNode external graph
    let sid = AmbDocument.formatStableId externalId
    let outline =
        "^" + AmbDocument.formatStableId localId + " local" + Environment.NewLine
        + "\t-> //peer.txt^" + sid + Environment.NewLine
    let result =
        AmbDocument.read outline docId graph
        |> function
            | Ok r -> r
            | Error msg -> failwith msg
    let localKids = kids result.childMap localId
    Assert.Equal(1, localKids.Length)
    Assert.Equal(externalId, localKids.[0].id)
    Assert.Equal(Ownership.Ref, localKids.[0].ref)

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
    Assert.Equal(missingId, (kids result.childMap localId).[0].id)
    Assert.Equal(Ownership.Ref, (kids result.childMap localId).[0].ref)
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
    Assert.Equal(missingId, (kids result.childMap docId).[0].id)
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
    Assert.Equal(1, (kids result.childMap docId).Length)

[<Fact>]
let ``read caret text without stable id as plain line`` () =
    let graph, docId = graphWithDocument []
    let outline = "^ff" + Environment.NewLine
    let result =
        AmbDocument.read outline docId graph
        |> function
            | Ok r -> r
            | Error msg -> failwith msg
    Assert.Equal(
        "^ff",
        result.nodes.[(kids result.childMap docId).[0].id].text)
    Assert.Equal(1, (kids result.childMap docId).Length)

[<Fact>]
let ``read ambiguous owner-link candidates keeps map order`` () =
    let graph, docId = graphWithDocument []
    let lowId = NodeId(Guid.Parse "00000000-0000-0000-0000-000000000001")
    let highId = NodeId(Guid.Parse "00000000-0000-0000-0000-000000000002")
    let low = Node.Create(lowId, text = "same", owner = docId)
    let high = Node.Create(highId, text = "same", owner = docId)
    let graph = addDetachedMany [ high; low ] graph

    let result =
        AmbDocument.read ("same" + Environment.NewLine) docId graph
        |> requireOk "read"

    Assert.Equal(lowId, (kids result.childMap docId).Head.id)

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
    Assert.Equal(nodeId, (kids result.childMap docId).[0].id)
    Assert.Equal("^ff", result.nodes.[nodeId].text)

[<Fact>]
let ``write owned nested File uses owner line not ref`` () =
    let graph0 = Graph.create ()
    let dirId = NodeId.New()
    let organizerId = NodeId.New()
    let fileId = NodeId.New()
    let dir =
        Node.Create(
            dirId,
            text = "docs",
            name = Filename.Ok "docs",
            owner = graph0.root,
            kind = Special Directory)
    let organizer =
        Node.Create(organizerId, text = "organizer", owner = dirId)
    let file =
        Node.Create(
            fileId,
            text = "already owned",
            name = Filename.Ok "present.txt",
            owner = organizerId,
            kind = Special File)
    let graph1 =
        addDetachedMany [ dir; organizer; file ] graph0
        |> appendKids graph0.root [ ChildNode.owner dirId ]
    let graph2 =
        Graph.replace dirId 0 [] (owned [ organizerId ]) graph1
        |> requireOk "place organizer"
    let graph3 =
        Graph.replace organizerId 0 [] (owned [ fileId ]) graph2
        |> requireOk "place file"
    let graph =
        setChildren fileId (owned [ NodeId.New() ]) graph3
    let written =
        AmbDocument.write graph dirId
        |> requireOk "write"
    let sid = AmbDocument.formatStableId fileId
    Assert.Contains("^" + sid + " present.txt\talready owned", written)
    Assert.DoesNotContain("-> ", written)
    let result =
        AmbDocument.read written dirId graph
        |> requireOk "read"
    let underOrganizer = kids result.childMap organizerId
    Assert.Equal(1, underOrganizer.Length)
    Assert.Equal(Ownership.Owner, underOrganizer.[0].ref)
    Assert.Equal(fileId, underOrganizer.[0].id)
    Assert.Equal(Special File, result.nodes.[fileId].kind)
    Assert.Equal(0, (kids result.childMap fileId).Length)

[<Fact>]
let ``round-trip preserves stable ids and tree shape`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let a =
        Node.Create(aId, text = "alpha")
    let b =
        Node.Create(bId, text = "beta", owner = aId)
    let graph0, docId = graphWithDocument [ a; b ]
    let graph = setChildren aId (owned [ bId ]) graph0
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
    Assert.Equal(aId, (kids result.childMap docId).[0].id)
    Assert.Equal(bId, (kids result.childMap aId).[0].id)
    Assert.Equal("alpha", result.nodes.[aId].text)
    Assert.Equal("beta", result.nodes.[bId].text)
    let rewritten =
        AmbDocument.write
            (withRead graph result.nodes result.childMap)
            docId
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
        AmbReconcile.reconcile OutlineLcs.diffTexts previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal(aId, (kids result.childMap docId).Head.id)
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
        AmbReconcile.reconcile OutlineLcs.diffTexts previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal(aId, (kids result.childMap docId).Head.id)
    Assert.Equal(bId, (kids result.childMap aId).Head.id)
    Assert.Equal("child", result.nodes.[bId].text)

[<Fact>]
let ``reconcile plain line add mints new id`` () =
    let aId = NodeId.New()
    let a = Node.Create(aId, text = "alpha")
    let graph, docId = graphWithDocument [ a ]
    let previous = "alpha" + nl
    let edited = "alpha" + nl + "gamma" + nl
    let result =
        AmbReconcile.reconcile OutlineLcs.diffTexts previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal(2, (kids result.childMap docId).Length)
    Assert.Equal(aId, (kids result.childMap docId).Head.id)
    let gammaId = (kids result.childMap docId).[1].id
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
        AmbReconcile.reconcile OutlineLcs.diffTexts previous graph docId edited
        |> requireOk "reconcile"
    Assert.Equal(1, (kids result.childMap docId).Length)
    Assert.Equal(aId, (kids result.childMap docId).Head.id)

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
            [ ChildNode.reference aId
              ChildNode.reference bId ]
            graph0
        |> requireOk "replace refs"
    let previous =
        AmbDocument.write graph docId |> requireOk "write previous"
    let edited =
        refLine bId + refLine aId
    let result =
        AmbReconcile.reconcile OutlineLcs.diffTexts previous graph docId edited
        |> requireOk "reconcile"
    let docKids = kids result.childMap docId
    Assert.Equal(2, docKids.Length)
    Assert.Equal(Ownership.Ref, docKids.[0].ref)
    Assert.Equal(Ownership.Ref, docKids.[1].ref)
    Assert.Equal(bId, docKids.[0].id)
    Assert.Equal(aId, docKids.[1].id)
