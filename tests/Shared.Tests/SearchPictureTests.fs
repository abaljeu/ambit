module SearchPictureTests

open System
open Xunit
open Gambol.Shared

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private text key = SearchPicture.Text key

let private ids n =
    List.init n (fun _ -> NodeId.New())

[<Fact>]
let ``text change before the gap ends sends no Start`` () =
    let armed = SearchPicture.awaitGap 0 (text "cat")
    let replaced = SearchPicture.awaitGap 0 (text "catalog")
    let stale =
        SearchPicture.startAfterGap armed (text "catalog") 0
    let current =
        SearchPicture.startAfterGap replaced (text "catalog") 0
    Assert.Equal(None, stale)
    Assert.Equal(Some (text "catalog"), current)

[<Fact>]
let ``gap end with the same text sends one Start`` () =
    let gap = SearchPicture.awaitGap 3 (text "quarterly")
    let start =
        SearchPicture.startAfterGap gap (text "quarterly") 3
    Assert.Equal(Some (text "quarterly"), start)

[<Fact>]
let ``client already at 200 hits sends no Start`` () =
    let gap =
        SearchPicture.awaitGap ViewModelSearch.searchHitCap (text "e")
    Assert.Equal(SearchPicture.Idle, gap)
    let armed = SearchPicture.awaitGap 0 (text "e")
    let blocked =
        SearchPicture.startAfterGap
            armed
            (text "e")
            ViewModelSearch.searchHitCap
    Assert.Equal(None, blocked)

[<Fact>]
let ``blank search text sends no Start`` () =
    let gap = SearchPicture.awaitGap 0 (text "   ")
    Assert.Equal(None, SearchPicture.startAfterGap gap (text "   ") 0)

[<Fact>]
let ``stale reply is ignored and a matching reply is kept`` () =
    let hit = NodeId.New()
    let reply: SearchPicture.Reply =
        { ids = [ hit ]
          matchKey = text "quarterly" }
    let stale =
        SearchPicture.applyReply 0 (text "other") reply
    let kept =
        SearchPicture.keepReply 0 (text "other") [ hit ] reply
    let applied =
        SearchPicture.applyReply 0 (text "quarterly") reply
    Assert.Equal(None, stale)
    Assert.Equal<NodeId list>([ hit ], kept)
    Assert.Equal(Some [ hit ], applied)

[<Fact>]
let ``generation matches that generation only`` () =
    let hit = NodeId.New()
    let reply: SearchPicture.Reply =
        { ids = [ hit ]
          matchKey = SearchPicture.Generation 3 }
    Assert.Equal(
        Some [ hit ],
        SearchPicture.applyReply 0 (SearchPicture.Generation 3) reply)
    Assert.Equal(
        None,
        SearchPicture.applyReply 0 (SearchPicture.Generation 4) reply)
    Assert.Equal(None, SearchPicture.applyReply 0 (text "3") reply)

[<Fact>]
let ``client hits plus the reply stop at the shared cap`` () =
    let five = ids 5
    let reply: SearchPicture.Reply =
        { ids = five
          matchKey = text "e" }
    let room = ViewModelSearch.searchHitCap - 198
    let kept = SearchPicture.applyReply 198 (text "e") reply
    let full =
        SearchPicture.applyReply
            ViewModelSearch.searchHitCap
            (text "e")
            reply
    Assert.Equal(2, room)
    Assert.Equal(Some (List.take 2 five), kept)
    Assert.Equal(Some [], full)

[<Fact>]
let ``search request round-trips text and generation`` () =
    let startId = NodeId(Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"))
    let request: SearchPicture.Request =
        { text = "quarterly"
          startId = startId
          generation = Some 3 }
    let json = Enc.toString 0 (SearchPicture.encodeRequest request)
    match Dec.fromString SearchPicture.decodeRequest json with
    | Error err -> failwith err
    | Ok decoded ->
        Assert.Equal(request.text, decoded.text)
        Assert.Equal(request.startId, decoded.startId)
        Assert.Equal(request.generation, decoded.generation)
        Assert.Equal(
            SearchPicture.Generation 3,
            SearchPicture.matchKeyOf decoded)

[<Fact>]
let ``search reply round-trips ids and text`` () =
    let hit = NodeId(Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"))
    let reply: SearchPicture.Reply =
        { ids = [ hit ]
          matchKey = text "quarterly" }
    let json = Enc.toString 0 (SearchPicture.encodeReply reply)
    Assert.DoesNotContain("cursor", json)
    match Dec.fromString SearchPicture.decodeReply json with
    | Error err -> failwith err
    | Ok decoded -> Assert.Equal(reply, decoded)
