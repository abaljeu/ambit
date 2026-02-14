module Gambol.Shared.Tests.SnapshotTests

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
    Assert.Equal("alpha\nbeta\ngamma\n", result)

[<Fact>]
let ``write createDag12 produces expected outline`` () =
    let graph = ModelBuilder.createDag12 ()
    let result = Snapshot.write graph
    let expected =
        "a\n"
        + "\td\n"
        + "\t\tj\n"
        + "\te\n"
        + "b\n"
        + "\tf\n"
        + "\t\tk\n"
        + "\tg\n"
        + "c\n"
        + "\th\n"
        + "\ti\n"
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
