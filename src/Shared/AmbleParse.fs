namespace Gambol.Shared

open System
open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
module AmbleParse =

    [<Literal>]
    let private maxInputLength = 4000

    type private Token =
        | TStr of string
        | TNum of AmbleNumber
        | TRef of PathExpr
        | TWord of string
        | TComma
        | TLParen
        | TRParen
        | TEquals
        | TLt
        | TGt
        | TGtGt
        | TPipe

    let private segmentRegex =
        Regex(@"""[^""]*""|>>|[<>|=(),]|[^<>|=(),\s]+")

    let private canStartRef (seg: string) =
        seg.Length > 0
        && match seg.[0] with
           | '/' | '#' | '^' | '!' -> true
           | '.' -> seg.Length = 1 || (seg.Length > 1 && seg.[1] = '/')
           | _ -> false

    let private isDigitLedRef (seg: string) =
        seg.Length > 0
        && match seg.[0] with
           | c when Char.IsDigit c -> true
           | '+' | '-' -> seg.Length > 1 && Char.IsDigit seg.[1]
           | _ -> false

    let private readDigits (seg: string) (start: int) =
        let rec loop i =
            if i < seg.Length && Char.IsDigit seg.[i] then
                loop (i + 1)
            else
                i

        loop start

    let rec private parseNumberSegment (seg: string) : Result<AmbleNumber, string> =
        let digitsAt =
            match seg.[0] with
            | '+' | '-' -> 1
            | _ -> 0

        let afterInt = readDigits seg digitsAt

        if afterInt = digitsAt then
            Error "expected digits"
        elif afterInt < seg.Length && seg.[afterInt] = '.' then
            parseFloatSegment seg afterInt
        elif afterInt < seg.Length && (seg.[afterInt] = 'e' || seg.[afterInt] = 'E') then
            Error "exponent notation is not supported"
        elif afterInt <> seg.Length then
            Error "unexpected character after number"
        else
            match Int64.TryParse seg with
            | true, n -> Ok(Int n)
            | _ -> Error "invalid number"

    and private parseFloatSegment (seg: string) (dotAt: int) =
        let afterFrac = readDigits seg (dotAt + 1)

        if afterFrac = dotAt + 1 then
            Error "expected digits after decimal point"
        elif afterFrac < seg.Length && (seg.[afterFrac] = 'e' || seg.[afterFrac] = 'E') then
            Error "exponent notation is not supported"
        elif afterFrac <> seg.Length then
            Error "unexpected character after number"
        else
            match Decimal.TryParse seg with
            | true, n -> Ok(Float n)
            | _ -> Error "invalid number"

    let private tryClassifyRef (seg: string) =
        if canStartRef seg || isDigitLedRef seg then
            RefExpr.parse seg |> Result.map TRef
        else
            Error "not a reference"

    let private classify (seg: string) : Result<Token, string> =
        if seg.Length >= 2 && seg.[0] = '"' && seg.[seg.Length - 1] = '"' then
            Ok(TStr(seg.Substring(1, seg.Length - 2)))
        else
            match seg with
            | "," -> Ok TComma
            | "(" -> Ok TLParen
            | ")" -> Ok TRParen
            | "=" -> Ok TEquals
            | "<" -> Ok TLt
            | ">" -> Ok TGt
            | ">>" -> Ok TGtGt
            | "|" -> Ok TPipe
            | _ ->
                match parseNumberSegment seg with
                | Ok num -> Ok(TNum num)
                | Error "exponent notation is not supported" -> Error "exponent notation is not supported"
                | Error _ ->
                    match tryClassifyRef seg with
                    | Ok tok -> Ok tok
                    | Error _ -> Ok(TWord seg)

    let private tokenize (input: string) : Result<Token list, string> =
        let matches = segmentRegex.Matches input

        let rec loop i acc =
            if i >= matches.Count then
                Ok(List.rev acc)
            else
                classify matches.[i].Value
                |> Result.bind (fun tok -> loop (i + 1) (tok :: acc))

        if matches.Count = 0 && not (String.IsNullOrWhiteSpace input) then
            Error "expected input"
        else
            loop 0 []

    let private isArgStarter = function
        | [] -> false
        | TLParen :: _ | TStr _ :: _ | TRef _ :: _ | TNum _ :: _ -> true
        | TWord _ :: _ -> true
        | _ -> false

    let rec private parseInfixExpr tokens =
        match tokens with
        | TWord fn :: TWord "of" :: rest ->
            parseInfixExpr rest
            |> Result.map (fun (arg, next) -> FunCall(fn, [ arg ]), next)
        | _ ->
            parseJuxtapose tokens
            |> Result.bind (fun (left, rest) ->
                match rest with
                | TComma :: r ->
                    parseInfixExpr r
                    |> Result.map (fun (right, next) -> FunCall(",", [ left; right ]), next)
                | _ -> Ok(left, rest))

    and private parseJuxtapose tokens =
        match tokens with
        | [] -> Error "expected expression"
        | TWord fn :: rest when isArgStarter rest -> parseArgs fn rest []
        | TComma :: rest when isArgStarter rest -> parseArgs "," rest []
        | _ -> parsePrimary tokens

    and private parseArgs fn tokens acc =
        if isArgStarter tokens then
            parseInfixExpr tokens
            |> Result.bind (fun (arg, next) -> parseArgs fn next (arg :: acc))
        else
            Ok(FunCall(fn, List.rev acc), tokens)

    and private parsePrimary tokens =
        match tokens with
        | TStr text :: rest -> Ok(Str text, rest)
        | TNum num :: rest -> Ok(Num num, rest)
        | TRef expr :: rest -> Ok(Ref expr, rest)
        | TLParen :: rest ->
            parseExpr false rest
            |> Result.bind (fun (inner, next) ->
                match next with
                | TRParen :: after -> Ok(Paren inner, after)
                | _ -> Error "expected ')'")
        | TWord word :: _ when word.Length > 0 && RefExpr.isNameChar word.[0] ->
            Error "reference requires an explicit anchor"
        | _ -> Error "expected expression"

    and private parseExpr atStatementStart tokens =
        parseInfixExpr tokens
        |> Result.bind (fun (expr, rest) ->
            match atStatementStart, rest with
            | true, [] -> Ok(expr, [])
            | true, _ -> Error "unexpected trailing input"
            | false, _ -> Ok(expr, rest))

    let private numToBare = function
        | Int n -> string n
        | Float n -> string n

    let private parseShellWord = function
        | TStr text :: rest -> Ok(WordStr text, rest)
        | TRef expr :: rest -> Ok(WordRef expr, rest)
        | TWord word :: rest -> Ok(WordBare word, rest)
        | TNum num :: rest -> Ok(WordBare(numToBare num), rest)
        | TLParen :: rest ->
            parseExpr false rest
            |> Result.bind (fun (expr, next) ->
                match next with
                | TRParen :: after -> Ok(WordExpr expr, after)
                | _ -> Error "expected ')'")
        | _ -> Error "expected shell word"

    let rec private parseStageParts acc tokens =
        match tokens with
        | [] -> Ok(List.rev acc)
        | TLt :: rest ->
            parseExpr false rest
            |> Result.bind (fun (expr, next) -> parseStageParts (RedirIn expr :: acc) next)
        | TGtGt :: rest ->
            parseExpr false rest
            |> Result.bind (fun (expr, next) -> parseStageParts (RedirAppend expr :: acc) next)
        | TGt :: rest ->
            parseExpr false rest
            |> Result.bind (fun (expr, next) -> parseStageParts (RedirOut expr :: acc) next)
        | tokens ->
            parseShellWord tokens
            |> Result.bind (fun (word, next) -> parseStageParts (ShellWord word :: acc) next)

    let rec private splitStages acc current = function
        | [] -> (List.rev current :: acc) |> List.rev
        | TPipe :: rest -> splitStages (List.rev current :: acc) [] rest
        | tok :: rest -> splitStages acc (tok :: current) rest

    let private parseStage tokens =
        parseStageParts [] tokens
        |> Result.bind (function
            | [] -> Error "empty command stage"
            | stage -> Ok stage)

    let private parseCmdLine tokens =
        splitStages [] [] tokens
        |> List.fold (fun acc stageTokens ->
            acc |> Result.bind (fun stages ->
                parseStage stageTokens |> Result.map (fun stage -> stage :: stages)))
            (Ok [])
        |> Result.map (List.rev >> Cmd)

    let private parseAssignmentOrExpr tokens =
        match tokens with
        | TWord name :: TEquals :: rest ->
            parseExpr false rest
            |> Result.bind (fun (expr, next) ->
                match next with
                | [] -> Ok(Assign(name, expr))
                | _ -> Error "unexpected trailing input")
        | TGt :: rest ->
            parseCmdLine rest |> Result.map ExprStmt
        | _ ->
            parseExpr true tokens
            |> Result.bind (fun (expr, next) ->
                match next with
                | [] -> Ok(ExprStmt expr)
                | _ -> Error "unexpected trailing input")

    let parseStatement (input: string) : Result<AmbleStatement, string> =
        if String.IsNullOrWhiteSpace input then
            Error "empty statement"
        elif input.Length > maxInputLength then
            Error "statement too long"
        else
            tokenize (input.TrimStart()) |> Result.bind parseAssignmentOrExpr

    let parse input = parseStatement input
