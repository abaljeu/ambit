namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module RefExprParse =

    [<Literal>]
    let private maxInputLength = 4000

    type private Token =
        | Slash
        | DoubleSlash
        | Hash
        | Caret
        | MultiWildcard
        | IndexOffset of int option
        | ChildIndex of int option
        | NamePattern of string

    let private isNamePatternChar (c: char) =
        Char.IsLetterOrDigit c
        || c = '@'
        || c = '.'
        || c = '-'
        || c = '_'
        || c = '?'
        || c = '*'

    let private isDelimiter (c: char) =
        Char.IsWhiteSpace c
        || c = '/'
        || c = '#'
        || c = '^'
        || c = '!'
        || c = ':'
        || c = '['
        || c = ']'

    let private tryReadSignedInt (s: string) (start: int) : (int * int) option =
        let rec readDigits i =
            if i >= s.Length || not (Char.IsDigit s.[i]) then
                i
            else
                readDigits (i + 1)

        if start >= s.Length then
            None
        else
            match s.[start] with
            | '+' | '-' as _ ->
                let digitsAt = start + 1

                if digitsAt >= s.Length || not (Char.IsDigit s.[digitsAt]) then
                    None
                else
                    let endAt = readDigits digitsAt

                    match Int32.TryParse(s.Substring(start, endAt - start)) with
                    | true, n -> Some(n, endAt)
                    | _ -> None
            | c when Char.IsDigit c ->
                let endAt = readDigits start

                match Int32.TryParse(s.Substring(start, endAt - start)) with
                | true, n -> Some(n, endAt)
                | _ -> None
            | _ -> None

    let private readNamePattern (s: string) (start: int) =
        let rec loop i =
            if i >= s.Length || isDelimiter s.[i] then
                i
            elif isNamePatternChar s.[i] then
                loop (i + 1)
            elif s.[i] = ':' then
                i
            else
                i

        let endAt = loop start

        if endAt = start then
            Error $"unexpected character '{s.[start]}'"
        else
            Ok(s.Substring(start, endAt - start), endAt)

    let private isMultiWildcard (s: string) (start: int) =
        start + 1 < s.Length
        && s.[start] = '*'
        && s.[start + 1] = '*'
        && (start + 2 = s.Length || isDelimiter s.[start + 2])

    let private tokenize (input: string) : Result<Token list, string> =
        let rec loop i acc =
            if i >= input.Length then
                Ok(List.rev acc)
            else
                match input.[i] with
                | c when Char.IsWhiteSpace c -> loop (i + 1) acc
                | '/' when i + 1 < input.Length && input.[i + 1] = '/' ->
                    loop (i + 2) (DoubleSlash :: acc)
                | '/' -> loop (i + 1) (Slash :: acc)
                | '#' -> loop (i + 1) (Hash :: acc)
                | '^' -> loop (i + 1) (Caret :: acc)
                | '!' ->
                    match tryReadSignedInt input (i + 1) with
                    | Some (n, next) -> loop next (IndexOffset(Some n) :: acc)
                    | None -> loop (i + 1) (IndexOffset None :: acc)
                | '"' -> Error "quoted path segments are not supported"
                | '['
                | ']' -> Error "postfix not supported yet"
                | ':' ->
                    match tryReadSignedInt input (i + 1) with
                    | Some (n, next) -> loop next (ChildIndex(Some n) :: acc)
                    | None -> loop (i + 1) (ChildIndex None :: acc)
                | '*' when isMultiWildcard input i -> loop (i + 2) (MultiWildcard :: acc)
                | _ ->
                    readNamePattern input i
                    |> Result.bind (fun (name, next) -> loop next (NamePattern name :: acc))

        loop 0 []

    let private parseAnchor tokens : ExprAnchor * Token list =
        match tokens with
        | DoubleSlash :: rest -> GlobalRoot, rest
        | Slash :: rest -> WorkspaceRoot, rest
        | Caret :: rest -> Structural, rest
        | Hash :: NamePattern _ :: _ -> Context, tokens
        | Hash :: rest -> Tagged, rest
        | NamePattern "." :: rest -> CurrentDir, rest
        | _ -> Context, tokens

    let private parseStep tokens : Result<ExprStep * Token list, string> =
        match tokens with
        | MultiWildcard :: rest -> Ok(MultiWild, rest)
        | IndexOffset offset :: rest -> Ok(IndexStep offset, rest)
        | ChildIndex index :: rest -> Ok(ChildStep index, rest)
        | Hash :: NamePattern name :: rest -> Ok(TagStep name, rest)
        | Hash :: _ -> Error "expected tag name after #"
        | NamePattern name :: Slash :: rest -> Ok(DirStep name, rest)
        | NamePattern name :: rest -> Ok(FileStep name, rest)
        | [] -> Error "expected step"
        | _ -> Error "expected step"

    let private parseSteps tokens : Result<ExprStep list, string> =
        let rec loop remaining acc =
            match remaining with
            | [] -> Ok(List.rev acc)
            | Slash :: [] -> Ok(List.rev acc)
            | Slash :: rest ->
                parseStep rest
                |> Result.bind (fun (step, next) -> loop next (step :: acc))
            | _ ->
                parseStep remaining
                |> Result.bind (fun (step, next) -> loop next (step :: acc))

        loop tokens []

    let private parseTokens tokens =
        let anchor, rest = parseAnchor tokens

        parseSteps rest
        |> Result.map (function
            | [] -> AnchorOnly anchor
            | steps -> Path(anchor, steps))

    let format (expr: PathExpr) : string =
        let formatAnchor =
            function
            | Context -> ""
            | WorkspaceRoot -> "/"
            | GlobalRoot -> "//"
            | CurrentDir -> "."
            | Structural -> "^"
            | Tagged -> "#"

        let formatStep =
            function
            | DirStep name -> name + "/"
            | FileStep name -> name
            | TagStep tag -> "#" + tag
            | MultiWild -> "**"
            | IndexStep None -> "!"
            | IndexStep (Some n) -> "!" + string n
            | ChildStep None -> ":"
            | ChildStep (Some n) -> ":" + string n

        let joinSteps (anchor: ExprAnchor) (steps: ExprStep list) =
            match steps with
            | [] -> ""
            | step :: rest ->
                let firstSep =
                    match anchor with
                    | Context
                    | WorkspaceRoot
                    | GlobalRoot -> ""
                    | _ -> "/"

                let first = firstSep + formatStep step

                rest
                |> List.fold (fun acc s ->
                    if acc.EndsWith("/") then
                        acc + formatStep s
                    else
                        acc + "/" + formatStep s) first

        match expr with
        | AnchorOnly anchor -> formatAnchor anchor
        | Path(anchor, steps) -> formatAnchor anchor + joinSteps anchor steps

    let parse (input: string) : Result<PathExpr, string> =
        if String.IsNullOrWhiteSpace input then
            Error "empty expression"
        elif input.Length > maxInputLength then
            Error "expression too long"
        else
            tokenize input
            |> Result.bind parseTokens
