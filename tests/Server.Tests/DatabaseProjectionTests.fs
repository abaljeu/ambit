module Gambol.Server.Tests.DatabaseProjectionTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared

let private id value =
    NodeId(Guid.Parse($"10000000-0000-0000-0000-{value:D12}"))

let private stamp value =
    DateTime(2026, 7, 24, 12, value, 0, DateTimeKind.Utc)

let private completeNode nodeId label =
    Node.Create(
        nodeId,
        text = label,
        name = Filename.create $"{label}.amb",
        cssClasses = CssClass.ofList [ "one"; "two" ],
        kind = Special SpecialKind.File,
        documentState = Unparsed,
        updateTime = stamp 1)

let private graphFromNodes _root nodes =
    let customNodes = nodes |> List.map (fun node -> node.id, node)

    (Graph.create ()).nodes
    |> Map.toList
    |> List.append customNodes
    |> Map.ofList
    |> Graph.fromNodes Graph.rootId

let private change op =
    { id = 0
      changeId = Guid.NewGuid()
      ops = [ op ] }

let private expectedRow node =
    { id = node.id.Value
      text = node.text
      name = Filename.tryValue node.name
      kind = NodeKindPersistence.toPersistString node.kind
      documentState =
        match node.documentState with
        | Current -> "current"
        | Unparsed -> "unparsed"
        | NoServerFile -> "noServerFile"
      cssClassNames = CssClass.toList node.cssClasses
      updateTime = node.updateTime }
    : GraphProjection.NodePersistenceRow

[<Fact>]
let ``plan selects complete final node rows for every current op`` () =
    let ids = [ 1..8 ] |> List.map id
    let nodes = ids |> List.mapi (fun index nodeId -> completeNode nodeId $"final-{index}")
    let graph = graphFromNodes ids.Head nodes
    let classes = CssClass.ofList [ "old" ]
    let child = { ref = Ownership.Ref; id = ids.[1] }

    let changes =
        [ Op.NewNode(ids.[0], "initial")
          Op.SetText(ids.[1], "before", "after")
          Op.SetClasses(ids.[2], classes, CssClass.empty)
          Op.Replace(ids.[3], 0, [], [ child ])
          Op.NewSpecialNode(ids.[4], SpecialKind.File, "initial.amb")
          Op.SetName(ids.[5], "before.amb", "after.amb")
          Op.SetDocumentState(ids.[6], Current, Unparsed)
          Op.SetUpdateTime(ids.[7], stamp 0, stamp 1) ]
        |> List.map change

    let patch = DatabaseProjection.plan graph 23 changes

    Assert.Equal(23, patch.graph.revision)
    Assert.Equal(graph.root.Value, patch.graph.rootId)
    Assert.Equal<GraphProjection.NodePersistenceRow list>(
        nodes |> List.map expectedRow,
        patch.nodeUpserts)

[<Fact>]
let ``plan uses final special rename text update time and generated defaults`` () =
    let normalId = id 10
    let specialId = id 11
    let normal = Node.Create(normalId, text = "created", updateTime = stamp 2)

    let special =
        Node.Create(
            specialId,
            text = "renamed.amb",
            name = Filename.create "renamed.amb",
            kind = Special SpecialKind.File,
            updateTime = stamp 3)

    let graph = graphFromNodes normalId [ normal; special ]
    let changes =
        [ change (Op.NewNode(normalId, "created"))
          change (Op.SetName(specialId, "old.amb", "renamed.amb")) ]

    let patch = DatabaseProjection.plan graph 1 changes

    Assert.Equal<GraphProjection.NodePersistenceRow list>(
        [ expectedRow normal; expectedRow special ],
        patch.nodeUpserts)
    Assert.Equal("normal", patch.nodeUpserts.[0].kind)
    Assert.Equal("renamed.amb", patch.nodeUpserts.[1].text)
    Assert.Equal(stamp 3, patch.nodeUpserts.[1].updateTime)

[<Fact>]
let ``plan collapses repeated node and parent touches`` () =
    let parentId = id 20
    let childId = id 21
    let child = completeNode childId "child"
    let childRef = { ref = Ownership.Owner; id = childId }
    let parent = Node.Create(parentId, text = "final", children = [ childRef ])
    let graph = graphFromNodes parentId [ parent; child ]

    let changes =
        [ { id = 0
            changeId = Guid.NewGuid()
            ops =
                [ Op.SetText(parentId, "first", "second")
                  Op.SetText(parentId, "second", "final")
                  Op.Replace(parentId, 0, [], [ childRef ])
                  Op.Replace(parentId, 0, [ childRef ], [ childRef ]) ] } ]

    let patch = DatabaseProjection.plan graph 1 changes

    Assert.Single(patch.nodeUpserts) |> ignore
    Assert.Single(patch.childReplacements) |> ignore
    Assert.Equal(parentId.Value, patch.childReplacements.Head.parentId)

[<Fact>]
let ``plan replaces children from final ordering including an empty list`` () =
    let firstId = id 31
    let secondId = id 32
    let filledId = id 33
    let emptyId = id 34
    let first = completeNode firstId "first"
    let second = completeNode secondId "second"

    let finalChildren =
        [ { ref = Ownership.Ref; id = secondId }
          { ref = Ownership.Owner; id = firstId } ]

    let filled = Node.Create(filledId, children = finalChildren)
    let empty = Node.Create(emptyId)
    let graph = graphFromNodes filledId [ first; second; filled; empty ]

    let changes =
        [ change (Op.Replace(filledId, 0, [], finalChildren))
          change (Op.Replace(emptyId, 0, finalChildren, [])) ]

    let patch = DatabaseProjection.plan graph 5 changes

    Assert.Equal(2, patch.childReplacements.Length)
    Assert.Equal<(Guid * Ownership) list>(
        [ secondId.Value, Ownership.Ref; firstId.Value, Ownership.Owner ],
        patch.childReplacements.[0].rows
        |> List.map (fun row -> row.childId, row.ownership))
    Assert.Empty(patch.childReplacements.[1].rows)

let private normalizeSql (sql: string) =
    sql.Split([| ' '; '\r'; '\n'; '\t' |], StringSplitOptions.RemoveEmptyEntries)
    |> String.concat " "

[<Fact>]
let ``commands expose deterministic SQL and bind values`` () =
    let parentId = id 40
    let childId = id 41
    let childRef = { ref = Ownership.Owner; id = childId }
    let parent = Node.Create(parentId, text = "parent", children = [ childRef ])
    let child = completeNode childId "child"
    let graph = graphFromNodes parentId [ parent; child ]

    let patch =
        DatabaseProjection.plan graph 7
            [ { id = 0
                changeId = Guid.NewGuid()
                ops =
                    [ Op.SetText(childId, "old", "child")
                      Op.Replace(parentId, 0, [], [ childRef ]) ] } ]

    let commands = DatabaseProjection.commands patch
    let sql = commands |> List.map DatabaseProjection.sqlText |> List.map normalizeSql

    Assert.Equal(4, commands.Length)
    Assert.StartsWith("INSERT INTO nodes", sql.[0])
    Assert.Contains("ON CONFLICT (id) DO UPDATE", sql.[0])
    Assert.Equal(
        "DELETE FROM node_children WHERE parent_id = ANY(@parent_ids::uuid[])",
        sql.[1])
    Assert.StartsWith("INSERT INTO node_children", sql.[2])
    Assert.StartsWith("INSERT INTO graph", sql.[3])
    Assert.Equal<string list>(
        sql,
        commands |> List.map DatabaseProjection.sqlText |> List.map normalizeSql)

    match commands.[0] with
    | DatabaseProjection.UpsertNodes rows ->
        Assert.Equal<Guid list>([ parentId.Value; childId.Value ], rows |> List.map _.id)
    | command -> Assert.Fail($"expected UpsertNodes, got {command}")

    match commands.[1] with
    | DatabaseProjection.DeleteChildren parentIds ->
        Assert.Equal<Guid list>([ parentId.Value ], parentIds)
    | command -> Assert.Fail($"expected DeleteChildren, got {command}")

    match commands.[2] with
    | DatabaseProjection.InsertChildren rows ->
        Assert.Equal<Guid list>([ childId.Value ], rows |> List.map _.childId)
    | command -> Assert.Fail($"expected InsertChildren, got {command}")

    let bindings = commands |> List.map DatabaseProjection.bindings

    Assert.Equal<string list>(
        [ "ids"; "texts"; "names"; "css_classes"; "update_times"; "kinds";
          "document_states" ],
        bindings.[0] |> List.map _.name)

    match bindings.[0].Head.value with
    | DatabaseProjection.GuidValues values ->
        Assert.Equal<Guid list>([ parentId.Value; childId.Value ], values)
    | value -> Assert.Fail($"expected GuidValues, got {value}")

    match bindings.[1].Head.value, bindings.[2].Head.value with
    | DatabaseProjection.GuidValues deleted,
      DatabaseProjection.GuidValues inserted ->
        Assert.Equal<Guid list>([ parentId.Value ], deleted)
        Assert.Equal<Guid list>([ parentId.Value ], inserted)
    | values -> Assert.Fail($"expected child GuidValues, got {values}")

    match bindings.[3] |> List.map _.value with
    | [ DatabaseProjection.GuidValue root; DatabaseProjection.IntValue revision ] ->
        Assert.Equal(Graph.rootId.Value, root)
        Assert.Equal(7, revision)
    | values -> Assert.Fail($"expected graph scalar values, got {values}")

    match commands.[3] with
    | DatabaseProjection.UpsertGraph graphPatch ->
        Assert.Equal(Graph.rootId.Value, graphPatch.rootId)
        Assert.Equal(7, graphPatch.revision)
    | command -> Assert.Fail($"expected UpsertGraph, got {command}")

[<Fact>]
let ``trim deleted nodes rebuilds indexes and protects canonical nodes`` () =
    let detachedId = id 100
    let detached = Node.Create(detachedId)
    let graph = graphFromNodes Graph.rootId [ detached ]
    let deleted =
        [ detachedId; Graph.rootId; Graph.workspacesId; Graph.systemId; Graph.trashId ]

    let trimmed = DatabaseProjection.trimDeletedNodes deleted graph

    Assert.False(trimmed.nodes.ContainsKey detachedId)
    Assert.Equal(Graph.rootId, trimmed.root)
    Assert.True(trimmed.nodes.ContainsKey Graph.rootId)
    Assert.True(trimmed.nodes.ContainsKey Graph.workspacesId)
    Assert.True(trimmed.nodes.ContainsKey Graph.systemId)
    Assert.True(trimmed.nodes.ContainsKey Graph.trashId)
    Assert.False(trimmed.parentByChild.ContainsKey detachedId)

[<Fact>]
let ``startup sweep command binds canonical protection and recursive delete returning`` () =
    let patch = DatabaseProjection.startupSweepPatch
    let command = DatabaseProjection.maintenanceCommand patch
    let sql = DatabaseProjection.maintenanceSqlText command |> normalizeSql
    let bindings = DatabaseProjection.maintenanceBindings command

    Assert.Contains("WITH RECURSIVE reachable", sql)
    Assert.Contains("SELECT root_id FROM graph WHERE singleton = 1", sql)
    Assert.Contains("UNION SELECT children.child_id", sql)
    Assert.Contains("JOIN reachable", sql)
    Assert.Contains("DELETE FROM nodes", sql)
    Assert.Contains("RETURNING id", sql)
    Assert.Single(bindings) |> ignore
    Assert.Equal("protected_ids", bindings.Head.name)

    match bindings.Head.value with
    | DatabaseProjection.GuidValues values ->
        Assert.Equal<Guid list>(
            [ Graph.rootId; Graph.trashId; Graph.workspacesId; Graph.systemId ]
            |> List.map _.Value,
            values)
    | value -> Assert.Fail($"expected GuidValues, got {value}")
