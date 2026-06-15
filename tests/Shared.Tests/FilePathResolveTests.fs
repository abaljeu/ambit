module FilePathResolveTests

open Gambol.Shared
open RefExprTestTree
open Xunit

let private tree = lazy build ()

let private requireSome opt =
    match opt with
    | Some v -> v
    | None -> failwith "expected Some"

[<Fact>]
let ``tryResolveConcreteTarget bare filename uses workspace root`` () =
    let t = tree.Value
    let target =
        FilePathResolve.tryResolveConcreteTarget t.contentFile t.graph "notes.md"
        |> requireSome
    Assert.Equal(t.workspaceRoot, target.parentId)
    Assert.Equal("notes.md", target.fileName)
    Assert.Empty(target.missingSegments)

[<Fact>]
let ``tryResolveConcreteTarget slash path matches at prefix`` () =
    let t = tree.Value
    let target =
        FilePathResolve.tryResolveConcreteTarget t.contentFile t.graph "/src/new.fs"
        |> requireSome
    Assert.Equal(t.bobbySrc, target.parentId)
    Assert.Equal("new.fs", target.fileName)
    Assert.Empty(target.missingSegments)

[<Fact>]
let ``tryResolveConcreteTarget at-colon slash path matches slash path`` () =
    let t = tree.Value
    let slash =
        FilePathResolve.tryResolveConcreteTarget t.contentFile t.graph "/readme.md"
        |> requireSome
    let atColon =
        FilePathResolve.tryResolveConcreteTarget t.contentFile t.graph "@:/readme.md"
        |> requireSome
    Assert.Equal(slash.parentId, atColon.parentId)
    Assert.Equal(slash.fileName, atColon.fileName)
    Assert.Equal<(SpecialKind * string) list>(slash.missingSegments, atColon.missingSegments)

[<Fact>]
let ``tryResolveConcreteTarget missing directory records segment`` () =
    let t = tree.Value
    let target =
        FilePathResolve.tryResolveConcreteTarget t.contentFile t.graph "@bobby:src/newdir/x.fs"
        |> requireSome
    Assert.Equal(t.bobbySrc, target.parentId)
    Assert.Equal("x.fs", target.fileName)
    Assert.Equal<(SpecialKind * string) list>(
        [ Directory, "newdir" ],
        target.missingSegments
    )

[<Fact>]
let ``tryResolveConcreteTarget missing workspace records workspace segment`` () =
    let t = tree.Value
    let target =
        FilePathResolve.tryResolveConcreteTarget t.contentFile t.graph "@newws:notes.md"
        |> requireSome
    Assert.Equal(Graph.workspacesId, target.parentId)
    Assert.Equal("notes.md", target.fileName)
    Assert.Equal<(SpecialKind * string) list>(
        [ Workspace, "newws" ],
        target.missingSegments
    )

[<Fact>]
let ``tryResolveConcreteTarget rejects wildcards in filename`` () =
    let t = tree.Value
    Assert.Equal(
        None,
        FilePathResolve.tryResolveConcreteTarget t.contentFile t.graph "*.fs"
    )

[<Fact>]
let ``tryResolveConcreteTarget rejects ambiguous parent path`` () =
    let t = tree.Value
    Assert.Equal(
        None,
        FilePathResolve.tryResolveConcreteTarget t.contentFile t.graph "@bobby:*/x.fs"
    )

[<Fact>]
let ``tryResolveConcreteTarget rejects when file already exists`` () =
    let t = tree.Value
    Assert.Equal(
        None,
        FilePathResolve.tryResolveConcreteTarget t.contentFile t.graph "@bobby:src/app.fs"
    )

[<Fact>]
let ``isNewEnabled mirrors concrete unresolved file paths`` () =
    let t = tree.Value
    Assert.True(
        FilePathResolve.isNewEnabled t.contentFile t.graph "@bobby:src/new.fs"
    )
    Assert.False(
        FilePathResolve.isNewEnabled t.contentFile t.graph "@bobby:src/app.fs"
    )
    Assert.False(
        FilePathResolve.isNewEnabled t.contentFile t.graph "*.fs"
    )
