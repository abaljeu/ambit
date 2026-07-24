module Gambol.Shared.Tests.OutlineDocumentTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``nestFlatLines preserves order and pads shorter span and id inputs`` () =
    let nodeId = NodeId.New()
    let flats =
        [ 0, "first", None
          0, "second", None ]

    let tree =
        OutlineDocument.nestFlatLines "first" flats [ Some nodeId ]

    Assert.Equal<string list>(
        [ "first"; "second" ],
        tree.children |> List.map (fun node -> node.text))
    Assert.Equal(Some nodeId, tree.children.[0].nodeId)
    Assert.Equal(None, tree.children.[1].nodeId)
    Assert.Equal(
        { TextSpan.start = 0; TextSpan.end_ = 0 },
        tree.children.[1].span)

[<Fact>]
let ``nestFlatLines ignores surplus spans and node ids`` () =
    let firstId = NodeId.New()
    let surplusId = NodeId.New()
    let tree =
        OutlineDocument.nestFlatLines
            "first\nsurplus"
            [ 0, "first", None ]
            [ Some firstId; Some surplusId ]

    Assert.Single(tree.children) |> ignore
    Assert.Equal("first", tree.children.Head.text)
    Assert.Equal(Some firstId, tree.children.Head.nodeId)
    Assert.Equal(
        { TextSpan.start = 0; TextSpan.end_ = 5 },
        tree.children.Head.span)
