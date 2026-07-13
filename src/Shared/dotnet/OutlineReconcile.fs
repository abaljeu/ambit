namespace Gambol.Shared

[<RequireQualifiedAccess>]
module OutlineReconcile =

    type OutlineLine = {
        depth: int
        text: string
        nodeId: NodeId option
    }

    type LineDisposition =
        | Keep of nodeId: NodeId * newDepth: int * newText: string
        | Insert of depth: int * text: string
        | Delete of nodeId: NodeId

    let private keepOrInsert (prev: OutlineLine) (ed: OutlineLine) =
        match prev.nodeId with
        | Some id -> Keep(id, ed.depth, ed.text)
        | None -> Insert(ed.depth, ed.text)

    let private deleteOf (prev: OutlineLine) =
        match prev.nodeId with
        | Some id -> Some(Delete id)
        | None -> None

    /// Pair identical delete/insert keys in order (moves / blank runs).
    let private pairIdenticalMoves
        (previous: OutlineLine list)
        (edited: OutlineLine list)
        (ops: OutlineDiffOp list)
        : LineDisposition list =
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
            |> List.map (fun ((_, prev), (ei, ed)) -> ei, keepOrInsert prev ed)
            |> Map.ofList

        ops
        |> List.choose (fun op ->
            match op with
            | OutlineDiffOp.Equal(pi, ei) ->
                Some(keepOrInsert previous.[pi] edited.[ei])
            | OutlineDiffOp.Insert ei ->
                match Map.tryFind ei moveByEdit with
                | Some d -> Some d
                | None ->
                    let ed = edited.[ei]
                    Some(Insert(ed.depth, ed.text))
            | OutlineDiffOp.Delete pi ->
                if Set.contains pi pairedDel then None
                else deleteOf previous.[pi])

    /// Adjacent delete+insert → in-place Keep (text edit).
    let private pairInPlaceEdits (disps: LineDisposition list) =
        let rec loop acc =
            function
            | [] -> List.rev acc
            | Delete id :: Insert(depth, text) :: rest ->
                loop (Keep(id, depth, text) :: acc) rest
            | x :: rest -> loop (x :: acc) rest

        loop [] disps

    let align
        (previous: OutlineLine list)
        (edited: OutlineLine list)
        : LineDisposition list =
        let ops =
            OutlineLcs.diffTexts
                (previous |> List.map (fun l -> l.text))
                (edited |> List.map (fun l -> l.text))

        ops
        |> pairIdenticalMoves previous edited
        |> pairInPlaceEdits
