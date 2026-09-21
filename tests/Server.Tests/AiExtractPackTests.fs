module Gambol.Server.Tests.AiExtractPackTests

open System
open System.Xml.Linq
open Xunit
open Gambol.Shared
open Gambol.Server

let private owned = ChildNode.owners

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private parseXml text = XElement.Parse text

let private classList (el: XElement) =
    match el.Attribute(XName.Get "class") with
    | null -> []
    | attr ->
        attr.Value.Split(
            [| ' ' |],
            StringSplitOptions.RemoveEmptyEntries)
        |> Array.toList

let private ownText (el: XElement) =
    el.Nodes()
    |> Seq.choose (function
        | :? XText as t -> Some t.Value
        | _ -> None)
    |> String.concat ""

let private findByOwnText (root: XElement) (text: string) =
    seq {
        yield root
        yield! root.Descendants()
    }
    |> Seq.tryFind (fun el -> ownText el = text)

let private zoomWithChild childText =
    let zoomId = NodeId.New()
    let childId = NodeId.New()
    let child = Node.Create(childId, text = childText)
    let zoom =
        Node.Create(
            zoomId,
            text = "zoom",
            children = owned [ childId ])
    let graph =
        Graph.fromExtracted
            zoomId
            (Map.ofList [ zoomId, zoom; childId, child ])
    graph, zoomId, childId

[<Fact>]
let ``pack string is XML with node elements`` () =
    let graph, zoomId, childId = zoomWithChild "visible"
    let text =
        AiExtractPack.packExtract graph zoomId childId
        |> requireOk "pack"
    let root = parseXml text
    Assert.Equal("node", root.Name.LocalName)
    Assert.Equal("zoom", ownText root)
    Assert.Contains("visible", text)

[<Fact>]
let ``Focus node element carries prompt class`` () =
    let graph, zoomId, childId = zoomWithChild "visible"
    let text =
        AiExtractPack.packExtract graph zoomId childId
        |> requireOk "pack"
    let root = parseXml text
    let focusEl = findByOwnText root "visible"
    Assert.True(focusEl.IsSome)
    Assert.Contains(AiExtractPack.PromptClass, classList focusEl.Value)
    Assert.DoesNotContain(AiExtractPack.PromptClass, classList root)

[<Fact>]
let ``Zoom Focus marks the Zoom root element`` () =
    let graph, zoomId, _ = zoomWithChild "visible"
    let text =
        AiExtractPack.packExtract graph zoomId zoomId
        |> requireOk "pack"
    let root = parseXml text
    Assert.Contains(AiExtractPack.PromptClass, classList root)

[<Fact>]
let ``existing cssClasses stay on the Focus copy`` () =
    let zoomId = NodeId.New()
    let focusId = NodeId.New()
    let prior = CssClass.ofList [ "h1" ]
    let focus =
        Node.Create(
            focusId,
            text = "prompt",
            cssClasses = prior,
            children = [])
    let zoom =
        Node.Create(
            zoomId,
            text = "zoom",
            children = owned [ focusId ])
    let graph =
        Graph.fromExtracted
            zoomId
            (Map.ofList [ zoomId, zoom; focusId, focus ])
    let text =
        AiExtractPack.packExtract graph zoomId focusId
        |> requireOk "pack"
    let focusEl = findByOwnText (parseXml text) "prompt"
    Assert.True(focusEl.IsSome)
    Assert.Contains("h1", classList focusEl.Value)
    Assert.Contains(AiExtractPack.PromptClass, classList focusEl.Value)
    Assert.Equal(prior, graph.nodes.[focusId].cssClasses)
    Assert.False(
        CssClass.contains AiExtractPack.PromptClass
            graph.nodes.[focusId].cssClasses)

[<Fact>]
let ``original graph node cssClasses stay unchanged`` () =
    let graph, zoomId, childId = zoomWithChild "visible"
    let before = graph.nodes.[childId].cssClasses
    let marked =
        graph
        |> Graph.withFocus (Some childId)
        |> AiExtractPack.markFocus
    AiExtractPack.packExtract graph zoomId childId
    |> requireOk "pack"
    |> ignore
    Assert.Equal(before, graph.nodes.[childId].cssClasses)
    Assert.False(
        CssClass.contains AiExtractPack.PromptClass
            graph.nodes.[childId].cssClasses)
    Assert.True(
        CssClass.contains AiExtractPack.PromptClass
            marked.nodes.[childId].cssClasses)

[<Fact>]
let ``node text is XML-escaped`` () =
    let graph, zoomId, childId = zoomWithChild "a <b> & c"
    let text =
        AiExtractPack.packExtract graph zoomId childId
        |> requireOk "pack"
    Assert.Contains("&lt;b&gt;", text)
    Assert.Contains("&amp;", text)
    let focusEl = findByOwnText (parseXml text) "a <b> & c"
    Assert.True(focusEl.IsSome)

[<Fact>]
let ``walk recurses present Ref and omits missing ids`` () =
    let zoomId = NodeId.New()
    let holderId = NodeId.New()
    let sharedId = NodeId.New()
    let leafId = NodeId.New()
    let missingId = NodeId.New()
    let leaf = Node.Create(leafId, text = "ref-leaf")
    let shared =
        Node.Create(
            sharedId,
            text = "shared-body",
            children = owned [ leafId ])
    let holder =
        Node.Create(
            holderId,
            text = "holder",
            children =
                [ ChildNode.reference sharedId
                  ChildNode.owner missingId ])
    let zoom =
        Node.Create(
            zoomId,
            text = "zoom",
            children = owned [ holderId ])
    let graph =
        Graph.fromExtracted
            zoomId
            (Map.ofList
                [ zoomId, zoom
                  holderId, holder
                  sharedId, shared
                  leafId, leaf ])
    let text =
        AiExtractPack.packExtract graph zoomId holderId
        |> requireOk "pack"
    Assert.Contains("shared-body", text)
    Assert.Contains("ref-leaf", text)
    Assert.DoesNotContain(missingId.Value.ToString(), text)

[<Fact>]
let ``pack does not persist a file path`` () =
    let graph, zoomId, childId = zoomWithChild "visible"
    let text =
        AiExtractPack.packExtract graph zoomId childId
        |> requireOk "pack"
    match DocumentPartition.artifactFileRelative graph zoomId with
    | None -> ()
    | Some path -> Assert.DoesNotContain(path, text)
    Assert.DoesNotContain("[Focus]", text)
