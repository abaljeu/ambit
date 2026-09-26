namespace Gambol.Server

open System
open System.Xml.Linq
open Gambol.Shared

/// Write-only XML pack of a Zoom-rooted extract for CloudAgents.
[<RequireQualifiedAccess>]
module AiExtractPack =

    [<Literal>]
    let PromptClass = "prompt"

    let private nodeName = XName.Get "node"
    let private className = XName.Get "class"

    let private withPromptClass (classes: CssClasses) =
        if CssClass.contains PromptClass classes then
            classes
        else
            CssClass.toggle PromptClass classes

    let markFocus (graph: Graph) : Graph =
        match graph.focus with
        | None -> graph
        | Some focusId ->
            match Map.tryFind focusId graph.nodes with
            | None -> graph
            | Some node ->
                let marked =
                    { node with
                        cssClasses = withPromptClass node.cssClasses }
                { graph with
                    nodes = graph.nodes |> Map.add focusId marked }

    let private classAttribute (node: Node) =
        match CssClass.toList node.cssClasses with
        | [] -> None
        | names ->
            Some (XAttribute(className, String.concat " " names))

    let rec private writePresent
        (graph: Graph)
        (path: Set<NodeId>)
        (nodeId: NodeId)
        : XElement option =
        match Map.tryFind nodeId graph.nodes with
        | None -> None
        | Some node ->
            let kids =
                if Set.contains nodeId path then
                    []
                else
                    let path' = Set.add nodeId path
                    Graph.children graph nodeId
                    |> List.choose (fun child ->
                        writePresent graph path' child.id)
            let parts =
                [ match classAttribute node with
                  | Some attr -> yield box attr
                  | None -> ()
                  if not (String.IsNullOrEmpty node.text) then
                      yield box node.text
                  for kid in kids do
                      yield box kid ]
            Some (XElement(nodeName, Array.ofList parts))

    let write (graph: Graph) (zoomId: NodeId) : Result<string, string> =
        match writePresent graph Set.empty zoomId with
        | None -> Error "document root not found"
        | Some root -> Ok (root.ToString(SaveOptions.DisableFormatting))

    let packExtract
        (graph: Graph)
        (zoomId: NodeId)
        (focusId: NodeId)
        : Result<string, string> =
        graph
        |> Graph.withFocus (Some focusId)
        |> markFocus
        |> fun extract -> write extract zoomId
