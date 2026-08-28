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

    type private Segment =
        | Quoted of string
        | Raw of string

    let private segmentRegex =
        Regex(@"""[^""]*""|[^""\s(),]+")

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

    let private splitSegments (input: string) : Segment list =
        segmentRegex.Matches input
        |> Seq.cast<Match>
        |> Seq.map (fun m -> m.Value)
        |> Seq.filter (not << String.IsNullOrWhiteSpace)
        |> Seq.map (fun seg ->
            if seg.Length >= 2 && seg.[0] = '"' && seg.[seg.Length - 1] = '"' then
                Quoted(seg.Substring(1, seg.Length - 2))
            else
                Raw seg)
        |> Seq.toList

    let private wordWantsTrailingLiteral (word: string) =
        word = "containing" || word = "named" || word = "class"

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

    let private parseSegments segments =
        let rec loop remaining acc =
            match remaining with
            | [] -> Ok(List.rev acc)
            | Quoted _ :: _ -> Error "unexpected quoted literal"
            | Raw word :: Quoted text :: rest when wordWantsTrailingLiteral word ->
                parseWord word (Some text)
                |> Result.bind (fun term -> loop rest (term :: acc))
            | Raw word :: rest when isPathCluster word ->
                let literal, after =
                    match rest with
                    | Quoted text :: tail -> Some text, tail
                    | _ -> None, rest

                parseClusterSegment word literal
                |> Result.bind (fun term -> loop after (term :: acc))
            | Raw word :: rest when isSignedInteger word -> Error numberOnlyOperand
            | Raw word :: rest ->
                parseWord word None
                |> Result.bind (fun term -> loop rest (term :: acc))

        loop segments []

    let parseExpr (input: string) : Result<ExprSeq, string> =
        if String.IsNullOrWhiteSpace input then
            Error "empty expression"
        else
            splitSegments input |> parseSegments

    let parseCluster = ExprPathClusterParse.parse
