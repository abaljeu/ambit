module ExprPathClusterParseTests

open Gambol.Shared
open Xunit

let private clusterOk input =
    match ExprPathClusterParse.parse input with
    | Ok steps -> steps
    | Error err -> failwith $"parse failed: {err}"

let private clusterErr input =
    match ExprPathClusterParse.parse input with
    | Error err -> err
    | Ok _ -> failwith "expected parse error"

let private exprOk input =
    match ExprParse.parseExpr input with
    | Ok terms -> terms
    | Error err -> failwith $"parse failed: {err}"

let private exprErr input =
    match ExprParse.parseExpr input with
    | Error err -> err
    | Ok _ -> failwith "expected parse error"

// ---- // desugar and bare operators ----

[<Fact>]
let ``//ws desugars to root slash ws`` () =
    let expected: PathCluster =
        [ ClusterStep.Root; ClusterStep.Structural "ws" ]
    Assert.Equal<PathCluster>(expected, clusterOk "//ws")

[<Fact>]
let ``root slash ws cluster matches spaced structural search`` () =
    let fromRoot = exprOk "root /ws"
    let fromQuoted = exprOk "root / \"ws\""
    let expected: ExprSeq =
        [ ExprTerm.Word("root", None)
          ExprTerm.Cluster([ ClusterStep.Structural "ws" ], None) ]
    Assert.Equal<ExprSeq>(expected, fromRoot)
    Assert.Equal<ExprSeq>(expected, fromQuoted)

[<Fact>]
let ``bare double slash is missing argument`` () =
    Assert.Contains("missing argument", clusterErr "//")

[<Fact>]
let ``bare slash is missing argument`` () =
    Assert.Contains("missing argument", clusterErr "/")

[<Fact>]
let ``bare hash is missing argument`` () =
    Assert.Contains("missing argument", clusterErr "#")

// ---- implicit structural names ----

[<Fact>]
let ``a slash b slash c uses implicit structural steps`` () =
    let expected: PathCluster =
        [ ClusterStep.Structural "a"
          ClusterStep.Structural "b"
          ClusterStep.Structural "c" ]
    Assert.Equal<PathCluster>(expected, clusterOk "a/b/c")

// ---- child and sibling all ----

[<Fact>]
let ``colon star is child all`` () =
    let expected: PathCluster = [ ClusterStep.ChildAt None ]
    Assert.Equal<PathCluster>(expected, clusterOk ":*")

[<Fact>]
let ``bang star is sibling all`` () =
    let expected: PathCluster = [ ClusterStep.SiblingAt None ]
    Assert.Equal<PathCluster>(expected, clusterOk "!*")

[<Fact>]
let ``bare colon is missing argument`` () =
    Assert.Contains("missing argument", clusterErr ":")

[<Fact>]
let ``bare bang is missing argument`` () =
    Assert.Contains("missing argument", clusterErr "!")

// ---- spaced quoted name arguments ----

[<Fact>]
let ``spaced quoted structural name argument`` () =
    let expected: ExprSeq =
        [ ExprTerm.Word("x", None)
          ExprTerm.Cluster([ ClusterStep.Structural "filename with spaces" ], None) ]
    Assert.Equal<ExprSeq>(expected, exprOk "x / \"filename with spaces\"")

[<Fact>]
let ``spaced quoted content name argument`` () =
    let expected: ExprSeq =
        [ ExprTerm.Word("x", None)
          ExprTerm.Cluster([ ClusterStep.Content "a b" ], None) ]
    Assert.Equal<ExprSeq>(expected, exprOk "x # \"a b\"")

// ---- spaced // ws stays parse error ----

[<Fact>]
let ``spaced ws after double slash is missing argument`` () =
    Assert.Contains("missing argument", exprErr "// ws")

// ---- standalone number and containing ----

[<Fact>]
let ``standalone number is parse error`` () =
    let err = exprErr "3"
    Assert.Contains(":", err)
    Assert.Contains("!", err)

[<Fact>]
let ``containing without string is missing argument`` () =
    Assert.Contains("missing argument", exprErr "containing")
