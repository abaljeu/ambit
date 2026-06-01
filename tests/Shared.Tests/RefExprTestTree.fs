/// Reusable workspace and file subtree for RefExpr matcher tests.
module RefExprTestTree

open Gambol.Shared

type Tree =
    { graph: Graph
      workspaceRoot: NodeId
      namedWorkspaces: Map<string, NodeId>
      bobbySrc: NodeId
      appFs: NodeId
      libFs: NodeId
      readmeMd: NodeId
      contentFile: NodeId
      contentFileDir: NodeId
      blueChild: NodeId
      nestedBlue: NodeId
      plainChild: NodeId }

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    { id = id
      text = name
      name = Filename.Ok name
      children = []
      cssClasses = CssClass.empty
      owner = owner
      kind = Special kind }

let private addUnder (parentId: NodeId) (child: Node) (graph: Graph) : Graph =
    let parent = graph.nodes.[parentId]
    let link = { ref = Ownership.Owner; id = child.id }
    let nodes =
        graph.nodes
        |> Map.add child.id child
        |> Map.add parentId { parent with children = parent.children @ [ link ] }
    Graph.fromNodes graph.root nodes

let private normalNode (text: string) (classes: CssClasses) (owner: NodeId) : Node =
    { id = NodeId.New()
      text = text
      name = Filename.Empty
      children = []
      cssClasses = classes
      owner = owner
      kind = Normal }

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

    let alpha = normalNode "alpha" (CssClass.ofList [ "blue" ]) appId
    let beta = normalNode "beta" (CssClass.ofList [ "blue" ]) alpha.id
    let gamma = normalNode "gamma" CssClass.empty appId

    let graph4 =
        graph3
        |> addUnder appId alpha
        |> addUnder alpha.id beta
        |> addUnder appId gamma

    { graph = graph4
      workspaceRoot = bobbyId
      namedWorkspaces = Map [ "bobby", bobbyId; "other", otherId ]
      bobbySrc = srcId
      appFs = appId
      libFs = libId
      readmeMd = readmeId
      contentFile = appId
      contentFileDir = srcId
      blueChild = alpha.id
      nestedBlue = beta.id
      plainChild = gamma.id }

let refContext (tree: Tree) : RefContext =
    { workspaceRoot = Some tree.workspaceRoot
      fileRoot = Some tree.contentFile
      fileDir = Some tree.contentFileDir
      namedWorkspaces = tree.namedWorkspaces }

let nodeIds (nodes: Node list) : NodeId list = nodes |> List.map (fun n -> n.id)
