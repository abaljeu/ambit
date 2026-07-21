namespace Gambol.Shared

/// Shared outline helpers: nest flat lines into SpanNode trees; LCS over bound lines.
[<RequireQualifiedAccess>]
module OutlineDocument =

    [<RequireQualifiedAccess>]
    type FlatOutlineLine = {
        depth: int
        span: TextSpan
        text: string
        hardKey: string option
        nodeId: NodeId option
    }

    let flatLine
        depth
        (span: TextSpan)
        text
        hardKey
        nodeId
        : FlatOutlineLine =
        {
            FlatOutlineLine.depth = depth
            span = span
            text = text
            hardKey = hardKey
            nodeId = nodeId
        }

    type private Frame = {
        depth: int
        node: SpanNode
        childrenRev: SpanNode list
    }

    let private closeFrame (f: Frame) : SpanNode =
        let kids = List.rev f.childrenRev
        let end_ =
            match kids with
            | [] -> f.node.span.end_
            | _ ->
                max f.node.span.end_ (List.last kids).span.end_

        {
            SpanNode.span = { f.node.span with TextSpan.end_ = end_ }
            text = f.node.text
            hardKey = f.node.hardKey
            nodeId = f.node.nodeId
            children = kids
        }

    /// Nest depth-ordered lines under a synthetic root (parent encloses descendants).
    let nestByDepth (rootSpan: TextSpan) (lines: FlatOutlineLine list) : SpanNode =
        let toNode (line: FlatOutlineLine) : SpanNode = {
            SpanNode.span = line.span
            text = line.text
            hardKey = line.hardKey
            nodeId = line.nodeId
            children = []
        }

        let rec closeTo depth (stack: Frame list) (topsRev: SpanNode list) =
            match stack with
            | f :: rest when f.depth >= depth ->
                let closed = closeFrame f

                match rest with
                | parent :: prest ->
                    let parent' = {
                        parent with
                            childrenRev = closed :: parent.childrenRev
                    }

                    closeTo depth (parent' :: prest) topsRev
                | [] -> closeTo depth [] (closed :: topsRev)
            | _ -> stack, topsRev

        let foldLine (stack, topsRev) (line: FlatOutlineLine) =
            let stack', tops' = closeTo line.depth stack topsRev
            let frame = {
                depth = line.depth
                node = toNode line
                childrenRev = []
            }

            frame :: stack', tops'

        let stack, topsRev = List.fold foldLine ([], []) lines

        let rec flush (st: Frame list) (tops: SpanNode list) =
            match st with
            | [] -> List.rev tops
            | f :: rest ->
                let closed = closeFrame f

                match rest with
                | parent :: prest ->
                    let parent' = {
                        parent with
                            childrenRev = closed :: parent.childrenRev
                    }

                    flush (parent' :: prest) tops
                | [] -> flush [] (closed :: tops)

        let children = flush stack topsRev
        let end_ =
            match children with
            | [] -> rootSpan.end_
            | _ -> max rootSpan.end_ (List.last children).span.end_

        {
            SpanNode.span = { rootSpan with TextSpan.end_ = end_ }
            text = ""
            hardKey = None
            nodeId = None
            children = children
        }

    /// Offsets for lines of a `\n`-normalized artifact.
    let lineSpans (normalized: string) : (TextSpan * string) list =
        if normalized.Length = 0 then
            []
        else
            let lines = normalized.Split('\n')
            let _, acc =
                lines
                |> Array.fold
                    (fun (offset, acc) line ->
                        let span: TextSpan = {
                            TextSpan.start = offset
                            end_ = offset + line.Length
                        }

                        let next =
                            if offset + line.Length < normalized.Length then
                                offset + line.Length + 1
                            else
                                offset + line.Length

                        next, (span, line) :: acc)
                    (0, [])

            List.rev acc

    let private emptySpan: TextSpan = {
        TextSpan.start = 0
        end_ = 0
    }

    /// Nest flat (depth, text, hardKey) rows into a SpanNode tree using artifact line spans.
    let nestFlatLines
        (artifactForSpans: string)
        (flats: (int * string * string option) list)
        (nodeIds: NodeId option list)
        : SpanNode =
        let spanned = lineSpans artifactForSpans

        let lines =
            flats
            |> List.mapi (fun i (depth, content, hardKey) ->
                let span =
                    match List.tryItem i spanned with
                    | Some(s, _) -> s
                    | None -> emptySpan

                let nodeId =
                    match List.tryItem i nodeIds with
                    | Some id -> id
                    | None -> None

                flatLine depth span content hardKey nodeId)

        let rootSpan: TextSpan = {
            TextSpan.start = 0
            end_ = artifactForSpans.Length
        }

        nestByDepth rootSpan lines

    /// Tab-indented artifact from outline rows, then nestFlatLines.
    let nestOutlineRows
        (flats: (int * string * string option) list)
        (nodeIds: NodeId option list)
        : SpanNode =
        let artifact =
            flats
            |> List.map (fun (depth, body, _) ->
                String.replicate depth "\t" + body)
            |> String.concat "\n"

        nestFlatLines artifact flats nodeIds

    let nodesRead
        (documentRootId: NodeId)
        (nodes: Map<NodeId, Node>)
        : DocumentNodesRead =
        {
            DocumentNodesRead.documentRootId = documentRootId
            nodes = nodes
        }

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
            readCold = readCold
            readWarm = readWarmByLcs toSpanTree readCold hooks
            write = write
        }
