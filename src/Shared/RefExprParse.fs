namespace Gambol.Shared

open System
open System.Text

[<RequireQualifiedAccess>]
module RefExprParse =

    [<Literal>]
    let private maxInputLength = 4000

    let private isNamePatternChar (c: char) =
        Char.IsLetterOrDigit c || c = '.' || c = '-' || c = '_' || c = '?' || c = '*'

    let private isWorkspaceLabelChar (c: char) =
        Char.IsLetterOrDigit c || c = '-' || c = '_'

    let private normalizeInput (input: string) : string =
        let trimmed = input.Trim()
        let sb = StringBuilder(trimmed.Length)

        let appendSlash () =
            if sb.Length = 0 || sb.[sb.Length - 1] <> '/' then
                sb.Append('/') |> ignore

        let appendToken (token: string) =
            if token.Length > 0 then
                if sb.Length > 0 && sb.[sb.Length - 1] <> '/' then
                    sb.Append('/') |> ignore

                sb.Append(token) |> ignore

        let mutable i = 0

        while i < trimmed.Length do
            if trimmed.[i] = '/' then
                if i + 1 < trimmed.Length && trimmed.[i + 1] = '/' && sb.Length = 0 then
                    sb.Append("//") |> ignore
                    i <- i + 2
                else
                    appendSlash ()
                    i <- i + 1

                    while i < trimmed.Length && Char.IsWhiteSpace trimmed.[i] do
                        i <- i + 1
            elif Char.IsWhiteSpace trimmed.[i] then
                i <- i + 1
            else
                let start = i
                i <- i + 1

                while i < trimmed.Length && trimmed.[i] <> '/' do
                    i <- i + 1

                appendToken (trimmed.Substring(start, i - start).Trim())

                while i < trimmed.Length && Char.IsWhiteSpace trimmed.[i] do
                    i <- i + 1

        sb.ToString()

    let private parseQuoted (s: string) (start: int) : Result<string * int, string> =
        if start >= s.Length || s.[start] <> '"' then
            Error "expected quoted string"
        else
            let rec loop (i: int) (sb: StringBuilder) : Result<string * int, string> =
                if i >= s.Length then
                    Error "unclosed string"
                else
                    let c = s.[i]

                    if c = '"' then
                        Ok(sb.ToString(), i + 1)
                    elif c = '\\' && i + 1 < s.Length then
                        loop (i + 2) (sb.Append(s.[i + 1]))
                    elif c = '\r' || c = '\n' then
                        Error "unclosed string"
                    else
                        loop (i + 1) (sb.Append(c))

            loop (start + 1) (StringBuilder())

    let private parseNamePattern (s: string) (start: int) : Result<string * int, string> =
        if start >= s.Length then
            Error "expected step"
        elif s.[start] = '"' then
            parseQuoted s start
        else
            let rec take (i: int) =
                if i >= s.Length || s.[i] = '/' then
                    i
                else
                    let c = s.[i]

                    if isNamePatternChar c then
                        take (i + 1)
                    else
                        i

            let endAt = take start

            if endAt = start then
                Error "expected step"
            else
                Ok(s.Substring(start, endAt - start), endAt)

    let private hasNamePatternAfterHash (s: string) (start: int) : bool =
        start + 1 < s.Length && isNamePatternChar s.[start + 1]

    let private parseOneStep (s: string) (start: int) : Result<ExprStep * int, string> =
        if start >= s.Length then
            Error "expected step"
        elif s.[start] = '#' then
            if not (hasNamePatternAfterHash s start) then
                Error "expected tag name after #"
            else
                parseNamePattern s (start + 1)
                |> Result.map (fun (name, i) -> TagStep name, i)
        elif s.[start] = '*' then
            let isStepEnd i = i >= s.Length || s.[i] = '/'

            if start + 1 < s.Length && s.[start + 1] = '*' && isStepEnd (start + 2) then
                Ok(MultiWild, start + 2)
            else
                parseNamePattern s start
                |> Result.bind (fun (name, i) ->
                    if i < s.Length && s.[i] = '/' then
                        Ok(DirStep name, i + 1)
                    else
                        Ok(FileStep name, i))
        else
            parseNamePattern s start
            |> Result.bind (fun (name, i) ->
                if i < s.Length && s.[i] = '/' then
                    Ok(DirStep name, i + 1)
                else
                    Ok(FileStep name, i))

    let private rejectPostfix (s: string) (pos: int) : Result<unit, string> =
        if pos >= s.Length then
            Ok ()
        elif s.[pos] = '.' || s.[pos] = '[' then
            Error "postfix not supported yet"
        else
            Error "unexpected trailing input"

    let private parseSteps (s: string) : Result<ExprStep list * int, string> =
        let rec loop (pos: int) (acc: ExprStep list) : Result<ExprStep list * int, string> =
            if pos >= s.Length then
                Ok(List.rev acc, pos)
            else
                let pos2 = if s.[pos] = '/' then pos + 1 else pos

                if pos2 >= s.Length then
                    if pos < s.Length && s.[pos] = '/' then
                        Ok(List.rev acc, pos)
                    else
                        Error "expected step after /"
                else
                    if s.[pos2] = '[' || s.[pos2] = '.' then
                        rejectPostfix s pos2 |> Result.map (fun _ -> List.rev acc, pos)
                    else
                        match parseOneStep s pos2 with
                        | Error e -> Error e
                        | Ok(step, next) -> loop next (step :: acc)

        if String.IsNullOrEmpty s then
            Ok([], 0)
        else
            loop 0 []

    let private parseAnchor (s: string) : Result<ExprAnchor * string, string> =
        if String.IsNullOrEmpty s then
            Ok(Context, "")
        elif s.StartsWith "//" then
            Ok(GlobalRoot, s.Substring 2)
        elif s.[0] = '/' then
            Ok(WorkspaceRoot, s.Substring 1)
        elif s.[0] = '^' then
            Ok(Structural, s.Substring 1)
        elif s.[0] = '.' then
            Ok(CurrentDir, s.Substring 1)
        elif s.[0] = '#' && not (hasNamePatternAfterHash s 0) then
            Ok(Tagged, s.Substring 1)
        elif s.[0] = '@' then
            let colon = s.IndexOf(':', 1)

            if colon < 0 then
                Error "expected ':' after workspace label"
            else
                let label = s.Substring(1, colon - 1).Trim()

                if label.Length = 0 then
                    Ok(WorkspaceRoot, s.Substring(colon + 1))
                elif label |> Seq.exists (fun c -> not (isWorkspaceLabelChar c)) then
                    Error "invalid workspace label"
                else
                    Ok(NamedWorkspace label, s.Substring(colon + 1))
        else
            Ok(Context, s)

    let format (expr: PathExpr) : string =
        let formatAnchor =
            function
            | Context -> ""
            | WorkspaceRoot -> "/"
            | GlobalRoot -> "//"
            | CurrentDir -> "."
            | Structural -> "^"
            | Tagged -> "#"
            | NamedWorkspace label -> "@" + label + ":"

        let formatStep =
            function
            | DirStep name when name.Contains(' ') || name.Contains('/') ->
                "\"" + name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"/"
            | DirStep name -> name + "/"
            | FileStep name when name.Contains(' ') || name.Contains('/') ->
                "\"" + name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""
            | FileStep name -> name
            | TagStep tag -> "#" + tag
            | MultiWild -> "**"

        let joinSteps (anchor: ExprAnchor) (steps: ExprStep list) =
            match steps with
            | [] -> ""
            | step :: rest ->
                let firstSep =
                    match anchor with
                    | NamedWorkspace _
                    | Context
                    | WorkspaceRoot
                    | GlobalRoot -> ""
                    | _ -> "/"

                let first = firstSep + formatStep step

                rest
                |> List.fold (fun acc s -> acc + "/" + formatStep s) first

        match expr with
        | AnchorOnly anchor -> formatAnchor anchor
        | Path(anchor, steps) -> formatAnchor anchor + joinSteps anchor steps

    let parse (input: string) : Result<PathExpr, string> =
        if String.IsNullOrWhiteSpace input then
            Error "empty expression"
        elif input.Length > maxInputLength then
            Error "expression too long"
        else
            let s = normalizeInput input

            match parseAnchor s with
            | Error e -> Error e
            | Ok(anchor, rest) ->
                match parseSteps rest with
                | Error e -> Error e
                | Ok(steps, endPos) ->
                    match rejectPostfix rest endPos with
                    | Error e -> Error e
                    | Ok _ ->
                        if List.isEmpty steps then
                            Ok(AnchorOnly anchor)
                        else
                            Ok(Path(anchor, steps))
