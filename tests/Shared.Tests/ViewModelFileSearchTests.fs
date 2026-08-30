module ViewModelFileSearchTests

open Gambol.Shared
open RefExprTestTree
open Xunit

let private tree = lazy build ()

let private ids (results: FileSearchResult list) =
    results |> List.map (fun r -> r.nodeId)

let private kinds (results: FileSearchResult list) (graph: Graph) =
    results
    |> List.map (fun r -> graph.nodes.[r.nodeId].kind)

let private addRootOwnedFile (name: string) (graph: Graph) : NodeId * Graph =
    let fileId = NodeId.New()
    let file =
        Node.Create(
            fileId,
            text = name,
            name = Filename.create name,
            owner = Graph.rootId,
            kind = Special File)
    let parent = graph.nodes.[Graph.rootId]
    let link = ChildNode.owner fileId
    let nodes =
        graph.nodes
        |> Map.add fileId file
        |> Map.add Graph.rootId { parent with children = parent.children @ [ link ] }
    fileId, Graph.fromNodes graph.root nodes

let private isArtifactKind (kind: NodeKind) =
    match kind with
    | Special (File | Directory | Workspace) -> true
    | _ -> false

[<Fact>]
let ``findInScope peers match ownedArtifactsInUniquenessScope for insert focus`` () =
    let t = tree.Value
    let assertSame focus parentId =
        let fromSearch =
            ViewModelFileSearch.findInScope "" focus t.graph |> ids |> List.sort
        let fromUniq =
            GraphQuery.ownedArtifactsInUniquenessScope t.graph parentId None
            |> List.sort
        Assert.Equal<NodeId list>(fromUniq, fromSearch)
    assertSame t.contentFile t.contentFileDir
    assertSame t.workspaceRoot t.workspaceRoot
    assertSame Graph.workspacesId Graph.workspacesId

[<Fact>]
let ``findInScope lists Workspace Directory File peers under directory scope`` () =
    let t = tree.Value
    let hits = ViewModelFileSearch.findInScope "" t.contentFile t.graph
    let hitIds = ids hits
    Assert.All(hits, fun r -> Assert.True(isArtifactKind t.graph.nodes.[r.nodeId].kind))
    Assert.Equal(2, hits.Length)
    Assert.Contains(t.appFs, hitIds)
    Assert.Contains(t.libFs, hitIds)
    Assert.DoesNotContain(t.embeddedMd, hitIds)
    Assert.DoesNotContain(t.readmeMd, hitIds)
    Assert.DoesNotContain(t.bobbySrc, hitIds)

[<Fact>]
let ``findInScope directory scope excludes sibling dirs and other workspaces`` () =
    let t = tree.Value
    let hits = ViewModelFileSearch.findInScope "" t.contentFile t.graph |> ids
    Assert.DoesNotContain(t.readmeMd, hits)
    Assert.DoesNotContain(t.workspaceRoot, hits)

[<Fact>]
let ``findInScope workspace scope lists peer dirs not nested files`` () =
    let t = tree.Value
    let docsId = t.graph.nodes.[t.readmeMd].owner
    let hits = ViewModelFileSearch.findInScope "" t.workspaceRoot t.graph
    let hitIds = ids hits
    Assert.Equal(2, hits.Length)
    Assert.Contains(t.bobbySrc, hitIds)
    Assert.Contains(docsId, hitIds)
    Assert.DoesNotContain(t.appFs, hitIds)
    Assert.DoesNotContain(t.readmeMd, hitIds)
    Assert.All(hits, fun r -> Assert.Equal(Special Directory, t.graph.nodes.[r.nodeId].kind))

[<Fact>]
let ``findInScope Workspaces focus lists root-owned files and named workspaces as peers`` () =
    let t = tree.Value
    let rootFileId, graph = addRootOwnedFile "shared.txt" t.graph
    let hits = ViewModelFileSearch.findInScope "" Graph.workspacesId graph
    let hitIds = ids hits
    Assert.Contains(rootFileId, hitIds)
    Assert.Contains(t.workspaceRoot, hitIds)
    Assert.All(hits, fun r -> Assert.True(isArtifactKind graph.nodes.[r.nodeId].kind))
    Assert.DoesNotContain(t.appFs, hitIds)
    Assert.DoesNotContain(t.readmeMd, hitIds)
    Assert.DoesNotContain(t.embeddedMd, hitIds)
    Assert.DoesNotContain(t.bobbySrc, hitIds)

[<Fact>]
let ``findInScope name filter keeps matching artifact kinds`` () =
    let t = tree.Value
    let hits = ViewModelFileSearch.findInScope ".fs" t.contentFile t.graph
    let hitIds = ids hits
    Assert.Equal(2, hits.Length)
    Assert.Contains(t.appFs, hitIds)
    Assert.Contains(t.libFs, hitIds)
    Assert.All(kinds hits t.graph, fun k -> Assert.Equal(Special File, k))

[<Fact>]
let ``findInScope path word matches RefExpr hits inside peer scope`` () =
    let t = tree.Value
    let hits =
        ViewModelFileSearch.findInScope "//bobby/src/*.fs" t.contentFile t.graph
        |> ids
    Assert.Equal(2, hits.Length)
    Assert.Contains(t.appFs, hits)
    Assert.Contains(t.libFs, hits)

[<Fact>]
let ``findInScope mixed words require same peer node`` () =
    let t = tree.Value
    let hits =
        ViewModelFileSearch.findInScope "app app.fs" t.contentFile t.graph
        |> ids
    Assert.Equal<NodeId>([ t.appFs ], hits)

[<Fact>]
let ``findInScope pathLabel uses desktop path syntax`` () =
    let t = tree.Value
    let hit =
        ViewModelFileSearch.findInScope "app.fs" t.contentFile t.graph
        |> List.tryFind (fun r -> r.nodeId = t.appFs)
        |> Option.defaultWith (fun () -> failwith "missing app.fs hit")
    Assert.Equal("//bobby/src/app.fs", hit.pathLabel)
    Assert.Equal(Special File, hit.kind)

[<Fact>]
let ``findInScope exposes kind for directory peers`` () =
    let t = tree.Value
    let hit =
        ViewModelFileSearch.findInScope "src" t.workspaceRoot t.graph
        |> List.tryFind (fun r -> r.nodeId = t.bobbySrc)
        |> Option.defaultWith (fun () -> failwith "missing src hit")
    Assert.Equal(Special Directory, hit.kind)
    Assert.Equal("//bobby/src/", hit.pathLabel)

[<Fact>]
let ``takeResults paging reconstitutes findInScope order`` () =
    let t = tree.Value
    let query = ""
    let full = ViewModelFileSearch.findInScope query t.workspaceRoot t.graph
    Assert.True(full.Length > 1, "expected multiple peer hits for paging")

    match ViewModelFileSearch.startFind query t.workspaceRoot t.graph with
    | None -> Assert.Fail "expected cursor"
    | Some cursor ->
        let rec drain acc cur =
            match ViewModelFileSearch.takeResults 1 cur with
            | [], None -> List.rev acc
            | page, Some next -> drain (List.rev page @ acc) next
            | page, None -> List.rev (List.rev page @ acc)

        let paged = drain [] cursor
        Assert.Equal<NodeId>(ids full, ids paged)
