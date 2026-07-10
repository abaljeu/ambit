module ViewModelFileSearchTests

open Gambol.Shared
open RefExprTestTree
open Xunit

let private tree = lazy build ()

let private fileIds (results: FileSearchResult list) =
    results |> List.map (fun r -> r.nodeId)

[<Fact>]
let ``searchFiles returns only file nodes`` () =
    let t = tree.Value
    let hits = ViewModelFileSearch.searchFiles ".fs" t.contentFile t.graph
    Assert.All(hits, fun r -> Assert.Equal(Special File, t.graph.nodes.[r.nodeId].kind))
    Assert.Equal(2, hits.Length)

[<Fact>]
let ``searchFiles prefers same directory before other workspace files`` () =
    let t = tree.Value
    let hits = ViewModelFileSearch.searchFiles "fs" t.contentFile t.graph |> fileIds
    Assert.Equal<NodeId>([ t.appFs; t.libFs ], hits)

[<Fact>]
let ``searchFiles ranks file directory before other workspace directories`` () =
    let t = tree.Value
    let hits = ViewModelFileSearch.searchFiles "." t.contentFile t.graph |> fileIds
    Assert.Equal<NodeId>([ t.appFs; t.libFs; t.embeddedMd; t.readmeMd ], hits)

[<Fact>]
let ``searchFiles path word matches RefExpr file hits`` () =
    let t = tree.Value
    let hits =
        ViewModelFileSearch.searchFiles "//bobby/src/*.fs" t.graph.root t.graph
        |> fileIds
    Assert.Equal<NodeId>([ t.appFs; t.libFs ], hits)

[<Fact>]
let ``searchFiles mixed words require same file node`` () =
    let t = tree.Value
    let hits =
        ViewModelFileSearch.searchFiles "readme //bobby/docs/readme.md" t.graph.root t.graph
        |> fileIds
    Assert.Equal<NodeId>([ t.readmeMd ], hits)

[<Fact>]
let ``searchFiles pathLabel uses desktop path syntax`` () =
    let t = tree.Value
    let hit =
        ViewModelFileSearch.searchFiles "app.fs" t.contentFile t.graph
        |> List.tryFind (fun r -> r.nodeId = t.appFs)
        |> Option.defaultWith (fun () -> failwith "missing app.fs hit")
    Assert.Equal("//bobby/src/app.fs", hit.pathLabel)
