module Gambol.Shared.Tests.OutlineReconcileTests

open Xunit
open Gambol.Shared

let private line depth text nodeId : OutlineReconcile.OutlineLine = {
    depth = depth
    text = text
    nodeId = nodeId
    hardKey = None
}

let private hardLine depth text nodeId hardKey : OutlineReconcile.OutlineLine = {
    depth = depth
    text = text
    nodeId = nodeId
    hardKey = Some hardKey
}

let private id () = NodeId.New()

[<Fact>]
let ``align mid insert keeps neighbor ids`` () =
    let a = id ()
    let b = id ()
    let previous = [ line 0 "alpha" (Some a); line 0 "beta" (Some b) ]
    let edited = [
        line 0 "alpha" None
        line 0 "blat" None
        line 0 "beta" None
    ]
    let result = OutlineReconcile.align OutlineLcs.diffTexts previous edited
    match result with
    | [ OutlineReconcile.Keep(ka, 0, "alpha")
        OutlineReconcile.Insert(0, "blat")
        OutlineReconcile.Keep(kb, 0, "beta") ] ->
        Assert.Equal(a, ka)
        Assert.Equal(b, kb)
    | other -> failwithf "unexpected: %A" other

[<Fact>]
let ``align block reindent keeps ids with new depths`` () =
    let a = id ()
    let b = id ()
    let previous = [ line 0 "parent" (Some a); line 0 "child" (Some b) ]
    let edited = [ line 0 "parent" None; line 1 "child" None ]
    let result = OutlineReconcile.align OutlineLcs.diffTexts previous edited
    match result with
    | [ OutlineReconcile.Keep(ka, 0, "parent")
        OutlineReconcile.Keep(kb, 1, "child") ] ->
        Assert.Equal(a, ka)
        Assert.Equal(b, kb)
    | other -> failwithf "unexpected: %A" other

[<Fact>]
let ``align blank run insert is positional`` () =
    let a = id ()
    let blank = id ()
    let b = id ()
    let previous = [
        line 0 "a" (Some a)
        line 0 "" (Some blank)
        line 0 "b" (Some b)
    ]
    let edited = [
        line 0 "a" None
        line 0 "" None
        line 0 "" None
        line 0 "b" None
    ]
    let result = OutlineReconcile.align OutlineLcs.diffTexts previous edited
    match result with
    | [ OutlineReconcile.Keep(ka, 0, "a")
        OutlineReconcile.Keep(kblank, 0, "")
        OutlineReconcile.Insert(0, "")
        OutlineReconcile.Keep(kb, 0, "b") ] ->
        Assert.Equal(a, ka)
        Assert.Equal(blank, kblank)
        Assert.Equal(b, kb)
    | other -> failwithf "unexpected: %A" other

[<Fact>]
let ``align in-place text edit keeps id`` () =
    let a = id ()
    let b = id ()
    let previous = [ line 0 "alpha" (Some a); line 0 "beta" (Some b) ]
    let edited = [ line 0 "alpha" None; line 0 "BETA" None ]
    let result = OutlineReconcile.align OutlineLcs.diffTexts previous edited
    match result with
    | [ OutlineReconcile.Keep(ka, 0, "alpha")
        OutlineReconcile.Keep(kb, 0, "BETA") ] ->
        Assert.Equal(a, ka)
        Assert.Equal(b, kb)
    | other -> failwithf "unexpected: %A" other

[<Fact>]
let ``align delete emits Delete`` () =
    let a = id ()
    let b = id ()
    let previous = [ line 0 "alpha" (Some a); line 0 "beta" (Some b) ]
    let edited = [ line 0 "alpha" None ]
    let result = OutlineReconcile.align OutlineLcs.diffTexts previous edited
    match result with
    | [ OutlineReconcile.Keep(ka, 0, "alpha"); OutlineReconcile.Delete d ] ->
        Assert.Equal(a, ka)
        Assert.Equal(b, d)
    | other -> failwithf "unexpected: %A" other

[<Fact>]
let ``align unique line swap keeps ids via move pairing`` () =
    let a = id ()
    let b = id ()
    let previous = [ line 0 "alpha" (Some a); line 0 "beta" (Some b) ]
    let edited = [ line 0 "beta" None; line 0 "alpha" None ]
    let result = OutlineReconcile.align OutlineLcs.diffTexts previous edited
    match result with
    | [ OutlineReconcile.Keep(kb, 0, "beta")
        OutlineReconcile.Keep(ka, 0, "alpha") ] ->
        Assert.Equal(b, kb)
        Assert.Equal(a, ka)
    | other -> failwithf "unexpected: %A" other

[<Fact>]
let ``align hard-match keeps id across text edit`` () =
    let a = id ()
    let b = id ()
    let previous = [
        hardLine 0 "^A old" (Some a) "^A"
        line 0 "plain" (Some b)
    ]
    let edited = [
        hardLine 0 "^A NEW" None "^A"
        line 0 "plain" None
    ]
    let result = OutlineReconcile.align OutlineLcs.diffTexts previous edited
    match result with
    | [ OutlineReconcile.Keep(ka, 0, "^A NEW")
        OutlineReconcile.Keep(kb, 0, "plain") ] ->
        Assert.Equal(a, ka)
        Assert.Equal(b, kb)
    | other -> failwithf "unexpected: %A" other

[<Fact>]
let ``align hard-match keeps id across reindent`` () =
    let a = id ()
    let previous = [ hardLine 0 "^A body" (Some a) "^A" ]
    let edited = [ hardLine 1 "^A body" None "^A" ]
    let result = OutlineReconcile.align OutlineLcs.diffTexts previous edited
    match result with
    | [ OutlineReconcile.Keep(ka, 1, "^A body") ] -> Assert.Equal(a, ka)
    | other -> failwithf "unexpected: %A" other

[<Fact>]
let ``align hard-match keeps ids across reorder`` () =
    let a = id ()
    let b = id ()
    let previous = [
        hardLine 0 "^A x" (Some a) "^A"
        hardLine 0 "^B y" (Some b) "^B"
    ]
    let edited = [
        hardLine 0 "^B y" None "^B"
        hardLine 0 "^A x" None "^A"
    ]
    let result = OutlineReconcile.align OutlineLcs.diffTexts previous edited
    match result with
    | [ OutlineReconcile.Keep(kb, 0, "^B y")
        OutlineReconcile.Keep(ka, 0, "^A x") ] ->
        Assert.Equal(b, kb)
        Assert.Equal(a, ka)
    | other -> failwithf "unexpected: %A" other

[<Fact>]
let ``align duplicate hard keys fall through to LCS`` () =
    let a = id ()
    let b = id ()
    let previous = [
        hardLine 0 "^A one" (Some a) "^A"
        hardLine 0 "^A two" (Some b) "^A"
    ]
    let edited = [
        hardLine 0 "^A one" None "^A"
        hardLine 0 "^A two" None "^A"
    ]
    let result = OutlineReconcile.align OutlineLcs.diffTexts previous edited
    match result with
    | [ OutlineReconcile.Keep(ka, 0, "^A one")
        OutlineReconcile.Keep(kb, 0, "^A two") ] ->
        Assert.Equal(a, ka)
        Assert.Equal(b, kb)
    | other -> failwithf "unexpected: %A" other
