namespace Gambol.Shared

open System
open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
module ExprParse =

    [<Literal>]
    let missingArgument = "missing argument"

    [<Literal>]
    let numberOnlyOperand =
        "a number is only valid as the right operand of : or !"

    type private Token =
        | Quoted of string
        | Raw of string
        | AndKw
        | OrKw
        | NotKw
        | OuterKw
        | IfKw
        | Comma
        | LParen
        | RParen

    let private segmentRegex =
        Regex(@"""[^""]*""|[(),]|[^""\s(),]+")

    let private isPathCluster (seg: string) =
        seg.Length > 0
        && (seg |> Seq.exists (fun c ->
            c = '/'
            || c = '^'
            || c = '#'
            || c = ':'
            || c = '!'
            || c = '*')
            || seg.[0] = '.')

    let private isSignedInteger (seg: string) =
        seg.Length > 0
        && match seg.[0] with
           | c when Char.IsDigit c -> true
           | '+' | '-' -> seg.Length > 1 && Char.IsDigit seg.[1]
           | _ -> false

    let private classify (seg: string) =
        if seg = "," then Comma
        elif seg = "(" then LParen
        elif seg = ")" then RParen
        elif seg = "AND" then AndKw
        elif seg = "OR" then OrKw
        elif seg = "NOT" then NotKw
        elif seg = "OUTER" then OuterKw
        elif seg = "IF" then IfKw
        elif seg.Length >= 2 && seg.[0] = '"' && seg.[seg.Length - 1] = '"' then
            Quoted(seg.Substring(1, seg.Length - 2))
        else
            Raw seg

    let private tokenize (input: string) : Token list =
        segmentRegex.Matches input
        |> Seq.cast<Match>
        |> Seq.map (fun m -> m.Value)
        |> Seq.filter (not << String.IsNullOrWhiteSpace)
        |> Seq.map classify
        |> Seq.toList

    let private wordWantsTrailingLiteral (word: string) =
        word = "containing"
        || word = "named"
        || word = "class"
        || word = "subsection"
        || word = "re"
        || word = "rei"

    let private parseWord (word: string) (literal: string option) : Result<ExprTerm, string> =
        if wordWantsTrailingLiteral word && literal.IsNone then
            Error missingArgument
        else
            Ok(ExprTerm.Word(word, literal))

    let private parseClusterSegment (seg: string) (literal: string option) : Result<ExprTerm, string> =
        match literal with
        | Some name ->
            ExprPathClusterParse.parseWithTrailingName seg name
            |> Result.map (fun cluster -> ExprTerm.Cluster(cluster, None))
        | None ->
            ExprPathClusterParse.parse seg
            |> Result.map (fun cluster -> ExprTerm.Cluster(cluster, None))

    let private takeLiteral rest =
        match rest with
        | Quoted text :: tail -> Some text, tail
        | _ -> None, rest

    let private isTermStart token =
        match token with
        | LParen
        | Quoted _ -> true
        | Raw _ -> true
        | _ -> false

    let private seqExpr items =
        match items with
        | [ one ] -> one
        | many -> Expr.Pipe many

    let private tryPrefix token =
        match token with
        | NotKw -> Some Expr.Not
        | OuterKw -> Some Expr.Outer
        | IfKw -> Some Expr.If
        | _ -> None

    let private attachPrefix wrap left inner =
        match left with
        | Expr.Pipe items -> Expr.Pipe(items @ [ wrap inner ])
        | _ -> Expr.Pipe [ left; wrap inner ]

    let rec private parseOr tokens =
        parseAnd tokens
        |> Result.bind (fun (left, rest) -> parseOrTail left rest)

    and private parseOrTail left tokens =
        match tokens with
        | OrKw :: rest
        | Comma :: rest ->
            parseAnd rest
            |> Result.bind (fun (right, rest2) ->
                parseOrTail (Expr.Or(left, right)) rest2)
        | _ -> Ok(left, tokens)

    and private parseAnd tokens =
        parseNot tokens
        |> Result.bind (fun (left, rest) -> parseAndTail left rest)

    and private parseAndTail left tokens =
        match tokens with
        | AndKw :: rest ->
            parseNot rest
            |> Result.bind (fun (right, rest2) ->
                parseAndTail (Expr.And(left, right)) rest2)
        | _ -> Ok(left, tokens)

    and private parseNot tokens =
        match tokens with
        | tok :: rest ->
            match tryPrefix tok with
            | Some wrap ->
                parseNot rest
                |> Result.map (fun (inner, rest2) -> wrap inner, rest2)
            | None -> parseNotAfterSeq tokens
        | [] -> parseNotAfterSeq tokens

    and private parseNotAfterSeq tokens =
        parseSeq tokens
        |> Result.bind (fun (seqNode, rest) ->
            match rest with
            | tok :: rest2 ->
                match tryPrefix tok with
                | Some wrap ->
                    parseNot rest2
                    |> Result.map (fun (inner, rest3) ->
                        attachPrefix wrap seqNode inner, rest3)
                | None -> Ok(seqNode, rest)
            | [] -> Ok(seqNode, rest))

    and private parseSeq tokens =
        parseTerm tokens
        |> Result.bind (fun (first, rest) -> parseSeqTail [ first ] rest)

    and private parseSeqTail acc tokens =
        match tokens with
        | t :: _ when isTermStart t ->
            parseTerm tokens
            |> Result.bind (fun (next, rest) -> parseSeqTail (next :: acc) rest)
        | _ -> Ok(seqExpr (List.rev acc), tokens)

    and private parseTerm tokens =
        match tokens with
        | LParen :: rest -> parseGroup rest
        | Quoted _ :: _ -> Error "unexpected quoted literal"
        | Raw word :: rest when isPathCluster word ->
            let literal, after = takeLiteral rest
            parseClusterSegment word literal
            |> Result.map (fun term -> Expr.Term term, after)
        | Raw word :: rest when isSignedInteger word -> Error numberOnlyOperand
        | Raw word :: rest ->
            let literal, after =
                if wordWantsTrailingLiteral word then takeLiteral rest
                else None, rest
            parseWord word literal
            |> Result.map (fun term -> Expr.Term term, after)
        | _ -> Error "empty expression"

    and private parseGroup tokens =
        parseOr tokens
        |> Result.bind (fun (inner, rest) ->
            match rest with
            | RParen :: rest2 -> Ok(inner, rest2)
            | _ -> Error "missing )")

    let parseExpr (input: string) : Result<Expr, string> =
        if String.IsNullOrWhiteSpace input then
            Error "empty expression"
        else
            parseOr (tokenize input)
            |> Result.bind (fun (expr, rest) ->
                match rest with
                | [] -> Ok expr
                | RParen :: _ -> Error "unexpected )"
                | _ -> Error "unexpected token")

    let parseCluster = ExprPathClusterParse.parse
