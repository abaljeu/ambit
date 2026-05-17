namespace Gambol.Shared

open System
open System.Text

type DesktopExportRequest =
    { path: string
      content: string }

type DesktopExportResponse =
    { path: string }

[<RequireQualifiedAccess>]
module ExportText =
    let private ownedChildIds (node: Node) : NodeId list =
        node.children
        |> List.choose (fun child ->
            match child.ref with
            | Ownership.Owner -> Some child.id
            | Ownership.Ref -> None)

    let private hasExportContent (entries: (string * int) list) : bool =
        entries
        |> List.exists (fun (text, _) -> text.Trim().Length > 0)

    /// Owned children of focusId only; tab-indented; Environment.NewLine between lines.
    let serializeOwnedChildren (graph: Graph) (focusId: NodeId) : string =
        let sb = StringBuilder()
        let nl = Environment.NewLine

        let rec walk (depth: int) (nodeId: NodeId) =
            let node = graph.nodes.[nodeId]
            sb.Append(String.replicate depth "\t").Append(node.text).Append(nl) |> ignore
            ownedChildIds node |> List.iter (fun childId -> walk (depth + 1) childId)

        match Map.tryFind focusId graph.nodes with
        | None -> ()
        | Some focus -> ownedChildIds focus |> List.iter (walk 0)

        let text = sb.ToString()

        if text.EndsWith(nl, StringComparison.Ordinal) then
            text.Substring(0, text.Length - nl.Length)
        else
            text

    let validateExportContent (text: string) : Result<unit, string> =
        let entries = Paste.parsePasteText text

        if hasExportContent entries then
            Ok ()
        else
            Error "export text is empty"

    let trySerializeOwnedChildren (graph: Graph) (focusId: NodeId) : Result<string, string> =
        let text = serializeOwnedChildren graph focusId
        validateExportContent text |> Result.map (fun () -> text)
