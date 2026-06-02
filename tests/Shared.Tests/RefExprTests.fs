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

// ---- parse ----

[<Fact>]
let ``parse rejects empty input`` () =
    match RefExpr.parse "   " with
    | Error msg -> Assert.Contains("empty", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``parse rejects unclosed string`` () =
    match RefExpr.parse "/ \"open" with
    | Error msg -> Assert.Contains("unclosed", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``parse rejects workspace without colon`` () =
    match RefExpr.parse "@bobby" with
    | Error msg -> Assert.Contains(":", msg)
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``parse accepts bases only`` () =
    Assert.Equal(BaseOnly WorkspaceRoot, parseOk "/")
    Assert.Equal(BaseOnly FileRoot, parseOk "^")
    Assert.Equal(BaseOnly FileDir, parseOk ".")
    Assert.Equal(BaseOnly(NamedWorkspace "bobby"), parseOk "@bobby:")

[<Fact>]
let ``parse accepts named workspace with attached step`` () =
    Assert.Equal(Path(NamedWorkspace "bobby", [ NameStep "src" ]), parseOk "@bobby:src")

[<Fact>]
let ``parse accepts slash-separated steps and wildcards`` () =
    Assert.Equal(
        Path(WorkspaceRoot, [ NameStep "src"; SingleWild; NameStep "app.fs" ]),
        parseOk "/ src / * / app.fs"
    )

    Assert.Equal(Path(FileRoot, [ MultiWild; TagStep "blue" ]), parseOk "^ / ** / #blue")

[<Fact>]
let ``parse round-trips all step kinds`` () =
    let samples =
        [ "/"
          "^"
          "."
          "@bobby:"
          "@bobby:src/lib.fs"
          "/docs/readme.md"
          "^/**/#blue"
          "@ws:src/*.fs"
          "/ \"My Folder\" / \"File Name.md\"" ]

    for sample in samples do
        let expr = parseOk sample
        let again = parseOk (RefExpr.format expr)
        Assert.Equal(expr, again)

// ---- refContext ----

[<Fact>]
let ``refContext from file node resolves owner chain and named workspaces`` () =
    let t = tree.Value
    let ctx = RefExpr.refContext t.contentFile t.graph
    Assert.Equal(Some t.workspaceRoot, ctx.workspaceRoot)
    Assert.Equal(Some t.contentFile, ctx.fileRoot)
    Assert.Equal(Some t.contentFileDir, ctx.fileDir)
    Assert.Equal<Map<string, NodeId>>(t.namedWorkspaces, ctx.namedWorkspaces)

// ---- match: bases ----

[<Fact>]
let ``match_ workspace root returns workspace node`` () =
    let t = tree.Value
    let nodes = RefExpr.match_ (ctx ()) t.graph (BaseOnly WorkspaceRoot)
    Assert.Equal<Set<NodeId>>(Set [ t.workspaceRoot ], ids nodes)

[<Fact>]
let ``match_ file root and file dir resolve`` () =
    let t = tree.Value
    let fileNodes = RefExpr.match_ (ctx ()) t.graph (BaseOnly FileRoot)
    Assert.Equal<Set<NodeId>>(Set [ t.contentFile ], ids fileNodes)
    let dirNodes = RefExpr.match_ (ctx ()) t.graph (BaseOnly FileDir)
    Assert.Equal<Set<NodeId>>(Set [ t.contentFileDir ], ids dirNodes)

[<Fact>]
let ``match_ named workspace resolves and misses`` () =
    let t = tree.Value
    let hit = RefExpr.match_ (ctx ()) t.graph (BaseOnly(NamedWorkspace "bobby"))
    Assert.Equal<Set<NodeId>>(Set [ t.workspaceRoot ], ids hit)
    let miss = RefExpr.match_ (ctx ()) t.graph (BaseOnly(NamedWorkspace "missing"))
    Assert.Empty(miss)

[<Fact>]
let ``match_ unresolved base returns empty`` () =
    let emptyCtx =
        { workspaceRoot = None
          fileRoot = None
          fileDir = None
          namedWorkspaces = Map.empty }

    Assert.Empty(RefExpr.match_ emptyCtx tree.Value.graph (BaseOnly WorkspaceRoot))

// ---- match: names ----

[<Fact>]
let ``match_ resolves exact path under named workspace`` () =
    let t = tree.Value
    let expr = parseOk "@bobby:src/app.fs"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.appFs ], ids nodes)

[<Fact>]
let ``match_ path miss returns empty`` () =
    let t = tree.Value
    let expr = parseOk "@bobby:src/missing.fs"
    Assert.Empty(RefExpr.match_ (ctx ()) t.graph expr)

[<Fact>]
let ``match_ single wildcard at one level`` () =
    let t = tree.Value
    let expr = parseOk "@bobby:src/*"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.appFs; t.libFs ], ids nodes)

[<Fact>]
let ``match_ glob pattern on file names`` () =
    let t = tree.Value
    let expr = parseOk "@bobby:src/*.fs"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.appFs; t.libFs ], ids nodes)

// ---- match: tags ----

[<Fact>]
let ``match_ tag step on direct file children`` () =
    let t = tree.Value
    let expr = parseOk "^/#blue"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.blueChild ], ids nodes)

[<Fact>]
let ``match_ multi wildcard then tag across depths`` () =
    let t = tree.Value
    let expr = parseOk "^/**/#blue"
    let nodes = RefExpr.match_ (ctx ()) t.graph expr
    Assert.Equal<Set<NodeId>>(Set [ t.blueChild; t.nestedBlue ], ids nodes)
