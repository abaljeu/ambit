module Gambol.Shared.Tests.OutlineReconcileTests

open Xunit
open Gambol.Shared

let private line depth text nodeId : OutlineReconcile.OutlineLine = {
    depth = depth
    text = text
    nodeId = nodeId
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
    let result = OutlineReconcile.align previous edited
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
    let result = OutlineReconcile.align previous edited
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
    let result = OutlineReconcile.align previous edited
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
    let result = OutlineReconcile.align previous edited
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
    let result = OutlineReconcile.align previous edited
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
    let result = OutlineReconcile.align previous edited
    match result with
    | [ OutlineReconcile.Keep(kb, 0, "beta")
        OutlineReconcile.Keep(ka, 0, "alpha") ] ->
        Assert.Equal(b, kb)
        Assert.Equal(a, ka)
    | other -> failwithf "unexpected: %A" other
