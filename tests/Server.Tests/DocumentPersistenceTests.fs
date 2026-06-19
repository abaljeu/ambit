module DocumentPersistenceTests

open System.IO
open Gambol.Server
open Gambol.Server.Tests.TestBackend
open Gambol.Shared
open Xunit

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    { id = id
      text = name
      name = Filename.create name
      children = []
      cssClasses = CssClass.empty
      owner = owner
      kind = Special kind
      updateTime = NodeUpdateTime.missing }

let private normalNode (id: NodeId) (text: string) (owner: NodeId) : Node =
    { id = id
      text = text
      name = Filename.Empty
      children = []
      cssClasses = CssClass.empty
      owner = owner
      kind = Normal
      updateTime = NodeUpdateTime.missing }

let private graphWithNestedDocs () : Graph * NodeId * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let normalId = NodeId.New()
    let wsNode = specialNode wsId Workspace "home" Graph.workspacesId
    let dirNode = specialNode dirId Directory "docs" wsId
    let fileNode = specialNode fileId File "readme.txt" dirId
    let normalNode = normalNode normalId "body" fileId

    let graph1 =
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add dirId dirNode
        |> Map.add fileId fileNode
        |> Map.add normalId normalNode
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

    let graph5 =
        Graph.replace fileId 0 [] (owned [ normalId ]) graph4
        |> requireOk "file->normal"

    graph5, wsId, dirId, fileId, normalId

let private graphFileOwnsDirectory () : Graph * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let dirId = NodeId.New()
    let normalId = NodeId.New()
    let fileNode = specialNode fileId File "container.txt" Graph.rootId
    let dirNode = specialNode dirId Directory "inner" fileId
    let normalNode = normalNode normalId "nested" dirId

    let graph1 =
        graph0.nodes
        |> Map.add fileId fileNode
        |> Map.add dirId dirNode
        |> Map.add normalId normalNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        Graph.replace Graph.rootId idx [] (owned [ fileId ]) graph1
        |> requireOk "root->file"

    let graph3 =
        Graph.replace fileId 0 [] (owned [ dirId ]) graph2
        |> requireOk "file->dir"

    let graph4 =
        Graph.replace dirId 0 [] (owned [ normalId ]) graph3
        |> requireOk "dir->normal"

    graph4, fileId, dirId, normalId

let private graphWithRootFile () : Graph * NodeId =
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let fileNode = specialNode fileId File "name.ext" Graph.rootId

    let graph1 =
        graph0.nodes
        |> Map.add fileId fileNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        Graph.replace Graph.rootId idx [] (owned [ fileId ]) graph1
        |> requireOk "root->file"

    graph2, fileId

let private artifactFullPath (dataDir: string) (graph: Graph) (documentRootId: NodeId) =
    DocumentPersistence.resolveArtifactPath dataDir graph documentRootId
    |> requireOk "resolveArtifactPath"

[<Fact>]
let ``writeAllDocuments bootstrap graph writes ROOT and TRASH artifacts`` () =
    let dataDir = newTempDir ()
    let graph = Graph.create ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    Assert.True(File.Exists(Path.Combine(dataDir, ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "TRASH", ".amb")))

[<Fact>]
let ``writeAllDocuments nested workspace tree writes expected paths`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    Assert.True(File.Exists(Path.Combine(dataDir, "@home", ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "@home", "docs", ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "@home", "docs", "readme.txt")))

[<Fact>]
let ``writeAllDocuments ROOT file lands at dataDir root without amb suffix`` () =
    let dataDir = newTempDir ()
    let graph, fileId = graphWithRootFile ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    let path = artifactFullPath dataDir graph fileId
    Assert.Equal(Path.Combine(dataDir, "name.ext"), path)
    Assert.True(File.Exists path)

[<Fact>]
let ``writeAllDocuments nested file directory boundary writes separate artifacts`` () =
    let dataDir = newTempDir ()
    let graph, fileId, dirId, normalId = graphFileOwnsDirectory ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    let dirPath = artifactFullPath dataDir graph dirId
    Assert.Equal(Path.Combine(dataDir, "container.txt"), filePath)
    Assert.Equal(Path.Combine(dataDir, "inner", ".amb"), dirPath)
    Assert.True(File.Exists filePath)
    Assert.True(File.Exists dirPath)
    let dirText = File.ReadAllText dirPath
    let normalSid = AmbDocument.formatStableId normalId
    Assert.Contains("^" + normalSid, dirText)

[<Fact>]
let ``resolveArtifactPath unknown document root returns error`` () =
    let dataDir = newTempDir ()
    let graph = Graph.create ()
    let unknownId = NodeId.New()

    match DocumentPersistence.resolveArtifactPath dataDir graph unknownId with
    | Ok _ -> failwith "expected error"
    | Error _ -> ()

    Assert.False(Directory.EnumerateFiles(dataDir, "*", SearchOption.AllDirectories) |> Seq.exists (fun _ -> true))

[<Fact>]
let ``writeDocument round trip preserves member text`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, normalId = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "writeAllDocuments" |> ignore
    let filePath = artifactFullPath dataDir graph fileId
    let text = File.ReadAllText filePath

    match AmbDocument.read text fileId graph with
    | Error msg -> failwith msg
    | Ok result ->
        match Map.tryFind normalId result.nodes with
        | None -> failwith "member node missing after read"
        | Some node -> Assert.Equal("body", node.text)
