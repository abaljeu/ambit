module Gambol.Shared.Tests.SnapshotTests

open System
open Xunit
open Gambol.Shared
open SpecialNodeTestHelpers

let private childId (child: ChildNode) = child.id
let private owned (ids: NodeId list) = ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

/// Extract the tree shape as (depth, text) pairs via depth-first traversal.
/// The root is excluded; its children are depth 0.
let private treeShape (graph: Graph) : (int * string) list =
    // Legacy tests should ignore special nodes like TRASH; reuse shared helper.
    userTreeShape graph

// ---- write tests ----

[<Fact>]
let ``write empty graph produces empty string`` () =
    let graph = Graph.create ()
    let result = Snapshot.write graph
    Assert.Equal("", stripSpecialLinesFromOutline result)

[<Fact>]
let ``write flat graph produces unindented lines`` () =
    let graph = Graph.create ()
    let graph, ids = ModelBuilder.createNodes [ "alpha"; "beta"; "gamma" ] graph
    let graph =
        Graph.replace graph.root 0 [] (owned ids) graph
        |> ModelBuilder.requireOk "test"
    let result = Snapshot.write graph |> stripSpecialLinesFromOutline
    Assert.Equal("alpha" + Environment.NewLine + "beta" + Environment.NewLine + "gamma" + Environment.NewLine, result)

[<Fact>]
let ``write createDag12 produces expected outline`` () =
    let graph = ModelBuilder.createDag12 ()
    let result = Snapshot.write graph |> stripSpecialLinesFromOutline
    let nl = Environment.NewLine
    let expected =
        "a" + nl
        + "\td" + nl
        + "\t\tj" + nl
        + "\te" + nl
        + "b" + nl
        + "\tf" + nl
        + "\t\tk" + nl
        + "\tg" + nl
        + "c" + nl
        + "\th" + nl
        + "\ti" + nl
    Assert.Equal(expected, result)

// ---- read tests ----

[<Fact>]
let ``read empty string produces empty graph`` () =
    let graph = Snapshot.read ""
    let root = graph.nodes.[graph.root]
    Assert.Equal(Graph.rootId, graph.root)
    Assert.Equal("ROOT", root.text)
    Assert.Empty(userRootChildren graph)
    Assert.Equal(0, userNodeCount graph)

[<Fact>]
let ``read flat lines produces root with children`` () =
    let graph = Snapshot.read "alpha\nbeta\ngamma\n"
    let rootChildren = userRootChildren graph
    Assert.Equal(3, rootChildren.Length)
    let texts = rootChildren |> List.map (fun child -> graph.nodes.[child.id].text)
    Assert.Equal<string list>([ "alpha"; "beta"; "gamma" ], texts)

[<Fact>]
let ``read nested text produces correct tree`` () =
    let text = "a\n\tb\n\t\tc\n"
    let graph = Snapshot.read text
    let shape = treeShape graph
    let expected = [ (0, "a"); (1, "b"); (2, "c") ]
    Assert.Equal<(int * string) list>(expected, shape)

[<Fact>]
let ``read handles Windows line endings`` () =
    let text = "a\r\n\tb\r\n"
    let graph = Snapshot.read text
    let shape = treeShape graph
    Assert.Equal<(int * string) list>([ (0, "a"); (1, "b") ], shape)

// ---- round-trip tests ----

[<Fact>]
let ``round-trip empty graph`` () =
    let original = Graph.create ()
    let decoded = original |> Snapshot.write |> Snapshot.read
    Assert.Equal(Graph.rootId, decoded.root)
    Assert.Equal("ROOT", decoded.nodes.[decoded.root].text)
    Assert.Equal<(int * string) list>(treeShape original, treeShape decoded)

[<Fact>]
let ``round-trip createDag12`` () =
    let original = ModelBuilder.createDag12 ()
    let decoded = original |> Snapshot.write |> Snapshot.read
    Assert.Equal<(int * string) list>(treeShape original, treeShape decoded)

// ---- file I/O round-trip ----

[<Fact>]
let ``file write then read preserves tree`` () =
    let original = ModelBuilder.createDag12 ()
    let path = System.IO.Path.GetTempFileName()
    try
        System.IO.File.WriteAllText(path, Snapshot.write original)
        let text = System.IO.File.ReadAllText(path)
        let decoded = Snapshot.read text
        Assert.Equal<(int * string) list>(treeShape original, treeShape decoded)
    finally
        System.IO.File.Delete(path)

// ---- shared-node (multi-occurrence) write ----

[<Fact>]
let ``write shared node emits hash on first visit and arrow on subsequent`` () =
    let graph = ModelBuilder.createSharedNodeGraph ()
    let text = Snapshot.write graph |> stripSpecialLinesFromOutline
    let nl = Environment.NewLine
    let expected =
        "parent1" + nl
        + "\t#n1 shared" + nl
        + "parent2" + nl
        + "\t-> #n1" + nl
    Assert.Equal(expected, text)

// ---- shared-node read ----

[<Fact>]
let ``read shared-node format produces shared NodeId`` () =
    let text = "parent1\n\t#n1 shared\nparent2\n\t-> #n1\n"
    let graph = Snapshot.read text
    let root = graph.nodes.[graph.root]
    let userChildren = userRootChildren graph
    Assert.Equal(2, userChildren.Length)
    let p1 = graph.nodes.[userChildren.[0].id]
    let p2 = graph.nodes.[userChildren.[1].id]
    Assert.Equal("parent1", p1.text)
    Assert.Equal("parent2", p2.text)
    Assert.Equal(1, p1.children.Length)
    Assert.Equal(1, p2.children.Length)
    Assert.Equal(p1.children.[0].id, p2.children.[0].id)   // same NodeId
    Assert.Equal(Ownership.Owner, p1.children.[0].ref)
    Assert.Equal(Ownership.Ref, p2.children.[0].ref)
    Assert.Equal("shared", graph.nodes.[p1.children.[0].id].text)
    Assert.Equal(3, userNodeCount graph)             // parent1 + parent2 + shared

[<Fact>]
let ``read ref-before-owner creates stub then merges owner`` () =
    let nl = Environment.NewLine
    let text =
        "parent2" + nl + "\t-> #n1" + nl + "parent1" + nl + "\t#n1 shared" + nl
    let graph = Snapshot.read text
    let userChildren = userRootChildren graph
    Assert.Equal(2, userChildren.Length)
    let p2 = graph.nodes.[userChildren.[0].id]
    let p1 = graph.nodes.[userChildren.[1].id]
    Assert.Equal("parent2", p2.text)
    Assert.Equal("parent1", p1.text)
    Assert.Equal(Ownership.Ref, p2.children.[0].ref)
    Assert.Equal(Ownership.Owner, p1.children.[0].ref)
    Assert.Equal(p1.children.[0].id, p2.children.[0].id)
    Assert.Equal("shared", graph.nodes.[p1.children.[0].id].text)
    Assert.Equal(3, userNodeCount graph)

let private createSharedNodeGraphRefParentFirst () : Graph =
    let g = ModelBuilder.createSharedNodeGraph ()
    let root = g.nodes.[g.root]
    let ch = root.children
    Graph.replace g.root 0 ch (List.rev ch) g |> ModelBuilder.requireOk "reorder root children"

// ---- shared-node round-trip ----

[<Fact>]
let ``round-trip shared-node graph preserves shape and sharing`` () =
    let original = ModelBuilder.createSharedNodeGraph ()
    let decoded = original |> Snapshot.write |> Snapshot.read
    Assert.Equal<(int * string) list>(treeShape original, treeShape decoded)
    Assert.Equal(3, userNodeCount decoded)            // parent1 + parent2 + shared
    let userChildren = userRootChildren decoded
    let p1 = decoded.nodes.[userChildren.[0].id]
    let p2 = decoded.nodes.[userChildren.[1].id]
    Assert.Equal(p1.children.[0].id, p2.children.[0].id)     // truly shared NodeId
    Assert.Equal(Ownership.Owner, p1.children.[0].ref)
    Assert.Equal(Ownership.Ref, p2.children.[0].ref)

[<Fact>]
let ``write ref parent before owner emits arrow before hash definition`` () =
    let original = createSharedNodeGraphRefParentFirst ()
    let text = Snapshot.write original
    let arrowIdx = text.IndexOf("-> #")
    let hashIdx = text.IndexOf("#n1 ")
    Assert.True(arrowIdx >= 0, "expected -> # line")
    Assert.True(hashIdx > arrowIdx, "ref line should precede owner definition")
    let decoded = Snapshot.read text
    Assert.Equal<(int * string) list>(treeShape original, treeShape decoded)
    let root = decoded.nodes.[decoded.root]
    Assert.Equal(decoded.nodes.[root.children.[0].id].children.[0].id,
                 decoded.nodes.[root.children.[1].id].children.[0].id)

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    { Node.create id with
        text = name
        name = Filename.create name
        owner = owner
        kind = Special kind }

let private graphWithWorkspaceTree () : Graph =
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
        |> ModelBuilder.requireOk "workspaces->ws"

    let graph3 =
        Graph.replace wsId 0 [] (owned [ dirId ]) graph2
        |> ModelBuilder.requireOk "ws->dir"

    Graph.replace dirId 0 [] (owned [ fileId ]) graph3
    |> ModelBuilder.requireOk "dir->file"

[<Fact>]
let ``write workspace emits label path body`` () =
    let graph = graphWithWorkspaceTree ()
    let text = Snapshot.write graph
    Assert.Contains("//home", text)
    Assert.DoesNotContain("//home/docs", text)
    Assert.DoesNotContain("//home/docs/readme.txt", text)
    Assert.DoesNotContain("#n1 home", text)

[<Fact>]
let ``normalizeOutlineForCompare treats CRLF and LF the same`` () =
    let a = "x\r\ny"
    let b = "x\ny"
    Assert.Equal(Snapshot.normalizeOutlineForCompare a, Snapshot.normalizeOutlineForCompare b)

[<Fact>]
let ``describeOutlineMismatch reports first differing code point`` () =
    let msg = Snapshot.describeOutlineMismatch "hello" "hallo"
    Assert.Contains("first differing", msg)
    Assert.Contains("U+", msg)

// ---- backward compatibility: plain lines still load correctly ----

[<Fact>]
let ``read old-format snapshot without hash markers loads unchanged`` () =
    let text = "a\n\tb\n\t\tc\nd\n"
    let graph = Snapshot.read text
    let expected = [ (0,"a"); (1,"b"); (2,"c"); (0,"d") ]
    Assert.Equal<(int * string) list>(expected, treeShape graph)
