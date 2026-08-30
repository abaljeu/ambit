namespace Gambol.Shared

open System

/// Pass 1: three-terminal brace grammar. Pass 2: newline explode + brace attach.
[<RequireQualifiedAccess>]
module CStyleBrace =

    type Statement = {
        otherText: string
        otherStart: int
        openIndex: int option
        closeIndex: int option
        block: Statement list option
    }

    type OutlineRow = {
        depth: int
        text: string
        braced: bool
    }

    /// Persistence-only slices for warm Keep (not used by brace attach).
    type WarmUnit = {
        depth: int
        text: string
        braced: bool
        openRaw: string
        closeRaw: string
    }

    let private readOther (text: string) (i: int) (stop: int) =
        let rec loop j =
            if j >= stop then j
            elif text.[j] = '{' || text.[j] = '}' then j
            else loop (j + 1)

        let j = loop i
        text.Substring(i, j - i), j

    let rec private parseStatements (text: string) (i: int) (stop: int) =
        let rec loop j acc =
            if j >= stop then List.rev acc, j
            elif text.[j] = '}' then List.rev acc, j
            else
                match parseStatement text j stop with
                | None, j' -> List.rev acc, j'
                | Some stmt, j' -> loop j' (stmt :: acc)

        loop i []

    and private parseStatement (text: string) (i: int) (stop: int) =
        let other, afterOther = readOther text i stop

        if afterOther < stop && text.[afterOther] = '{' then
            let openIdx = afterOther
            let body, afterBody = parseStatements text (afterOther + 1) stop

            let closeIdx, afterClose =
                if afterBody < stop && text.[afterBody] = '}' then
                    Some afterBody, afterBody + 1
                else
                    None, afterBody

            Some {
                otherText = other
                otherStart = i
                openIndex = Some openIdx
                closeIndex = closeIdx
                block = Some body
            },
            afterClose
        elif other = "" then
            None, afterOther
        else
            Some {
                otherText = other
                otherStart = i
                openIndex = None
                closeIndex = None
                block = None
            },
            afterOther

    let parseDocument (text: string) : Statement list =
        fst (parseStatements text 0 text.Length)

    let private splitOtherLines (otherText: string) : string list =
        if otherText = "" then []
        else
            DocumentOutlineOps.splitRawLines otherText
            |> List.map (fun line -> line.content)

    let private lineBody (line: string) =
        let hadLeading =
            line.Length > 0 && Char.IsWhiteSpace line.[0]

        let wsLen =
            DocumentOutlineOps.leadingWhitespace line |> String.length

        let body = line.Substring wsLen

        if hadLeading then body.Trim() else body

    let private depthOf (line: string) (style: PlainTextIndentStyle) =
        let ws = DocumentOutlineOps.leadingWhitespace line

        match style with
        | PlainTextIndentStyle.Tabs ->
            ws |> Seq.filter ((=) '\t') |> Seq.length
        | PlainTextIndentStyle.Spaces n ->
            (ws |> Seq.filter ((=) ' ') |> Seq.length) / n

    let private prepareLines (lines: string list) (_braced: bool) =
        let dropLeading = lines |> List.skipWhile ((=) "")

        match List.rev dropLeading with
        | ws :: rest when String.IsNullOrWhiteSpace ws -> List.rev rest
        | _ -> dropLeading

    let private startsAtLineStart (text: string) (otherStart: int) =
        otherStart = 0
        || text.[otherStart - 1] = '\n'
        || text.[otherStart - 1] = '\r'

    let private ownerDepthOf
        (doc: string)
        (style: PlainTextIndentStyle)
        (parentDepth: int option)
        (stmt: Statement)
        (lines: string list)
        =
        match parentDepth with
        | Some d -> d + 1
        | None ->
            match lines with
            | line :: _ when startsAtLineStart doc stmt.otherStart ->
                depthOf line style
            | _ -> 0

    let private rowDepths
        (style: PlainTextIndentStyle)
        (parentDepth: int option)
        (ownerDepth: int)
        (lines: string list)
        =
        lines
        |> List.mapi (fun i line ->
            if i = 0 then ownerDepth
            else
                match parentDepth with
                | Some d ->
                    d
                    + 1
                    + max 0 (depthOf line style - depthOf lines.Head style)
                | None -> depthOf line style)

    let rec private flattenStmt
        (doc: string)
        (style: PlainTextIndentStyle)
        (parentDepth: int option)
        (stmt: Statement)
        : OutlineRow list =
        if stmt.block.IsNone && String.IsNullOrWhiteSpace stmt.otherText then
            []
        else
            let braced = stmt.block.IsSome
            let lines = prepareLines (splitOtherLines stmt.otherText) braced
            let ownerDepth = ownerDepthOf doc style parentDepth stmt lines

            let ownRows =
                match lines with
                | [] -> []
                | _ ->
                    let depths = rowDepths style parentDepth ownerDepth lines
                    let n = lines.Length

                    List.zip3 lines depths [ 0 .. n - 1 ]
                    |> List.map (fun (line, depth, i) -> {
                        depth = depth
                        text = lineBody line
                        braced = braced && i = n - 1
                    })

            let bracedDepth =
                ownRows
                |> List.tryFindBack (fun r -> r.braced)
                |> Option.map (fun r -> r.depth)
                |> Option.defaultValue ownerDepth

            let childRows =
                match stmt.block with
                | None -> []
                | Some body ->
                    let p =
                        if ownRows = [] then parentDepth
                        else Some bracedDepth

                    body |> List.collect (flattenStmt doc style p)

            if ownRows = [] then childRows else ownRows @ childRows

    let toOutlineRows (text: string) : PlainTextIndentStyle * OutlineRow list =
        let style, _ = PlainTextDocument.flattenText text

        style,
        parseDocument text |> List.collect (flattenStmt text style None)

    let private slice (text: string) (startIdx: int) (endIdx: int) =
        if endIdx <= startIdx then ""
        else text.Substring(startIdx, endIdx - startIdx)

    let private closeRawSlice (text: string) (ci: int) : string * int =
        let start =
            let rec back i =
                if i <= 0 then 0
                else
                    match text.[i - 1] with
                    | ' '
                    | '\t' -> back (i - 1)
                    | '\n'
                    | '\r' -> i
                    | _ -> ci

            back ci

        let endExcl =
            let i = ci + 1

            if i >= text.Length then i
            elif i + 1 < text.Length && text.[i] = '\r' && text.[i + 1] = '\n' then
                i + 2
            elif text.[i] = '\n' || text.[i] = '\r' then i + 1
            else i

        slice text start endExcl, endExcl

    let private consumeNewline (text: string) (i: int) =
        if i + 1 < text.Length && text.[i] = '\r' && text.[i + 1] = '\n' then
            i + 2
        elif i < text.Length && (text.[i] = '\n' || text.[i] = '\r') then
            i + 1
        else
            i

    /// End index after `{` plus its trailing newline when braced.
    let private headerOpenEnd (text: string) (stmt: Statement) =
        match stmt.openIndex with
        | Some oi -> consumeNewline text (oi + 1)
        | None -> stmt.otherStart + stmt.otherText.Length

    /// Raw lines of otherText from cursor, with prepareLines filtering.
    let private preparedOpenLines
        (text: string)
        (cursor: int)
        (stmt: Statement)
        =
        let otherEnd = stmt.otherStart + stmt.otherText.Length
        let rawLines =
            DocumentOutlineOps.splitRawLines (slice text cursor otherEnd)

        let dropLeading =
            rawLines |> List.skipWhile (fun l -> l.content = "")

        let preparedRaw =
            match List.rev dropLeading with
            | ws :: rest when String.IsNullOrWhiteSpace ws.content ->
                List.rev rest
            | _ -> dropLeading

        let prefixLen = rawLines.Length - dropLeading.Length

        let ends =
            (cursor, rawLines)
            ||> List.mapFold (fun pos line ->
                let next = pos + line.raw.Length
                next, next)
            |> fst

        prefixLen, preparedRaw, ends

    /// Per-outline-row openRaw slices (no sibling overlap).
    let private openRawSlices
        (text: string)
        (cursor: int)
        (stmt: Statement)
        (n: int)
        : string list * int =
        let prefixLen, preparedRaw, ends =
            preparedOpenLines text cursor stmt

        let openEnd =
            match stmt.openIndex with
            | Some _ -> headerOpenEnd text stmt
            | None ->
                if n = 0 || preparedRaw.IsEmpty then cursor
                else
                    let lastIdx = prefixLen + n - 1

                    if lastIdx < ends.Length then ends.[lastIdx]
                    else stmt.otherStart + stmt.otherText.Length

        let startOf i =
            if i = 0 then cursor
            else ends.[prefixLen + i - 1]

        let opens =
            [ 0 .. n - 1 ]
            |> List.map (fun i ->
                let stop =
                    if i < n - 1 then startOf (i + 1) else openEnd

                slice text (startOf i) stop)

        opens, openEnd

    /// Warm Keep units in the same preorder as toOutlineRows.
    let toWarmUnits (text: string) : WarmUnit list =
        let style, _ = PlainTextDocument.flattenText text

        let rec walk
            (parentDepth: int option)
            (stmt: Statement)
            (cursor: int)
            (acc: WarmUnit list)
            : WarmUnit list * int =
            if stmt.block.IsNone && String.IsNullOrWhiteSpace stmt.otherText then
                acc, cursor
            else
                let braced = stmt.block.IsSome
                let lines = prepareLines (splitOtherLines stmt.otherText) braced
                let ownerDepth = ownerDepthOf text style parentDepth stmt lines

                match lines with
                | [] ->
                    match stmt.block with
                    | None -> acc, cursor
                    | Some body ->
                        body
                        |> List.fold
                            (fun (acc, c) child ->
                                walk parentDepth child c acc)
                            (acc, cursor)
                | _ ->
                    let depths = rowDepths style parentDepth ownerDepth lines
                    let n = lines.Length
                    let texts = lines |> List.map lineBody
                    let bracedDepth = depths.[n - 1]

                    let openRaws, openEnd =
                        openRawSlices text cursor stmt n

                    let closeRaw, afterCloseBrace =
                        match stmt.closeIndex with
                        | Some ci -> closeRawSlice text ci
                        | None -> "", openEnd

                    let units =
                        [ 0 .. n - 1 ]
                        |> List.map (fun i -> {
                            depth = depths.[i]
                            text = texts.[i]
                            braced = braced && i = n - 1
                            openRaw = openRaws.[i]
                            closeRaw =
                                if braced && i = n - 1 then closeRaw
                                else ""
                        })

                    let acc' =
                        units |> List.fold (fun a unit -> unit :: a) acc

                    let acc'', afterChildren =
                        match stmt.block with
                        | None -> acc', openEnd
                        | Some body ->
                            body
                            |> List.fold
                                (fun (acc, c) child ->
                                    walk (Some bracedDepth) child c acc)
                                (acc', openEnd)

                    let afterClose =
                        match stmt.closeIndex with
                        | Some _ -> afterCloseBrace
                        | None -> afterChildren

                    acc'', afterClose

        let unitsRev, cursor =
            parseDocument text
            |> List.fold
                (fun (acc, c) stmt ->
                    walk None stmt c acc)
                ([], 0)

        let units = List.rev unitsRev

        if cursor < text.Length && units <> [] then
            let attachIdx =
                units
                |> List.tryFindIndexBack (fun u -> u.closeRaw <> "")
                |> Option.defaultValue (units.Length - 1)

            units
            |> List.mapi (fun i u ->
                if i = attachIdx then
                    { u with
                        closeRaw = u.closeRaw + text.Substring cursor
                    }
                else
                    u)
        else
            units
