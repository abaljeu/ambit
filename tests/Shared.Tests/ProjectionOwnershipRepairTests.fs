module Gambol.Shared.Tests.ProjectionOwnershipRepairTests

open System
open Xunit
open Gambol.Shared

let private created = Graph.create ()

let private canonicalNodes = GraphProjection.nodeRowsFromGraph created

let private canonicalChildren = GraphProjection.childRowsFromGraph created

let private protectedIds =
    [ Graph.rootId; Graph.trashId; Graph.workspacesId; Graph.systemId ]
    |> List.map _.Value

let private guid n =
    Guid.Parse($"10000000-0000-0000-0000-{n:D12}")

let private nodeRow id text =
    GraphProjection.nodeRowFromNode (Node.Create(NodeId id, text = text))

let private child parent ordinal childId ownership : GraphProjection.ChildPersistenceRow =
    { parentId = parent
      ordinal = ordinal
      childId = childId
      ownership = ownership }

let private run nodes children =
    ProjectionOwnershipRepair.plan
        Graph.rootId.Value
        protectedIds
        nodes
        children

let private expectOk nodes children =
    match run nodes children with
    | Error e ->
        Assert.Fail($"expected Ok plan, got Error: {e}")
        ProjectionOwnershipRepair.emptyPlan
    | Ok plan -> plan

[<Fact>]
let ``dual owner keeps ranked owner and demotes others to ref`` () =
    let aId, uId = guid 10, guid 11
    let nodes = canonicalNodes @ [ nodeRow aId "A"; nodeRow uId "U" ]
    let children =
        canonicalChildren
        @ [ child Graph.rootId.Value 3 uId Ownership.Owner
            child Graph.workspacesId.Value 0 aId Ownership.Owner
            child uId 0 aId Ownership.Owner ]
    let plan = expectOk nodes children
    Assert.Empty(plan.deleteNodeIds)
    Assert.Empty(plan.insertNodes)
    Assert.Empty(plan.insertChildren)
    let demoted =
        plan.ownershipUpdates
        |> List.tryFind (fun u ->
            u.parentId = uId && u.childId = aId && u.ownership = Ownership.Ref)
    Assert.True(demoted.IsSome, "expected U→A demoted to ref")
    Assert.DoesNotContain(
        plan.ownershipUpdates,
        fun u -> u.parentId = Graph.workspacesId.Value && u.childId = aId)
    let uToA = children |> List.find (fun r -> r.parentId = uId && r.childId = aId)
    Assert.Equal(0, uToA.ordinal)
    Assert.Equal(uId, demoted.Value.parentId)
    Assert.Equal(0, demoted.Value.ordinal)

[<Fact>]
let ``reachable ownerless promotes best existing ref not trash`` () =
    let aId = guid 20
    let nodes = canonicalNodes @ [ nodeRow aId "A" ]
    let children =
        canonicalChildren
        @ [ child Graph.workspacesId.Value 0 aId Ownership.Ref
            child Graph.trashId.Value 0 aId Ownership.Ref ]
    let plan = expectOk nodes children
    Assert.Empty(plan.deleteNodeIds)
    Assert.Contains(
        plan.ownershipUpdates,
        fun u ->
            u.parentId = Graph.workspacesId.Value
            && u.childId = aId
            && u.ownership = Ownership.Owner)
    Assert.DoesNotContain(
        plan.ownershipUpdates,
        fun u -> u.parentId = Graph.trashId.Value && u.ownership = Ownership.Owner)
    Assert.Empty(plan.insertChildren)

[<Fact>]
let ``unreachable non-protected nodes are garbage collected`` () =
    let orphanId, orphanChildId = guid 30, guid 31
    let nodes =
        canonicalNodes
        @ [ nodeRow orphanId "orphan"; nodeRow orphanChildId "orphan-child" ]
    let children =
        canonicalChildren
        @ [ child orphanId 0 orphanChildId Ownership.Owner ]
    let plan = expectOk nodes children
    Assert.Equal<Guid list>(
        [ orphanId; orphanChildId ] |> List.sort,
        plan.deleteNodeIds |> List.sort)
    Assert.DoesNotContain(Graph.rootId.Value, plan.deleteNodeIds)
    Assert.DoesNotContain(Graph.trashId.Value, plan.deleteNodeIds)
    Assert.DoesNotContain(Graph.workspacesId.Value, plan.deleteNodeIds)
    Assert.DoesNotContain(Graph.systemId.Value, plan.deleteNodeIds)

[<Fact>]
let ``owned cycle with rooted ingress keeps rooted owner`` () =
    let aId, bId = guid 40, guid 41
    let nodes = canonicalNodes @ [ nodeRow aId "A"; nodeRow bId "B" ]
    let children =
        canonicalChildren
        @ [ child Graph.rootId.Value 3 aId Ownership.Owner
            child aId 0 bId Ownership.Owner
            child bId 0 aId Ownership.Owner ]
    let plan = expectOk nodes children
    Assert.Contains(
        plan.ownershipUpdates,
        fun u ->
            u.parentId = bId && u.childId = aId && u.ownership = Ownership.Ref)
    Assert.DoesNotContain(
        plan.ownershipUpdates,
        fun u -> u.parentId = Graph.rootId.Value && u.childId = aId)
    Assert.DoesNotContain(
        plan.ownershipUpdates,
        fun u -> u.parentId = aId && u.childId = bId)

[<Fact>]
let ``owned cycle only ref-reachable promotes ingress then demotes closer`` () =
    let aId, bId = guid 50, guid 51
    let nodes = canonicalNodes @ [ nodeRow aId "A"; nodeRow bId "B" ]
    let children =
        canonicalChildren
        @ [ child Graph.rootId.Value 3 aId Ownership.Ref
            child aId 0 bId Ownership.Owner
            child bId 0 aId Ownership.Owner ]
    let plan = expectOk nodes children
    Assert.Contains(
        plan.ownershipUpdates,
        fun u ->
            u.parentId = Graph.rootId.Value
            && u.childId = aId
            && u.ownership = Ownership.Owner)
    Assert.Contains(
        plan.ownershipUpdates,
        fun u ->
            u.parentId = bId && u.childId = aId && u.ownership = Ownership.Ref)

[<Fact>]
let ``incoming owner on root is demoted to ref`` () =
    let uId = guid 60
    let nodes = canonicalNodes @ [ nodeRow uId "U" ]
    let children =
        canonicalChildren
        @ [ child Graph.rootId.Value 3 uId Ownership.Owner
            child uId 0 Graph.rootId.Value Ownership.Owner ]
    let plan = expectOk nodes children
    Assert.Contains(
        plan.ownershipUpdates,
        fun u ->
            u.parentId = uId
            && u.childId = Graph.rootId.Value
            && u.ownership = Ownership.Ref)

[<Fact>]
let ``missing trash node and owned-under-root edge are inserted`` () =
    let nodes =
        canonicalNodes
        |> List.filter (fun n -> n.id <> Graph.trashId.Value)
    let children =
        canonicalChildren
        |> List.filter (fun r ->
            r.childId <> Graph.trashId.Value && r.parentId <> Graph.trashId.Value)
    let plan = expectOk nodes children
    Assert.Equal(Graph.trashId.Value, Assert.Single(plan.insertNodes).id)
    let inserted = Assert.Single(plan.insertChildren)
    Assert.Equal(Graph.rootId.Value, inserted.parentId)
    Assert.Equal(Graph.trashId.Value, inserted.childId)
    Assert.Equal(Ownership.Owner, inserted.ownership)
    Assert.Equal("Trash", plan.insertNodes.Head.text)
    Assert.Equal(Some "TRASH", plan.insertNodes.Head.name)
    Assert.Equal("directory", plan.insertNodes.Head.kind)

[<Fact>]
let ``missing workspaces and system owned-under-root edges are inserted`` () =
    let nodes =
        canonicalNodes
        |> List.filter (fun n ->
            n.id <> Graph.workspacesId.Value && n.id <> Graph.systemId.Value)
    let children =
        canonicalChildren
        |> List.filter (fun r ->
            r.childId <> Graph.workspacesId.Value
            && r.childId <> Graph.systemId.Value
            && r.parentId <> Graph.workspacesId.Value
            && r.parentId <> Graph.systemId.Value)
    let plan = expectOk nodes children
    let insertedIds = plan.insertNodes |> List.map _.id |> Set.ofList
    Assert.True(Set.contains Graph.workspacesId.Value insertedIds)
    Assert.True(Set.contains Graph.systemId.Value insertedIds)
    Assert.Contains(
        plan.insertChildren,
        fun r ->
            r.parentId = Graph.rootId.Value
            && r.childId = Graph.workspacesId.Value
            && r.ownership = Ownership.Owner)
    Assert.Contains(
        plan.insertChildren,
        fun r ->
            r.parentId = Graph.rootId.Value
            && r.childId = Graph.systemId.Value
            && r.ownership = Ownership.Owner)
    let wsOrd =
        plan.insertChildren
        |> List.find (fun r -> r.childId = Graph.workspacesId.Value)
    let sysOrd =
        plan.insertChildren
        |> List.find (fun r -> r.childId = Graph.systemId.Value)
    Assert.True(wsOrd.ordinal < 2)
    Assert.True(sysOrd.ordinal < 2)

[<Fact>]
let ``missing workspaces and system shift later dense root ordinals`` () =
    let u1, u2 = guid 70, guid 71
    let rootId = Graph.rootId.Value
    let trashId = Graph.trashId.Value
    let rootNode =
        canonicalNodes |> List.find (fun n -> n.id = rootId)
    let trashNode =
        canonicalNodes |> List.find (fun n -> n.id = trashId)
    let nodes =
        [ rootNode; trashNode; nodeRow u1 "U1"; nodeRow u2 "U2" ]
    let children =
        [ child rootId 0 u1 Ownership.Owner
          child rootId 1 u2 Ownership.Owner
          child rootId 2 trashId Ownership.Owner ]
    let plan = expectOk nodes children
    let ws =
        plan.insertChildren
        |> List.find (fun r -> r.childId = Graph.workspacesId.Value)
    let sys =
        plan.insertChildren
        |> List.find (fun r -> r.childId = Graph.systemId.Value)
    Assert.Equal(rootId, ws.parentId)
    Assert.Equal(rootId, sys.parentId)
    Assert.Equal(2, ws.ordinal)
    Assert.Equal(3, sys.ordinal)
    let trashShift =
        plan.rootOrdinalUpdates
        |> List.find (fun u -> u.childId = trashId)
    Assert.Equal(4, trashShift.ordinal)
    Assert.DoesNotContain(plan.rootOrdinalUpdates, fun u -> u.childId = u1)
    Assert.DoesNotContain(plan.rootOrdinalUpdates, fun u -> u.childId = u2)
    let occupied =
        let shifted = plan.rootOrdinalUpdates |> List.map _.childId |> Set.ofList
        let kept =
            children
            |> List.filter (fun r -> not (Set.contains r.childId shifted))
            |> List.map _.ordinal
        let moved = plan.rootOrdinalUpdates |> List.map _.ordinal
        let inserted =
            plan.insertChildren
            |> List.filter (fun r -> r.parentId = rootId)
            |> List.map _.ordinal
        kept @ moved @ inserted
    Assert.Equal(occupied.Length, occupied |> List.distinct |> List.length)

[<Fact>]
let ``duplicate root child keeps both rows when canonicals insert`` () =
    let u1 = guid 80
    let rootId = Graph.rootId.Value
    let trashId = Graph.trashId.Value
    let rootNode =
        canonicalNodes |> List.find (fun n -> n.id = rootId)
    let trashNode =
        canonicalNodes |> List.find (fun n -> n.id = trashId)
    let nodes = [ rootNode; trashNode; nodeRow u1 "U1" ]
    let children =
        [ child rootId 0 u1 Ownership.Owner
          child rootId 1 u1 Ownership.Ref
          child rootId 2 trashId Ownership.Owner ]
    let plan = expectOk nodes children
    Assert.DoesNotContain(plan.rootOrdinalUpdates, fun u -> u.childId = u1)
    let trashShift =
        plan.rootOrdinalUpdates
        |> List.find (fun u -> u.childId = trashId)
    Assert.Equal(4, trashShift.ordinal)
    Assert.Equal(2, plan.insertChildren.Length)

[<Fact>]
let ``canonicals already under root as ref promote in place with no insert`` () =
    let uId = guid 90
    let rootId = Graph.rootId.Value
    let nodes = canonicalNodes @ [ nodeRow uId "U" ]
    let children =
        [ child rootId 0 uId Ownership.Owner
          child rootId 1 Graph.workspacesId.Value Ownership.Ref
          child rootId 2 Graph.systemId.Value Ownership.Ref
          child rootId 3 Graph.trashId.Value Ownership.Owner ]
    let plan = expectOk nodes children
    Assert.Empty(plan.insertNodes)
    Assert.Empty(plan.insertChildren)
    Assert.Empty(plan.rootOrdinalUpdates)
    Assert.Empty(plan.deleteNodeIds)
    Assert.Contains(
        plan.ownershipUpdates,
        fun u ->
            u.parentId = rootId
            && u.childId = Graph.workspacesId.Value
            && u.ownership = Ownership.Owner
            && u.ordinal = 1)
    Assert.Contains(
        plan.ownershipUpdates,
        fun u ->
            u.parentId = rootId
            && u.childId = Graph.systemId.Value
            && u.ownership = Ownership.Owner
            && u.ordinal = 2)
    Assert.DoesNotContain(
        plan.ownershipUpdates,
        fun u -> u.childId = Graph.trashId.Value || u.childId = uId)

[<Fact>]
let ``canonicals already owned under root do not insert or shift`` () =
    let uId = guid 91
    let rootId = Graph.rootId.Value
    let nodes = canonicalNodes @ [ nodeRow uId "U" ]
    let children =
        [ child rootId 0 Graph.trashId.Value Ownership.Owner
          child rootId 1 uId Ownership.Owner
          child rootId 2 Graph.workspacesId.Value Ownership.Owner
          child rootId 3 Graph.systemId.Value Ownership.Owner ]
    let plan = expectOk nodes children
    Assert.True(ProjectionOwnershipRepair.isNoOp plan)

[<Fact>]
let ``canonical node without root edge is inserted once`` () =
    let uId = guid 92
    let rootId = Graph.rootId.Value
    let sysId = Graph.systemId.Value
    let nodes = canonicalNodes @ [ nodeRow uId "U" ]
    let children =
        [ child rootId 0 Graph.workspacesId.Value Ownership.Owner
          child rootId 1 uId Ownership.Owner
          child rootId 2 Graph.trashId.Value Ownership.Owner ]
    let plan = expectOk nodes children
    Assert.Empty(plan.insertNodes)
    let inserted = Assert.Single(plan.insertChildren)
    Assert.Equal(rootId, inserted.parentId)
    Assert.Equal(sysId, inserted.childId)
    Assert.Equal(Ownership.Owner, inserted.ownership)
    Assert.Equal(2, inserted.ordinal)
    let trashShift =
        plan.rootOrdinalUpdates |> List.find (fun u -> u.childId = Graph.trashId.Value)
    Assert.Equal(3, trashShift.ordinal)
    Assert.DoesNotContain(plan.rootOrdinalUpdates, fun u -> u.childId = uId)

[<Fact>]
let ``valid tree is an empty plan`` () =
    let plan = expectOk canonicalNodes canonicalChildren
    Assert.True(ProjectionOwnershipRepair.isNoOp plan)

[<Fact>]
let ``missing root id in nodes is an error`` () =
    let nodes =
        canonicalNodes |> List.filter (fun n -> n.id <> Graph.rootId.Value)
    match run nodes canonicalChildren with
    | Error e -> Assert.Contains("root id missing from nodes", e)
    | Ok _ -> Assert.Fail("expected Error for missing root")
