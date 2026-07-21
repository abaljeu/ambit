namespace Gambol.Shared

open System.Text

/// Outline warm helpers. Diff implementation is injected (DotNet: OutlineLcs).
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
        (diffTexts: OutlineDiffTexts)
        (previous: SpanNode)
        (edited: SpanNode)
        : OutlineReconcile.LineDisposition list =
        OutlineReconcile.align
            diffTexts
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
        (diffTexts: OutlineDiffTexts)
        (toSpanTree: string -> NodeId option list -> SpanNode)
        (previousText: string)
        (editedText: string)
        (previousNodeIds: NodeId option list)
        : (int * string * NodeId option) list =
        let prevTree = toSpanTree previousText previousNodeIds
        let editTree = toSpanTree editedText []
        warmByLcs diffTexts prevTree editTree |> alignedRows

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
        (diffTexts: OutlineDiffTexts)
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
                        diffTexts
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
        (diffTexts: OutlineDiffTexts)
        (toSpanTree: string -> NodeId option list -> SpanNode)
        (readCold: string -> Graph -> NodeId -> Result<DocumentNodesRead, string>)
        (hooks: OutlineWarmHooks)
        (write: Graph -> NodeId -> string option -> Result<string, string>)
        : DocumentHandler =
        {
            DocumentHandler.parse =
                fun text _graph _documentRootId -> Ok(toSpanTree text [])
            DocumentHandler.readCold = readCold
            DocumentHandler.readWarm =
                readWarmByLcs diffTexts toSpanTree readCold hooks
            DocumentHandler.write = write
        }

    type private WriteStep =
        | WKeep of prevIndex: int * edit: OutlineReconcile.OutlineLine
        | WInsert of edit: OutlineReconcile.OutlineLine
        | WDelete of prevIndex: int

    let private pairIdenticalMovesWrite
        (previous: OutlineReconcile.OutlineLine list)
        (edited: OutlineReconcile.OutlineLine list)
        (ops: OutlineDiffOp list)
        =
        let dels =
            ops
            |> List.choose (function
                | OutlineDiffOp.Delete pi -> Some(pi, previous.[pi])
                | _ -> None)

        let ins =
            ops
            |> List.choose (function
                | OutlineDiffOp.Insert ei -> Some(ei, edited.[ei])
                | _ -> None)

        let rec zipMin xs ys =
            match xs, ys with
            | x :: xs', y :: ys' -> (x, y) :: zipMin xs' ys'
            | _ -> []

        let paired =
            dels
            |> List.groupBy (fun (_, line) -> line.text)
            |> List.collect (fun (text, dlist) ->
                let ilist =
                    ins
                    |> List.filter (fun (_, line) -> line.text = text)

                zipMin dlist ilist)

        let pairedDel = paired |> List.map (fun ((pi, _), _) -> pi) |> Set.ofList

        let moveByEdit =
            paired
            |> List.map (fun ((pi, _), (ei, _)) -> ei, pi)
            |> Map.ofList

        ops
        |> List.choose (fun op ->
            match op with
            | OutlineDiffOp.Equal(pi, ei) -> Some(WKeep(pi, edited.[ei]))
            | OutlineDiffOp.Insert ei ->
                match Map.tryFind ei moveByEdit with
                | Some pi -> Some(WKeep(pi, edited.[ei]))
                | None -> Some(WInsert edited.[ei])
            | OutlineDiffOp.Delete pi ->
                if Set.contains pi pairedDel then None
                else Some(WDelete pi))

    let private pairInPlaceWrite (steps: WriteStep list) =
        let rec loop acc =
            function
            | [] -> List.rev acc
            | WDelete pi :: WInsert edit :: rest ->
                loop (WKeep(pi, edit) :: acc) rest
            | WDelete _ :: rest -> loop acc rest
            | x :: rest -> loop (x :: acc) rest

        loop [] steps

    let private writeStepsLcs
        (diffTexts: OutlineDiffTexts)
        (previous: OutlineReconcile.OutlineLine list)
        (edited: OutlineReconcile.OutlineLine list)
        =
        let ops =
            diffTexts
                (previous |> List.map (fun l -> l.text))
                (edited |> List.map (fun l -> l.text))

        ops
        |> pairIdenticalMovesWrite previous edited
        |> pairInPlaceWrite

    let private writeSteps
        (diffTexts: OutlineDiffTexts)
        (previous: OutlineReconcile.OutlineLine list)
        (edited: OutlineReconcile.OutlineLine list)
        =
        let pairs = OutlineReconcile.hardMatchPairs previous edited

        if pairs.IsEmpty then
            writeStepsLcs diffTexts previous edited
        else
            let hardPrev = pairs |> List.map fst |> Set.ofList
            let hardEdit = pairs |> List.map snd |> Set.ofList

            let hardByEdit =
                pairs
                |> List.map (fun (pi, ei) -> ei, WKeep(pi, edited.[ei]))
                |> Map.ofList

            let prevRest =
                previous
                |> List.mapi (fun i line -> i, line)
                |> List.filter (fun (i, _) -> not (Set.contains i hardPrev))
                |> List.map snd

            let editRest =
                edited
                |> List.mapi (fun i line -> i, line)
                |> List.filter (fun (i, _) -> not (Set.contains i hardEdit))
                |> List.map snd

            let restSteps = writeStepsLcs diffTexts prevRest editRest

            let prevRestIndex =
                previous
                |> List.mapi (fun i _ -> i)
                |> List.filter (fun i -> not (Set.contains i hardPrev))

            let remap = function
                | WKeep(localPi, edit) -> WKeep(prevRestIndex.[localPi], edit)
                | WInsert e -> WInsert e
                | WDelete localPi -> WDelete prevRestIndex.[localPi]

            let restKeepInsert =
                restSteps
                |> List.choose (function
                    | WDelete _ -> None
                    | s -> Some(remap s))

            let merged, _ =
                edited
                |> List.mapi (fun ei _ -> ei)
                |> List.fold
                    (fun (acc, restIdx) ei ->
                        match Map.tryFind ei hardByEdit with
                        | Some s -> s :: acc, restIdx
                        | None ->
                            restKeepInsert.[restIdx] :: acc, restIdx + 1)
                    ([], 0)

            List.rev merged

    /// Graph-wins emit plan: Delete omitted; Keep/Insert in graph order.
    type WriteEmit =
        | EmitKeep of prevIndex: int * edit: OutlineReconcile.OutlineLine
        | EmitInsert of edit: OutlineReconcile.OutlineLine

    let writePlan
        (diffTexts: OutlineDiffTexts)
        (previous: OutlineReconcile.OutlineLine list)
        (edited: OutlineReconcile.OutlineLine list)
        : WriteEmit list =
        writeSteps diffTexts previous edited
        |> List.choose (function
            | WKeep(pi, e) -> Some(EmitKeep(pi, e))
            | WInsert e -> Some(EmitInsert e)
            | WDelete _ -> None)

    /// Stateful fold over a write plan; `stepEmit` returns next state and chunk text.
    let executeWritePlan
        (plan: WriteEmit list)
        (stepEmit: 'State -> WriteEmit -> 'State * string)
        (initialState: 'State)
        : string =
        let sb = StringBuilder()

        plan
        |> List.fold
            (fun state step ->
                let state', chunk = stepEmit state step
                sb.Append chunk |> ignore
                state')
            initialState
        |> ignore

        sb.ToString()

    /// Graph-wins outline write: Delete omitted; Keep/Insert via format hooks.
    let writeByLcs
        (diffTexts: OutlineDiffTexts)
        (previous: OutlineReconcile.OutlineLine list)
        (edited: OutlineReconcile.OutlineLine list)
        (emitKeep: int -> OutlineReconcile.OutlineLine -> string)
        (emitInsert: OutlineReconcile.OutlineLine -> string)
        : string =
        let plan = writePlan diffTexts previous edited

        executeWritePlan
            plan
            (fun () step ->
                match step with
                | EmitKeep(pi, e) -> (), emitKeep pi e
                | EmitInsert e -> (), emitInsert e)
            ()
