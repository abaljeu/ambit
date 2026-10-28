module DocumentPersistenceTests.Fixtures

open Gambol.Shared

let requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private owned = ChildNode.owners

let private setChildren parentId kids (graph: Graph) =
    Graph.fromNodes
        graph.root
        graph.nodes
        (Map.add parentId kids graph.childMap)

let private attach parentId childIds label graph =
    Graph.replace parentId 0 [] (owned childIds) graph
    |> requireOk label

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private normalNode (id: NodeId) (text: string) (owner: NodeId) : Node =
    Node.Create(id, text = text, owner = owner)

let graphWithNestedDocs () : Graph * NodeId * NodeId * NodeId * NodeId =
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let normalId = NodeId.New()
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode (specialNode wsId Workspace "home" Graph.workspacesId)
        |> Graph.addDetachedNode (specialNode dirId Directory "docs" wsId)
        |> Graph.addDetachedNode (specialNode fileId File "readme.txt" dirId)
        |> Graph.addDetachedNode (normalNode normalId "body" fileId)
        |> attach Graph.workspacesId [ wsId ] "workspaces->ws"
        |> attach wsId [ dirId ] "ws->dir"
        |> attach dirId [ fileId ] "dir->file"
        |> attach fileId [ normalId ] "file->normal"
    graph, wsId, dirId, fileId, normalId

let graphFileOwnsDirectory () : Graph * NodeId * NodeId * NodeId =
    let fileId = NodeId.New()
    let dirId = NodeId.New()
    let normalId = NodeId.New()
    let graph1 =
        Graph.create ()
        |> Graph.addDetachedNode (specialNode fileId File "container.txt" Graph.rootId)
        |> Graph.addDetachedNode (specialNode dirId Directory "inner" fileId)
        |> Graph.addDetachedNode (normalNode normalId "nested" dirId)
        |> setChildren fileId (owned [ dirId ])
        |> setChildren dirId (owned [ normalId ])
    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        Graph.replace Graph.rootId idx [] (owned [ fileId ]) graph1
        |> requireOk "root->file"
    graph2, fileId, dirId, normalId

let graphWithRootFile () : Graph * NodeId =
    let fileId = NodeId.New()
    let graph1 =
        Graph.create ()
        |> Graph.addDetachedNode (specialNode fileId File "name.ext" Graph.rootId)
    let idx = Graph.fileTreeInsertIndex graph1 Graph.rootId
    let graph2 =
        Graph.replace Graph.rootId idx [] (owned [ fileId ]) graph1
        |> requireOk "root->file"
    graph2, fileId

let graphWithPlainFileRef () : Graph * NodeId * NodeId * NodeId =
    let wsId = NodeId.New()
    let fileId = NodeId.New()
    let holderId = NodeId.New()
    let sharedId = NodeId.New()
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode (specialNode wsId Workspace "home" Graph.workspacesId)
        |> Graph.addDetachedNode (specialNode fileId File "readme.txt" wsId)
        |> Graph.addDetachedNode (normalNode holderId "holder" fileId)
        |> Graph.addDetachedNode (normalNode sharedId "shared text" Graph.rootId)
        |> setChildren holderId [ ChildNode.reference sharedId ]
        |> attach Graph.workspacesId [ wsId ] "workspaces->ws"
        |> attach wsId [ fileId ] "ws->file"
        |> attach fileId [ holderId ] "file->holder"
    graph, wsId, fileId, sharedId

let graphWithTwoSiblingFiles () : Graph * NodeId * NodeId * NodeId * NodeId =
    let wsId = NodeId.New()
    let fileAId = NodeId.New()
    let fileBId = NodeId.New()
    let bodyAId = NodeId.New()
    let bodyBId = NodeId.New()
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode (specialNode wsId Workspace "home" Graph.workspacesId)
        |> Graph.addDetachedNode (specialNode fileAId File "a.txt" wsId)
        |> Graph.addDetachedNode (specialNode fileBId File "b.txt" wsId)
        |> Graph.addDetachedNode (normalNode bodyAId "alpha" fileAId)
        |> Graph.addDetachedNode (normalNode bodyBId "beta" fileBId)
        |> attach Graph.workspacesId [ wsId ] "workspaces->ws"
        |> attach wsId [ fileAId; fileBId ] "ws->files"
        |> attach fileAId [ bodyAId ] "fileA->body"
        |> attach fileBId [ bodyBId ] "fileB->body"
    graph, fileAId, fileBId, bodyAId, bodyBId

let graphWithSystemSecretBody () : Graph * NodeId * NodeId =
    let fileId = NodeId.New()
    let bodyId = NodeId.New()
    let fileNode =
        Node.Create(
            fileId,
            text = "secret.txt",
            name = Filename.Ok "secret.txt",
            owner = Graph.systemId,
            kind = Special File)
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode fileNode
        |> Graph.addDetachedNode (normalNode bodyId "old" fileId)
        |> attach Graph.systemId [ fileId ] "system->file"
        |> attach fileId [ bodyId ] "file->body"
    graph, fileId, bodyId

let graphWithEmptyRootFile () : Graph * NodeId =
    let fileId = NodeId.New()
    let fileNode =
        Node.Create(
            fileId,
            text = "",
            name = Filename.Empty,
            owner = Graph.rootId,
            kind = Special File,
            documentState = Unparsed)
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode fileNode
        |> attach Graph.rootId [ fileId ] "root->file"
    graph, fileId

// Bypass Filename.create: illicit Ok ".amb" that somehow exists.
let graphWithIllicitAmbFile () : Graph * NodeId * NodeId =
    let wsId = NodeId.New()
    let fileId = NodeId.New()
    let fileNode =
        Node.Create(
            fileId,
            text = ".amb",
            name = Filename.Ok ".amb",
            owner = wsId,
            kind = Special File)
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode (specialNode wsId Workspace "home" Graph.workspacesId)
        |> Graph.addDetachedNode fileNode
        |> attach Graph.workspacesId [ wsId ] "workspaces->ws"
        |> attach wsId [ fileId ] "ws->file"
    graph, wsId, fileId

let graphWithSystemFile (fileName: string) : Graph * NodeId =
    let fileId = NodeId.New()
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode (specialNode fileId File fileName Graph.systemId)
        |> attach Graph.systemId [ fileId ] "system->file"
    graph, fileId

let artifactFullPath (dataDir: string) (graph: Graph) (documentRootId: NodeId) =
    Gambol.Server.DocumentPersistPath.resolveArtifactPath dataDir graph documentRootId
    |> requireOk "resolveArtifactPath"

let applyOps (graph: Graph) (ops: Op list) : Graph =
    ops
    |> List.fold
        (fun state op ->
            match Op.apply op state with
            | ApplyResult.Changed next
            | ApplyResult.Unchanged next -> next
            | ApplyResult.Invalid(_, error) -> failwith error)
        { graph = graph; eventId = EventId.zero }
    |> fun state -> state.graph
