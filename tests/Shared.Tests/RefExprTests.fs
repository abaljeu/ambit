module RefExprTests

open Gambol.Shared
open RefExprTestTree
open Xunit

let private tree = lazy build ()
let private ctx () = refContext tree.Value

let private parseOk input =
    match RefExpr.parse input with
    | Ok expr -> expr
    | Error err -> failwith $"parse failed: {err}"

let private ids results = results |> List.map (fun r -> r.nodeId) |> Set.ofList

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

// ---- parse ----

[<Fact>]
let ``parse rejects empty input`` () =
    match RefExpr.parse "   " with
    | Error msg -> Assert.Contains("empty", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``parse rejects quoted path segment`` () =
    match RefExpr.parse "/ \"open" with
    | Error msg -> Assert.Contains("quoted", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``parse rejects old workspace anchor syntax`` () =
    match RefExpr.parse "@bobby:" with
    | Error msg -> Assert.Contains("workspace", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``parse rejects postfix`` () =
    match RefExpr.parse "/x/y[0]" with
    | Error msg -> Assert.Contains("postfix", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``parse rejects old at-colon workspace root syntax`` () =
    match RefExpr.parse "@:" with
    | Error msg -> Assert.Contains("workspace", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``parse accepts anchors only`` () =
    Assert.Equal(AnchorOnly WorkspaceRoot, parseOk "/")
    Assert.Equal(AnchorOnly GlobalRoot, parseOk "//")
    Assert.Equal(AnchorOnly Structural, parseOk "^")
    Assert.Equal(AnchorOnly CurrentDir, parseOk ".")
    Assert.Equal(AnchorOnly Tagged, parseOk "#")

[<Fact>]
let ``parse accepts named workspace as root-relative path`` () =
    Assert.Equal(
        Path(GlobalRoot, [ DirStep "@bobby"; DirStep "src" ]),
        parseOk "//@bobby/src/"
    )

[<Fact>]
let ``parse accepts dir and file steps with wildcards`` () =
    Assert.Equal(
        Path(WorkspaceRoot, [ DirStep "src"; DirStep "*"; FileStep "app.fs" ]),
        parseOk "/ src / * / app.fs"
    )

    Assert.Equal(Path(Structural, [ MultiWild; TagStep "blue" ]), parseOk "^ / ** / #blue")

[<Fact>]
let ``parse disambiguates hash anchor from tag step`` () =
    Assert.Equal(AnchorOnly Tagged, parseOk "#")
    Assert.Equal(Path(Context, [ TagStep "todo" ]), parseOk "#todo")

[<Fact>]
let ``parse round-trips all step kinds`` () =
    let samples =
        [ "/"
          "//"
          "^"
          "."
          "#"
          "//@bobby/src/lib.fs"
          "/docs/readme.md"
          "^/**/#blue"
          "//@ws/src/*.fs" ]

    for sample in samples do
        let expr = parseOk sample
        let again = parseOk (RefExpr.format expr)
        Assert.Equal(expr, again)

// ---- refContext ----

[<Fact>]
let ``refContext from file node resolves owner chain`` () =
    let t = tree.Value
    let ctx = RefExpr.refContext t.contentFile t.graph
    Assert.Equal(t.contentFile, ctx.contextNode)
    Assert.Equal(Some t.workspaceRoot, ctx.workspaceRoot)
    Assert.Equal(Some t.contentFile, ctx.structural)
    Assert.Equal(Some t.contentFileDir, ctx.currentDir)
    Assert.Equal(None, ctx.tagged)

[<Fact>]
let ``refContext tagged from named normal node`` () =
    let t = tree.Value
    let ctx = RefExpr.refContext t.nestedBlue t.graph
    Assert.Equal(Some t.nestedBlue, ctx.tagged)

// ---- match: anchors ----

[<Fact>]
let ``match_ workspace root returns workspace node`` () =
    let t = tree.Value
    let nodes = RefExpr.match_ (ctx ()) t.graph (AnchorOnly WorkspaceRoot)
    Assert.Equal<Set<NodeId>>(Set [ t.workspaceRoot ], ids nodes)

[<Fact>]
let ``match_ global root returns ROOT`` () =
    let t = tree.Value
    let nodes = RefExpr.match_ (ctx ()) t.graph (AnchorOnly GlobalRoot)
    Assert.Equal<Set<NodeId>>(Set [ Graph.rootId ], ids nodes)

[<Fact>]
let ``match_ structural and current dir resolve`` () =
    let t = tree.Value
    let fileNodes = RefExpr.match_ (ctx ()) t.graph (AnchorOnly Structural)
    Assert.Equal<Set<NodeId>>(Set [ t.contentFile ], ids fileNodes)
    let dirNodes = RefExpr.match_ (ctx ()) t.graph (AnchorOnly CurrentDir)
    Assert.Equal<Set<NodeId>>(Set [ t.contentFileDir ], ids dirNodes)

[<Fact>]
let ``match_ tagged anchor from named normal context`` () =
    let t = tree.Value
    let ctx = RefExpr.refContext t.nestedBlue t.graph
    let nodes = RefExpr.match_ ctx t.graph (AnchorOnly Tagged)
    Assert.Equal<Set<NodeId>>(Set [ t.nestedBlue ], ids nodes)

[<Fact>]
let ``match_ workspace root from outline context resolves to ROOT`` () =
    let graph0 = Graph.create ()
    let graph1, focusIds = ModelBuilder.createNodes [ "focus" ] graph0
    let focus = focusIds.[0]
    let graph2 =
        match Graph.replace graph1.root 0 [] (owned [ focus ]) graph1 with
        | Ok g -> g
        | Error e -> failwith e
    let ctx = RefExpr.refContext focus graph2
    Assert.Equal(Some Graph.rootId, ctx.workspaceRoot)
    let nodes = RefExpr.match_ ctx graph2 (AnchorOnly WorkspaceRoot)
    Assert.Equal<Set<NodeId>>(Set [ Graph.rootId ], ids nodes)

// ---- match: paths ----

[<Fact>]
let ``match_ resolves exact path under root-relative workspace`` () =
    let t = tree.Value
    let expr = parseOk "//@bobby/src/app.fs"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.appFs ], ids nodes)

[<Fact>]
let ``match_ path miss returns empty`` () =
    let t = tree.Value
    let expr = parseOk "//@bobby/src/missing.fs"
    Assert.Empty(RefExpr.match_ (ctx ()) t.graph expr)

[<Fact>]
let ``match_ glob file step under directory`` () =
    let t = tree.Value
    let expr = parseOk "//@bobby/src/*"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.appFs; t.libFs; t.embeddedMd ], ids nodes)

[<Fact>]
let ``match_ glob pattern on file names`` () =
    let t = tree.Value
    let expr = parseOk "//@bobby/src/*.fs"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.appFs; t.libFs ], ids nodes)

// ---- match: tags ----

[<Fact>]
let ``match_ tag step on file content`` () =
    let t = tree.Value
    let expr = parseOk "^/#blue"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.blueChild; t.nestedBlue ], ids nodes)

[<Fact>]
let ``match_ multi wildcard then tag across depths`` () =
    let t = tree.Value
    let expr = parseOk "^/**/#blue"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.blueChild; t.nestedBlue ], ids nodes)

[<Fact>]
let ``match_ nested tag steps`` () =
    let t = tree.Value
    let expr = parseOk "^/#blue/#blue"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.nestedBlue ], ids nodes)

[<Fact>]
let ``match_ file step after tag step searches from tagged nodes`` () =
    let t = tree.Value
    let expr = parseOk "^/#blue/embedded.md"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.embeddedMd ], ids nodes)
