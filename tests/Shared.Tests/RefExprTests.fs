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
let ``parse accepts bare colon as all children step`` () =
    Assert.Equal(Path(Context, [ ChildStep None ]), parseOk ":")
    Assert.Equal(
        Path(Context, [ FileStep "@bobby"; ChildStep None ]),
        parseOk "@bobby:"
    )

[<Fact>]
let ``parse rejects postfix`` () =
    match RefExpr.parse "/x/y[0]" with
    | Error msg -> Assert.Contains("postfix", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``parse rejects at-colon as name pattern only`` () =
    match RefExpr.parse "@:" with
    | Ok (Path(Context, [ FileStep "@"; ChildStep None ])) -> ()
    | _ -> Assert.Fail("expected file step and all-children step")

[<Fact>]
let ``match_ bare colon selects all owned children`` () =
    let t = tree.Value
    let nodes = RefExpr.match_ (ctx ()) t.graph (parseOk ":")
    Assert.Equal<Set<NodeId>>(Set [ t.blueChild; t.plainChild ], ids nodes)

[<Fact>]
let ``parse accepts anchors only`` () =
    Assert.Equal(AnchorOnly WorkspaceRoot, parseOk "/")
    Assert.Equal(AnchorOnly GlobalRoot, parseOk "//")
    Assert.Equal(AnchorOnly Structural, parseOk "^")
    Assert.Equal(AnchorOnly CurrentDir, parseOk ".")
    Assert.Equal(AnchorOnly Tagged, parseOk "#")

[<Fact>]
let ``parse lexes dot slash as two tokens`` () =
    Assert.Equal(
        Path(CurrentDir, [ DirStep "folder" ]),
        parseOk "./folder/"
    )

[<Fact>]
let ``parse lexes dot name as one token`` () =
    Assert.Equal(Path(Context, [ FileStep ".amb" ]), parseOk ".amb")
    Assert.Equal(Path(Context, [ FileStep ".5" ]), parseOk ".5")

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

[<Fact>]
let ``parse accepts child index steps`` () =
    Assert.Equal(Path(Context, [ ChildStep None ]), parseOk ":")
    Assert.Equal(Path(Context, [ ChildStep(Some 0) ]), parseOk ":0")
    Assert.Equal(Path(Structural, [ ChildStep(Some 1) ]), parseOk "^:1")
    Assert.Equal(
        Path(Context, [ TagStep "blue"; ChildStep(Some 0) ]),
        parseOk "#blue:0"
    )

[<Fact>]
let ``parse round-trips child index steps`` () =
    for sample in [ ":"; ":0"; ":1"; "^:0"; "#blue:0" ] do
        let expr = parseOk sample
        let again = parseOk (RefExpr.format expr)
        Assert.Equal(expr, again)

[<Fact>]
let ``parse accepts index steps`` () =
    Assert.Equal(Path(Context, [ IndexStep None ]), parseOk "!")
    Assert.Equal(Path(Context, [ IndexStep(Some 1) ]), parseOk "!1")
    Assert.Equal(Path(Context, [ IndexStep(Some -2) ]), parseOk "!-2")
    Assert.Equal(Path(Structural, [ IndexStep(Some 1) ]), parseOk "^!1")

[<Fact>]
let ``parse round-trips index steps`` () =
    for sample in [ "!"; "!0"; "!1"; "!-1"; "^/#blue!-1" ] do
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
let ``match_ index current returns bases`` () =
    let t = tree.Value
    let expr = parseOk "!"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.contentFile ], ids nodes)

[<Fact>]
let ``match_ index sibling offset on owned children`` () =
    let t = tree.Value
    let ctx = RefExpr.refContext t.blueChild t.graph
    let next = RefExpr.match_ ctx t.graph (parseOk "!1")
    Assert.Equal<Set<NodeId>>(Set [ t.plainChild ], ids next)
    let prev = RefExpr.match_ ctx t.graph (parseOk "!-1")
    Assert.Empty(prev)

[<Fact>]
let ``match_ child index selects owned child by position`` () =
    let t = tree.Value
    let nodes0 = RefExpr.match_ (ctx ()) t.graph (parseOk ":0")
    Assert.Equal<Set<NodeId>>(Set [ t.blueChild ], ids nodes0)
    let nodes1 = RefExpr.match_ (ctx ()) t.graph (parseOk ":1")
    Assert.Equal<Set<NodeId>>(Set [ t.plainChild ], ids nodes1)
    Assert.Empty(RefExpr.match_ (ctx ()) t.graph (parseOk ":2"))

[<Fact>]
let ``match_ index sibling between files`` () =
    let t = tree.Value
    let ctx = RefExpr.refContext t.libFs t.graph
    let nodes = RefExpr.match_ ctx t.graph (parseOk "!-1")
    Assert.Equal<Set<NodeId>>(Set [ t.appFs ], ids nodes)
