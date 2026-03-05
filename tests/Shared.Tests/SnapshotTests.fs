module Gambol.Shared.Tests.SnapshotTests

open System
open Xunit
open Gambol.Shared

/// Extract the tree shape as (depth, text) pairs via depth-first traversal.
/// The root is excluded; its children are depth 0.
let private treeShape (graph: Graph) : (int * string) list =
    let rec walk depth nodeId =
        let node = graph.nodes.[nodeId]
        (depth, node.text) :: (node.children |> List.collect (walk (depth + 1)))

    let root = graph.nodes.[graph.root]
    root.children |> List.collect (walk 0)

// ---- write tests ----

[<Fact>]
let ``write empty graph produces empty string`` () =
    let graph = Graph.create ()
    let result = Snapshot.write graph
    Assert.Equal("", result)

[<Fact>]
let ``write flat graph produces unindented lines`` () =
    let graph = Graph.create ()
    let graph, ids = ModelBuilder.createNodes [ "alpha"; "beta"; "gamma" ] graph
    let graph =
        Graph.replace graph.root 0 [] ids graph
        |> ModelBuilder.requireOk "test"
    let result = Snapshot.write graph
    Assert.Equal("alpha" + Environment.NewLine + "beta" + Environment.NewLine + "gamma" + Environment.NewLine, result)

[<Fact>]
let ``write createDag12 produces expected outline`` () =
    let graph = ModelBuilder.createDag12 ()
    let result = Snapshot.write graph
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
    Assert.Empty(root.children)
    Assert.Equal(1, Graph.nodeCount graph) // root only

[<Fact>]
let ``read flat lines produces root with children`` () =
    let graph = Snapshot.read "alpha\nbeta\ngamma\n"
    let root = graph.nodes.[graph.root]
    Assert.Equal(3, root.children.Length)
    let texts = root.children |> List.map (fun id -> graph.nodes.[id].text)
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
    let text = Snapshot.write graph
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
    Assert.Equal(2, root.children.Length)
    let p1 = graph.nodes.[root.children.[0]]
    let p2 = graph.nodes.[root.children.[1]]
    Assert.Equal("parent1", p1.text)
    Assert.Equal("parent2", p2.text)
    Assert.Equal(1, p1.children.Length)
    Assert.Equal(1, p2.children.Length)
    Assert.Equal(p1.children.[0], p2.children.[0])   // same NodeId
    Assert.Equal("shared", graph.nodes.[p1.children.[0]].text)
    Assert.Equal(4, Graph.nodeCount graph)             // root + parent1 + parent2 + shared

// ---- shared-node round-trip ----

[<Fact>]
let ``round-trip shared-node graph preserves shape and sharing`` () =
    let original = ModelBuilder.createSharedNodeGraph ()
    let decoded = original |> Snapshot.write |> Snapshot.read
    Assert.Equal<(int * string) list>(treeShape original, treeShape decoded)
    Assert.Equal(4, Graph.nodeCount decoded)            // root + parent1 + parent2 + shared
    let root = decoded.nodes.[decoded.root]
    let p1 = decoded.nodes.[root.children.[0]]
    let p2 = decoded.nodes.[root.children.[1]]
    Assert.Equal(p1.children.[0], p2.children.[0])     // truly shared NodeId

// ---- backward compatibility: plain lines still load correctly ----

[<Fact>]
let ``read old-format snapshot without hash markers loads unchanged`` () =
    let text = "a\n\tb\n\t\tc\nd\n"
    let graph = Snapshot.read text
    let expected = [ (0,"a"); (1,"b"); (2,"c"); (0,"d") ]
    Assert.Equal<(int * string) list>(expected, treeShape graph)
