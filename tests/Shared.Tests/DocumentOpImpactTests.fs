module DocumentOpImpactTests

open System
open Gambol.Shared
open Xunit

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error -> failwith $"{label}: {error}"

let private owned ids =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private specialNode id kind name owner =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private normalNode id text owner =
    Node.Create(id, text = text, owner = owner)

let private applyOps graph ops =
    ops
    |> List.fold
        (fun state op ->
            match Op.apply op state with
            | ApplyResult.Changed next
            | ApplyResult.Unchanged next -> next
            | ApplyResult.Invalid(_, error) -> failwith error)
        { graph = graph; history = History.empty; revision = Revision.Zero }
    |> _.graph

let private graphWithDocuments () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirAId = NodeId.New()
    let dirBId = NodeId.New()
    let fileAId = NodeId.New()
    let fileBId = NodeId.New()
    let bodyAId = NodeId.New()
    let bodyBId = NodeId.New()
    let nodes =
        graph0.nodes
        |> Map.add wsId (specialNode wsId Workspace "home" Graph.workspacesId)
        |> Map.add dirAId (specialNode dirAId Directory "a" wsId)
        |> Map.add dirBId (specialNode dirBId Directory "b" wsId)
        |> Map.add fileAId (specialNode fileAId File "a.txt" dirAId)
        |> Map.add fileBId (specialNode fileBId File "b.txt" dirAId)
        |> Map.add bodyAId (normalNode bodyAId "alpha" fileAId)
        |> Map.add bodyBId (normalNode bodyBId "beta" fileBId)
    let graph1 = Graph.fromNodes graph0.root nodes
    let attach parent ids graph =
        Graph.replace parent 0 [] (owned ids) graph |> requireOk "attach"
    let graph =
        graph1
        |> attach Graph.workspacesId [ wsId ]
        |> attach wsId [ dirAId; dirBId ]
        |> attach dirAId [ fileAId; fileBId ]
        |> attach fileAId [ bodyAId ]
        |> attach fileBId [ bodyBId ]
    graph, wsId, dirAId, dirBId, fileAId, fileBId, bodyAId, bodyBId

let private affectedByOps preGraph postGraph ops =
    let moveIds =
        DocumentPathMove.planPathMovesBetweenGraphs preGraph postGraph
        |> List.map _.nodeId
    DocumentOpImpact.documentRootsAffectedByOps preGraph postGraph ops moveIds

let private assertParity preGraph postGraph ops =
    let moveIds =
        DocumentPathMove.planPathMovesBetweenGraphs preGraph postGraph
        |> List.map _.nodeId
    let expected =
        DocumentPartition.documentRootsAffectedByGraphChange
            preGraph
            postGraph
            moveIds
    let actual =
        DocumentOpImpact.documentRootsAffectedByOps preGraph postGraph ops moveIds
    Assert.Equal<Set<NodeId>>(expected, actual)

[<Fact>]
let ``SetText and SetClasses affect only their containing document`` () =
    let graph, _, _, _, fileAId, fileBId, bodyAId, _ = graphWithDocuments ()
    let ops =
        [ Op.SetText(bodyAId, "alpha", "ALPHA")
          Op.SetClasses(bodyAId, CssClass.empty, CssClass.ofList [ "edited" ]) ]
    let post = applyOps graph ops
    let affected = affectedByOps graph post ops
    assertParity graph post ops
    Assert.Equal<Set<NodeId>>(Set.singleton fileAId, affected)
    Assert.DoesNotContain(fileBId, affected)

[<Fact>]
let ``SetName on nested root affects itself and parent including path moves`` () =
    let graph, _, dirAId, _, fileAId, fileBId, _, _ = graphWithDocuments ()
    let ops = [ Op.SetName(fileAId, "a.txt", "renamed.txt") ]
    let post = applyOps graph ops
    let affected = affectedByOps graph post ops
    assertParity graph post ops
    Assert.Equal<Set<NodeId>>(Set.ofList [ dirAId; fileAId ], affected)
    Assert.DoesNotContain(fileBId, affected)

[<Fact>]
let ``NewNode and Replace append affect only the receiving document`` () =
    let graph, _, dirAId, _, fileAId, fileBId, _, _ = graphWithDocuments ()
    let childId = NodeId.New()
    let child = { ref = Ownership.Owner; id = childId }
    let index = graph.nodes.[fileAId].children.Length
    let ops =
        [ Op.NewNode(childId, "new")
          Op.Replace(fileAId, index, [], [ child ]) ]
    let post = applyOps graph ops
    let affected = affectedByOps graph post ops
    assertParity graph post ops
    Assert.Equal<Set<NodeId>>(Set.ofList [ dirAId; fileAId ], affected)
    Assert.DoesNotContain(fileBId, affected)

[<Fact>]
let ``Replace reparent affects old and new packages and moved root`` () =
    let graph, wsId, dirAId, dirBId, fileAId, fileBId, _, _ = graphWithDocuments ()
    let child = { ref = Ownership.Owner; id = fileAId }
    let ops =
        [ Op.Replace(dirAId, 0, [ child ], [])
          Op.Replace(dirBId, 0, [], [ child ]) ]
    let post = applyOps graph ops
    let affected = affectedByOps graph post ops
    assertParity graph post ops
    Assert.Equal<Set<NodeId>>(
        Set.ofList [ wsId; dirAId; dirBId; fileAId ],
        affected)
    Assert.DoesNotContain(fileBId, affected)

[<Fact>]
let ``NewSpecialNode and Replace affect new root and parent package`` () =
    let graph, wsId, dirAId, _, _, fileBId, _, _ = graphWithDocuments ()
    let fileId = NodeId.New()
    let child = { ref = Ownership.Owner; id = fileId }
    let index = graph.nodes.[dirAId].children.Length
    let ops =
        [ Op.NewSpecialNode(fileId, File, "new.txt")
          Op.Replace(dirAId, index, [], [ child ]) ]
    let post = applyOps graph ops
    let affected = affectedByOps graph post ops
    assertParity graph post ops
    Assert.Equal<Set<NodeId>>(Set.ofList [ wsId; dirAId; fileId ], affected)
    Assert.DoesNotContain(fileBId, affected)

[<Fact>]
let ``SetDocumentState includes the containing parent and filters unwritable root`` () =
    let graph, _, dirAId, _, fileAId, fileBId, _, _ = graphWithDocuments ()
    let ops = [ Op.SetDocumentState(fileAId, Current, NoServerFile) ]
    let post = applyOps graph ops
    let affected = affectedByOps graph post ops
    assertParity graph post ops
    Assert.Equal<Set<NodeId>>(Set.singleton dirAId, affected)
    Assert.DoesNotContain(fileAId, affected)
    Assert.DoesNotContain(fileBId, affected)

[<Fact>]
let ``SetUpdateTime has no document artifact impact`` () =
    let graph, _, _, _, fileAId, _, _, _ = graphWithDocuments ()
    let oldTime = graph.nodes.[fileAId].updateTime
    let newTime = DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc)
    let ops = [ Op.SetUpdateTime(fileAId, oldTime, newTime) ]
    let post = applyOps graph ops
    let affected = affectedByOps graph post ops
    Assert.Empty(affected)

[<Fact>]
let ``inverse SetText operation has the same scoped impact`` () =
    let graph, _, _, _, fileAId, fileBId, bodyAId, _ = graphWithDocuments ()
    let forward = [ Op.SetText(bodyAId, "alpha", "ALPHA") ]
    let changed = applyOps graph forward
    let inverse = [ Op.SetText(bodyAId, "ALPHA", "alpha") ]
    let restored = applyOps changed inverse
    let affected = affectedByOps changed restored inverse
    assertParity changed restored inverse
    Assert.Equal<Set<NodeId>>(Set.singleton fileAId, affected)
    Assert.DoesNotContain(fileBId, affected)
