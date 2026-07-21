namespace Gambol.Shared

/// LCS-style ops over text keys. DiffPlex lives in DotNet OutlineLcs.
type OutlineDiffOp =
    | Equal of prevIndex: int * editedIndex: int
    | Insert of editedIndex: int
    | Delete of prevIndex: int

type OutlineDiffTexts = string list -> string list -> OutlineDiffOp list

[<RequireQualifiedAccess>]
module OutlineReconcile =

    type OutlineLine = {
        depth: int
        text: string
        nodeId: NodeId option
        /// Durable format key (e.g. Amb `^id` / `->^id`). None → LCS only.
        hardKey: string option
    }

    type LineDisposition =
        | Keep of nodeId: NodeId * newDepth: int * newText: string
        | Insert of depth: int * text: string
        | Delete of nodeId: NodeId

    /// NodeId string for warm-write hard matching.
    let outlineHardKey (nodeId: NodeId option) =
        nodeId |> Option.map (fun id -> id.Value.ToString())

    let writeLine depth text nodeId : OutlineLine = {
        depth = depth
        text = text
        nodeId = nodeId
        hardKey = outlineHardKey nodeId
    }

    /// Assign hard keys to previous lines when text uniquely identifies a graph node.
    let assignPrevHardKeys
        (graphLines: OutlineLine list)
        (prevLines: OutlineLine list)
        : OutlineLine list =
        let uniqueText =
            graphLines
            |> List.choose (fun l ->
                l.nodeId |> Option.map (fun id -> l.text, id))
            |> List.groupBy fst
            |> List.choose (function
                | text, [ (_, id) ] -> Some(text, id)
                | _ -> None)
            |> Map.ofList

        prevLines
        |> List.map (fun line ->
            match Map.tryFind line.text uniqueText with
            | Some id -> writeLine line.depth line.text (Some id)
            | None -> line)

    let private keepOrInsert (prev: OutlineLine) (ed: OutlineLine) =
        match prev.nodeId with
        | Some id -> Keep(id, ed.depth, ed.text)
        | None -> Insert(ed.depth, ed.text)

    let private deleteOf (prev: OutlineLine) =
        match prev.nodeId with
        | Some id -> Some(Delete id)
        | None -> None

    /// Unique hardKey → index; duplicates are excluded (fall through to LCS).
    let uniqueHardIndex (lines: OutlineLine list) =
        lines
        |> List.mapi (fun i line -> line.hardKey, i)
        |> List.choose (fun (key, i) -> key |> Option.map (fun k -> k, i))
        |> List.groupBy fst
        |> List.choose (function
            | key, [ (_, i) ] -> Some(key, i)
            | _ -> None)
        |> Map.ofList

    let hardMatchPairs
        (previous: OutlineLine list)
        (edited: OutlineLine list)
        : (int * int) list =
        let prevByKey = uniqueHardIndex previous
        let editByKey = uniqueHardIndex edited

        prevByKey
        |> Map.toList
        |> List.choose (fun (key, pi) ->
            match Map.tryFind key editByKey with
            | Some ei -> Some(pi, ei)
            | None -> None)

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

    let private alignLcs
        (diffTexts: OutlineDiffTexts)
        (previous: OutlineLine list)
        (edited: OutlineLine list)
        : LineDisposition list =
        let ops =
            diffTexts
                (previous |> List.map (fun l -> l.text))
                (edited |> List.map (fun l -> l.text))

        ops
        |> pairIdenticalMoves previous edited
        |> pairInPlaceEdits

    /// Hard-match unique keys first; LCS on the remainder (Plain: all hardKey=None).
    let align
        (diffTexts: OutlineDiffTexts)
        (previous: OutlineLine list)
        (edited: OutlineLine list)
        : LineDisposition list =
        let pairs = hardMatchPairs previous edited

        if pairs.IsEmpty then
            alignLcs diffTexts previous edited
        else
            let hardPrev = pairs |> List.map fst |> Set.ofList
            let hardEdit = pairs |> List.map snd |> Set.ofList

            let hardByEdit =
                pairs
                |> List.map (fun (pi, ei) ->
                    ei, keepOrInsert previous.[pi] edited.[ei])
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

            let restDisps = alignLcs diffTexts prevRest editRest

            let restKeepInsert =
                restDisps
                |> List.choose (function
                    | Delete _ -> None
                    | d -> Some d)

            let restDeletes =
                restDisps
                |> List.choose (function
                    | Delete id -> Some(Delete id)
                    | _ -> None)

            let keepInsert, _ =
                edited
                |> List.mapi (fun ei _ -> ei)
                |> List.fold
                    (fun (acc, restIdx) ei ->
                        match Map.tryFind ei hardByEdit with
                        | Some d -> d :: acc, restIdx
                        | None ->
                            restKeepInsert.[restIdx] :: acc, restIdx + 1)
                    ([], 0)

            List.rev keepInsert @ restDeletes
