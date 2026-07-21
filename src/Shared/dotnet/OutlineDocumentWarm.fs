namespace Gambol.Shared

/// DotNet-only outline warm helpers (DiffPlex LCS). Cold nest helpers stay in Documents.
[<RequireQualifiedAccess>]
module OutlineDocumentWarm =

    /// Preorder bound/content lines with outline depth (Amb/Plain: every line is bound).
    let flattenBoundLines (root: SpanNode) : OutlineReconcile.OutlineLine list =
        let rec walk depth (n: SpanNode) =
            let line: OutlineReconcile.OutlineLine = {
                depth = depth
                text = n.text
                nodeId = n.nodeId
                hardKey = n.hardKey
            }

            line :: List.collect (walk (depth + 1)) n.children

        List.collect (walk 0) root.children

    /// Outline LCS dispositions over preorder bound lines.
    let warmByLcs
        (previous: SpanNode)
        (edited: SpanNode)
        : OutlineReconcile.LineDisposition list =
        OutlineReconcile.align
            (flattenBoundLines previous)
            (flattenBoundLines edited)

    let alignedRows
        (disps: OutlineReconcile.LineDisposition list)
        : (int * string * NodeId option) list =
        disps
        |> List.choose (function
            | OutlineReconcile.Keep(id, depth, text) ->
                Some(depth, text, Some id)
            | OutlineReconcile.Insert(depth, text) ->
                Some(depth, text, None)
            | OutlineReconcile.Delete _ -> None)

    /// Build prev/edit trees and map LCS dispositions to aligned rows.
    let alignWarmEdit
        (toSpanTree: string -> NodeId option list -> SpanNode)
        (previousText: string)
        (editedText: string)
        (previousNodeIds: NodeId option list)
        : (int * string * NodeId option) list =
        let prevTree = toSpanTree previousText previousNodeIds
        let editTree = toSpanTree editedText []
        warmByLcs prevTree editTree |> alignedRows

    /// Format-specific hooks for outline warm import.
    /// whenUnchanged None → readCold (Amb); Some → format-specific (Plain).
    [<RequireQualifiedAccess>]
    type OutlineWarmHooks = {
        previousNodeIds:
            string -> Graph -> NodeId -> Result<NodeId option list, string>
        whenUnchanged:
            (string -> Graph -> NodeId -> Result<DocumentNodesRead, string>) option
        fromAligned:
            string
                -> Graph
                -> NodeId
                -> (int * string * NodeId option) list
                -> Result<DocumentNodesRead, string>
    }

    let readWarmByLcs
        (toSpanTree: string -> NodeId option list -> SpanNode)
        (readCold: string -> Graph -> NodeId -> Result<DocumentNodesRead, string>)
        (hooks: OutlineWarmHooks)
        (editedText: string)
        (contextGraph: Graph)
        (documentRootId: NodeId)
        (previousText: string)
        : Result<DocumentNodesRead, string> =
        if editedText = previousText then
            match hooks.whenUnchanged with
            | Some f -> f previousText contextGraph documentRootId
            | None -> readCold previousText contextGraph documentRootId
        else
            hooks.previousNodeIds previousText contextGraph documentRootId
            |> Result.bind (fun prevIds ->
                let aligned =
                    alignWarmEdit
                        toSpanTree
                        previousText
                        editedText
                        prevIds

                hooks.fromAligned
                    editedText
                    contextGraph
                    documentRootId
                    aligned)

    let makeOutlineHandler
        (toSpanTree: string -> NodeId option list -> SpanNode)
        (readCold: string -> Graph -> NodeId -> Result<DocumentNodesRead, string>)
        (hooks: OutlineWarmHooks)
        (write: Graph -> NodeId -> string option -> Result<string, string>)
        : DocumentHandler =
        {
            DocumentHandler.parse =
                fun text _graph _documentRootId -> Ok(toSpanTree text [])
            DocumentHandler.readCold = readCold
            DocumentHandler.readWarm = readWarmByLcs toSpanTree readCold hooks
            DocumentHandler.write = write
        }
