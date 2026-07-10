/// Reusable workspace and file subtree for RefExpr matcher tests.
module RefExprTestTree

open Gambol.Shared

type Tree =
    { graph: Graph
      workspaceRoot: NodeId
      bobbySrc: NodeId
      appFs: NodeId
      libFs: NodeId
      readmeMd: NodeId
      contentFile: NodeId
      contentFileDir: NodeId
      embeddedMd: NodeId
      blueChild: NodeId
      nestedBlue: NodeId
      plainChild: NodeId
      taggedAncestor: NodeId }

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private addUnder (parentId: NodeId) (child: Node) (graph: Graph) : Graph =
    let parent = graph.nodes.[parentId]
    let link = { ref = Ownership.Owner; id = child.id }
    let nodes =
        graph.nodes
        |> Map.add child.id child
        |> Map.add parentId { parent with children = parent.children @ [ link ] }
    Graph.fromNodes graph.root nodes

let private namedNormalNode (text: string) (tagName: string) (owner: NodeId) : Node =
    Node.Create(
        NodeId.New(),
        text = text,
        name = Filename.create tagName,
        owner = owner)

/// Workspace tree under Workspaces plus a tagged outline under `app.fs`.
let build () : Tree =
    let graph0 = Graph.create ()
    let bobbyId = NodeId.New()
    let otherId = NodeId.New()
    let srcId = NodeId.New()
    let docsId = NodeId.New()
    let appId = NodeId.New()
    let libId = NodeId.New()
    let readmeId = NodeId.New()
    let embeddedId = NodeId.New()

    let graph1 =
        graph0
        |> addUnder Graph.workspacesId (specialNode bobbyId Workspace "bobby" Graph.workspacesId)
        |> addUnder Graph.workspacesId (specialNode otherId Workspace "other" Graph.workspacesId)

    let graph2 =
        graph1
        |> addUnder bobbyId (specialNode srcId Directory "src" bobbyId)
        |> addUnder bobbyId (specialNode docsId Directory "docs" bobbyId)

    let graph3 =
        graph2
        |> addUnder srcId (specialNode appId File "app.fs" srcId)
        |> addUnder srcId (specialNode libId File "lib.fs" srcId)
        |> addUnder docsId (specialNode readmeId File "readme.md" docsId)

    let alpha = namedNormalNode "alpha" "blue" appId
    let beta = namedNormalNode "beta" "blue" alpha.id
    let gamma = namedNormalNode "gamma" "plain" appId

    let graph4 =
        graph3
        |> addUnder appId alpha
        |> addUnder alpha.id beta
        |> addUnder appId gamma
        |> addUnder alpha.id (specialNode embeddedId File "embedded.md" alpha.id)

    { graph = graph4
      workspaceRoot = bobbyId
      bobbySrc = srcId
      appFs = appId
      libFs = libId
      readmeMd = readmeId
      contentFile = appId
      contentFileDir = srcId
      embeddedMd = embeddedId
      blueChild = alpha.id
      nestedBlue = beta.id
      plainChild = gamma.id
      taggedAncestor = beta.id }

let refContext (tree: Tree) : RefContext =
    RefExpr.refContext tree.contentFile tree.graph

let nodeIds (nodes: Node list) : NodeId list = nodes |> List.map (fun n -> n.id)
