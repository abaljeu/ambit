namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module ExprPathClusterParse =

    [<Literal>]
    let missingArgument = "missing argument"

    type private Token =
        | Slash
        | DoubleSlash
        | Hash
        | Caret
        | Dot
        | MultiWildcard
        | Colon
        | Bang
        | ChildAt of int option
        | SiblingAt of int option
        | NamePattern of string

    type private LastEmit =
        | Start
        | UpWalk
        | Other

    type private Pending =
        | StructuralName
        | ContentName

    let private isNameChar (c: char) =
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
        || c = '('
        || c = ')'
        || c = ','
        || c = '"'

    let private readName (s: string) (start: int) : Result<string * int, string> =
        let rec loop i =
            if i >= s.Length || isDelimiter s.[i] then
                i
            elif isNameChar s.[i] then
                loop (i + 1)
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
                | ':' ->
                    if i + 1 < input.Length && input.[i + 1] = '*' then
                        loop (i + 2) (ChildAt None :: acc)
                    else
                        match tryReadSignedInt input (i + 1) with
                        | Some (n, next) -> loop next (ChildAt(Some n) :: acc)
                        | None -> loop (i + 1) (Colon :: acc)
                | '!' ->
                    if i + 1 < input.Length && input.[i + 1] = '*' then
                        loop (i + 2) (SiblingAt None :: acc)
                    else
                        match tryReadSignedInt input (i + 1) with
                        | Some (n, next) -> loop next (SiblingAt(Some n) :: acc)
                        | None -> loop (i + 1) (Bang :: acc)
                | '*' when isMultiWildcard input i -> loop (i + 2) (MultiWildcard :: acc)
                | '.' ->
                    if
                        i + 1 < input.Length
                        && isNameChar input.[i + 1]
                        && input.[i + 1] <> '/'
                    then
                        readName input i
                        |> Result.bind (fun (name, next) -> loop next (NamePattern name :: acc))
                    else
                        loop (i + 1) (Dot :: acc)
                | _ ->
                    readName input i
                    |> Result.bind (fun (name, next) -> loop next (NamePattern name :: acc))

        loop 0 []

    let private needsImplicit (last: LastEmit) =
        match last with
        | Start
        | UpWalk -> true
        | Other -> false

    let private finish pending steps literal =
        match pending, literal with
        | None, None -> Ok steps
        | Some StructuralName, None -> Error missingArgument
        | Some ContentName, None -> Error missingArgument
        | Some StructuralName, Some name ->
            Ok(ClusterStep.Structural name :: steps)
        | Some ContentName, Some name -> Ok(ClusterStep.Content name :: steps)
        | None, Some _ -> Error "unexpected literal"

    let private parseTokenList tokens trailingLiteral =
        let rec loop remaining steps last pending =
            match remaining with
            | [] -> finish pending steps trailingLiteral
            | DoubleSlash :: rest ->
                loop rest (ClusterStep.Root :: steps) Other (Some StructuralName)
            | Slash :: rest -> loop rest steps Other (Some StructuralName)
            | Hash :: rest -> loop rest steps Other (Some ContentName)
            | Caret :: rest -> loop rest (ClusterStep.StructuralUp :: steps) UpWalk None
            | Dot :: rest -> loop rest (ClusterStep.DirectoryUp :: steps) UpWalk None
            | MultiWildcard :: rest -> loop rest (ClusterStep.Tree :: steps) UpWalk None
            | ChildAt n :: rest -> loop rest (ClusterStep.ChildAt n :: steps) Other None
            | SiblingAt n :: rest -> loop rest (ClusterStep.SiblingAt n :: steps) Other None
            | Colon :: _
            | Bang :: _ -> Error missingArgument
            | NamePattern name :: rest ->
                match pending with
                | Some StructuralName -> loop rest (ClusterStep.Structural name :: steps) Other None
                | Some ContentName -> loop rest (ClusterStep.Content name :: steps) Other None
                | None when needsImplicit last ->
                    loop rest (ClusterStep.Structural name :: steps) Other None
                | None -> Error "unexpected name pattern"

        loop tokens [] Start None

    let private parseInternal input trailingLiteral =
        tokenize input
        |> Result.bind (fun tokens -> parseTokenList tokens trailingLiteral)
        |> Result.map List.rev

    let parse (input: string) : Result<PathCluster, string> =
        if String.IsNullOrWhiteSpace input then
            Error "empty cluster"
        else
            parseInternal input None

    let parseWithTrailingName (input: string) (name: string) : Result<PathCluster, string> =
        if String.IsNullOrWhiteSpace input then
            Error "empty cluster"
        else
            parseInternal input (Some name)
