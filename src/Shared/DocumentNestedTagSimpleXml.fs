namespace Gambol.Shared

open System
open Fable.SimpleXml
open Fable.SimpleXml.Generator

/// Spike: nested-tag pack via Fable.SimpleXml. Not the DotNet pack.
[<RequireQualifiedAccess>]
module DocumentNestedTagSimpleXml =

    type NestedTagNode =
        { tag: string
          text: string
          children: NestedTagNode list }

    [<Literal>]
    let missingRoot = "extract root not found"

    [<Literal>]
    let incomplete = "incomplete extract"

    [<Literal>]
    let residue = "residue after extract"

    let tagName (_node: Node) = "div"

    let private writeTag (graph: Graph) (node: Node) =
        match graph.focus with
        | Some id when id = node.id -> "focus"
        | _ -> tagName node

    let rec private writeNode
        (graph: Graph)
        (ancestors: Set<NodeId>)
        (item: Node)
        : XNode =
        let nextAncestors = Set.add item.id ancestors
        let kids =
            item.children
            |> List.choose (fun child ->
                if Set.contains child.id ancestors then
                    None
                else
                    Map.tryFind child.id graph.nodes
                    |> Option.map (writeNode graph nextAncestors))
        node (writeTag graph item) [] (text item.text :: kids)

    let writeExtract (graph: Graph) : Result<string, string> =
        match Map.tryFind graph.root graph.nodes with
        | None -> Error missingRoot
        | Some root ->
            writeNode graph Set.empty root
            |> serializeXml
            |> Ok

    let rec private fromElement (el: XmlElement) : NestedTagNode =
        let text =
            match el.Content with
            | "" ->
                el.Children
                |> List.choose (fun c ->
                    if c.IsTextNode then Some c.Content else None)
                |> String.concat ""
            | content -> content
        let children =
            el.Children
            |> List.filter (fun c ->
                not c.IsTextNode && not c.IsComment)
            |> List.map fromElement
        { tag = el.Name
          text = text
          children = children }

    let private leftoverText (wrap: XmlElement) =
        wrap.Children
        |> List.exists (fun c ->
            c.IsTextNode && not (String.IsNullOrWhiteSpace c.Content))

    let private elementChildren (wrap: XmlElement) =
        wrap.Children
        |> List.filter (fun c ->
            not c.IsTextNode && not c.IsComment)

    let parseExtract (text: string) : Result<NestedTagNode, string> =
        match SimpleXml.tryParseElement ("<r>" + text + "</r>") with
        | None -> Error incomplete
        | Some wrap ->
            match elementChildren wrap, leftoverText wrap with
            | [ el ], false -> Ok(fromElement el)
            | _ :: _, _ -> Error residue
            | [], _ -> Error incomplete
