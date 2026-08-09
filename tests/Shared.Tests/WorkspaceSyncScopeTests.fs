module WorkspaceSyncScopeTests

open Gambol.Shared
open Xunit

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private graphWithWorkspaceTree () : Graph * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let wsNode = specialNode wsId Workspace "home" Graph.workspacesId
    let dirNode = specialNode dirId Directory "docs" wsId
    let fileNode = specialNode fileId File "readme.txt" dirId

    let graph1 =
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add dirId dirNode
        |> Map.add fileId fileNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"

    let graph3 =
        Graph.replace wsId 0 [] (owned [ dirId ]) graph2
        |> requireOk "ws->dir"

    let graph4 =
        Graph.replace dirId 0 [] (owned [ fileId ]) graph3
        |> requireOk "dir->file"

    graph4, wsId, dirId, fileId

[<Fact>]
let ``normalizeRelative accepts empty as workspace root`` () =
    Assert.Equal(Ok "", WorkspaceSyncScope.normalizeRelative "")
    Assert.Equal(Ok "", WorkspaceSyncScope.normalizeRelative "/")
    Assert.Equal(Ok "a/b", WorkspaceSyncScope.normalizeRelative "a\\b")

[<Fact>]
let ``normalizeRelative rejects dotdot and empty segments`` () =
    Assert.Equal(Error "invalid_path", WorkspaceSyncScope.normalizeRelative "..")
    Assert.Equal(
        Error "invalid_path",
        WorkspaceSyncScope.normalizeRelative "foo/../bar")
    Assert.Equal(
        Error "invalid_path",
        WorkspaceSyncScope.normalizeRelative "foo//bar")

[<Fact>]
let ``tryFromFocus workspace is empty relative`` () =
    let graph, wsId, _, _ = graphWithWorkspaceTree ()
    match WorkspaceSyncScope.tryFromFocus graph wsId with
    | Error e -> Assert.Fail(e)
    | Ok scope ->
        Assert.Equal("home", scope.label)
        Assert.Equal("", scope.relative)
        Assert.Equal(SyncScopeKind.Workspace, scope.kind)

[<Fact>]
let ``tryFromFocus directory and file use relative prefix`` () =
    let graph, _, dirId, fileId = graphWithWorkspaceTree ()
    match WorkspaceSyncScope.tryFromFocus graph dirId with
    | Error e -> Assert.Fail(e)
    | Ok scope ->
        Assert.Equal("home", scope.label)
        Assert.Equal("docs", scope.relative)
        Assert.Equal(SyncScopeKind.Directory, scope.kind)

    match WorkspaceSyncScope.tryFromFocus graph fileId with
    | Error e -> Assert.Fail(e)
    | Ok scope ->
        Assert.Equal("home", scope.label)
        Assert.Equal("docs/readme.txt", scope.relative)
        Assert.Equal(SyncScopeKind.File, scope.kind)

[<Fact>]
let ``tryFromFocus SYSTEM directory is workspace-kind empty relative`` () =
    let graph = Graph.create ()
    match WorkspaceSyncScope.tryFromFocus graph Graph.systemId with
    | Error e -> Assert.Fail(e)
    | Ok scope ->
        Assert.Equal("SYSTEM", scope.label)
        Assert.Equal("", scope.relative)
        Assert.Equal(SyncScopeKind.Workspace, scope.kind)

[<Fact>]
let ``tryFromFocus TRASH directory is workspace-kind empty relative`` () =
    let graph = Graph.create ()
    match WorkspaceSyncScope.tryFromFocus graph Graph.trashId with
    | Error e -> Assert.Fail(e)
    | Ok scope ->
        Assert.Equal("TRASH", scope.label)
        Assert.Equal("", scope.relative)
        Assert.Equal(SyncScopeKind.Workspace, scope.kind)

[<Fact>]
let ``tryFromFocus file under SYSTEM uses SYSTEM label`` () =
    let graph0 = Graph.create ()
    let fileId, ops =
        FileNodeOps.planCreateOwnedFile graph0 Graph.systemId "user.css"
    let graph =
        ops
        |> List.fold
            (fun s op ->
                match Op.apply op s with
                | ApplyResult.Changed next
                | ApplyResult.Unchanged next -> next
                | ApplyResult.Invalid(_, msg) -> failwith msg)
            { graph = graph0
              history = History.empty
              revision = Revision.Zero }
        |> fun s -> s.graph
    match WorkspaceSyncScope.tryFromFocus graph fileId with
    | Error e -> Assert.Fail(e)
    | Ok scope ->
        Assert.Equal("SYSTEM", scope.label)
        Assert.Equal("user.css", scope.relative)
        Assert.Equal(SyncScopeKind.File, scope.kind)

[<Fact>]
let ``filterUnderScope directory prefix does not match sibling`` () =
    let scope =
        { label = "home"
          relative = "docs"
          kind = SyncScopeKind.Directory }
    let kept =
        WorkspaceSyncScope.filterUnderScope
            scope
            [ "docs"
              "docs/a.txt"
              "docs2/x"
              "other/docs/a" ]
    Assert.Equal<string list>([ "docs"; "docs/a.txt" ], kept)

[<Fact>]
let ``filterUnderScope file is exact match only`` () =
    let scope =
        { label = "home"
          relative = "docs/readme.txt"
          kind = SyncScopeKind.File }
    let kept =
        WorkspaceSyncScope.filterUnderScope
            scope
            [ "docs/readme.txt"; "docs/readme.txt.bak"; "docs/other.txt" ]
    Assert.Equal<string list>([ "docs/readme.txt" ], kept)

[<Fact>]
let ``filterUnderScope workspace includes all valid relatives`` () =
    let scope =
        { label = "home"
          relative = ""
          kind = SyncScopeKind.Workspace }
    let kept =
        WorkspaceSyncScope.filterUnderScope
            scope
            [ "a.txt"; "docs/b.txt"; "../escape" ]
    Assert.Equal<string list>([ "a.txt"; "docs/b.txt" ], kept)

[<Fact>]
let ``tryRelativeUnderLabel rejects other workspace`` () =
    match WorkspaceSyncScope.tryRelativeUnderLabel "home" "//other/docs/a" with
    | Ok _ -> Assert.Fail("expected label escape")
    | Error e -> Assert.Equal("path escapes workspace label", e)

[<Fact>]
let ``tryRelativeUnderLabel accepts matching label`` () =
    match WorkspaceSyncScope.tryRelativeUnderLabel "home" "//home/docs/a.txt" with
    | Error e -> Assert.Fail(e)
    | Ok rel -> Assert.Equal("docs/a.txt", rel)

[<Fact>]
let ``coalesceDownloadTargets one file per label is File scope`` () =
    let scopes =
        WorkspaceSyncScope.coalesceDownloadTargets
            [ { label = "home"; relative = "docs/readme.txt" } ]
    Assert.Equal<WorkspaceSyncScope list>(
        [ { label = "home"
            relative = "docs/readme.txt"
            kind = SyncScopeKind.File } ],
        scopes)

[<Fact>]
let ``coalesceDownloadTargets shared parent is nearest Directory`` () =
    let scopes =
        WorkspaceSyncScope.coalesceDownloadTargets
            [ { label = "home"; relative = "docs/a.txt" }
              { label = "home"; relative = "docs/sub/b.txt" } ]
    Assert.Equal<WorkspaceSyncScope list>(
        [ { label = "home"; relative = "docs"; kind = SyncScopeKind.Directory } ],
        scopes)

[<Fact>]
let ``coalesceDownloadTargets disjoint roots fall back to Workspace`` () =
    let scopes =
        WorkspaceSyncScope.coalesceDownloadTargets
            [ { label = "home"; relative = "docs/a.txt" }
              { label = "home"; relative = "src/b.txt" } ]
    Assert.Equal<WorkspaceSyncScope list>(
        [ { label = "home"; relative = ""; kind = SyncScopeKind.Workspace } ],
        scopes)

[<Fact>]
let ``coalesceDownloadTargets root-level files coalesce to Workspace`` () =
    let scopes =
        WorkspaceSyncScope.coalesceDownloadTargets
            [ { label = "home"; relative = "a.txt" }
              { label = "home"; relative = "b.txt" } ]
    Assert.Equal<WorkspaceSyncScope list>(
        [ { label = "home"; relative = ""; kind = SyncScopeKind.Workspace } ],
        scopes)

[<Fact>]
let ``coalesceDownloadTargets groups per label`` () =
    let scopes =
        WorkspaceSyncScope.coalesceDownloadTargets
            [ { label = "home"; relative = "a.txt" }
              { label = "work"; relative = "b.txt" }
              { label = "home"; relative = "a.txt" } ]
    Assert.Equal(2, scopes.Length)
    Assert.Contains(
        { label = "home"; relative = "a.txt"; kind = SyncScopeKind.File },
        scopes)
    Assert.Contains(
        { label = "work"; relative = "b.txt"; kind = SyncScopeKind.File },
        scopes)

[<Fact>]
let ``coalesceDownloadTargets drops empty label or relative`` () =
    let scopes =
        WorkspaceSyncScope.coalesceDownloadTargets
            [ { label = ""; relative = "a.txt" }
              { label = "home"; relative = "" } ]
    Assert.Equal<WorkspaceSyncScope list>([], scopes)

[<Fact>]
let ``autoDownloadFileTargets keeps File stamps drops non-file`` () =
    let graph, wsId, _, fileId = graphWithWorkspaceTree ()
    let stamp = System.DateTime(2024, 1, 1)
    let ops =
        [ Op.SetUpdateTime(fileId, NodeUpdateTime.missing, stamp)
          Op.SetUpdateTime(wsId, NodeUpdateTime.missing, stamp) ]
    let targets = WorkspaceUploadStructure.autoDownloadFileTargets graph ops
    Assert.Equal<AutoDownloadTarget list>(
        [ { label = "home"; relative = "docs/readme.txt" } ], targets)
