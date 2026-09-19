namespace Gambol.Shared

open System
open System.Xml
open System.Xml.Linq

/// Nested-tag extract pack via System.Xml.Linq. Not Fable; not a file codec.
[<RequireQualifiedAccess>]
module DocumentNestedTag =

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

    let rec private writeElement
        (graph: Graph)
        (ancestors: Set<NodeId>)
        (node: Node)
        : XElement =
        let nextAncestors = Set.add node.id ancestors
        let kids =
            node.children
            |> List.choose (fun child ->
                if Set.contains child.id ancestors then
                    None
                else
                    Map.tryFind child.id graph.nodes
                    |> Option.map (writeElement graph nextAncestors))
        let content : obj[] =
            Array.append
                [| box node.text |]
                (kids |> List.map box |> List.toArray)
        XElement(XName.Get(writeTag graph node), content)

    let writeExtract (graph: Graph) : Result<string, string> =
        match Map.tryFind graph.root graph.nodes with
        | None -> Error missingRoot
        | Some root ->
            let el = writeElement graph Set.empty root
            Ok(el.ToString(SaveOptions.DisableFormatting))

    let rec private fromElement (el: XElement) : NestedTagNode =
        let text =
            el.Nodes()
            |> Seq.choose (function
                | :? XText as t -> Some t.Value
                | _ -> None)
            |> String.concat ""
        let children =
            el.Elements() |> Seq.map fromElement |> List.ofSeq
        { tag = el.Name.LocalName
          text = text
          children = children }

    let private leftoverText (wrap: XElement) =
        wrap.Nodes()
        |> Seq.exists (function
            | :? XText as t -> not (String.IsNullOrWhiteSpace t.Value)
            | _ -> false)

    let parseExtract (text: string) : Result<NestedTagNode, string> =
        try
            let wrap = XElement.Parse("<r>" + text + "</r>")
            match List.ofSeq (wrap.Elements()), leftoverText wrap with
            | [ el ], false -> Ok(fromElement el)
            | _ :: _, _ -> Error residue
            | [], _ -> Error incomplete
        with
        | :? XmlException -> Error incomplete
