namespace Gambol.Shared

open System
open System.Collections.Generic
open System.Text

type ExprBase =
    | WorkspaceRoot
    | FileRoot
    | FileDir
    | NamedWorkspace of string

type ExprStep =
    | NameStep of string
    | TagStep of string
    | SingleWild
    | MultiWild

type PathExpr =
    | BaseOnly of ExprBase
    | Path of ExprBase * ExprStep list

type RefContext =
    { workspaceRoot: NodeId option
      fileRoot: NodeId option
      fileDir: NodeId option
      namedWorkspaces: Map<string, NodeId> }

[<RequireQualifiedAccess>]
module RefExpr =

    [<Literal>]
    let private maxInputLength = 4000

    let private isIdentChar (c: char) =
        Char.IsLetterOrDigit c || c = '.' || c = '-' || c = '_' || c = '?' || c = '*'

    let private isTagIdentChar (c: char) =
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

    let private parseIdentifier (s: string) (start: int) (allowGlob: bool) : Result<string * int, string> =
        if start >= s.Length then
            Error "expected step"
        else
            let rec take (i: int) =
                if i >= s.Length || s.[i] = '/' then
                    i
                else
                    let c = s.[i]

                    if allowGlob && (c = '*' || c = '?') then
                        take (i + 1)
                    elif isIdentChar c then
                        take (i + 1)
                    else
                        i

            let endAt = take start

            if endAt = start then
                Error "expected step"
            else
                Ok(s.Substring(start, endAt - start), endAt)

    let private parseOneStep (s: string) (start: int) : Result<ExprStep * int, string> =
        if start >= s.Length then
            Error "expected step"
        elif s.[start] = '"' then
            parseQuoted s start |> Result.map (fun (text, i) -> NameStep text, i)
        elif s.[start] = '#' then
            if start + 1 >= s.Length then
                Error "expected tag name after #"
            else
                let mutable i = start + 1

                while i < s.Length && s.[i] <> '/' && isTagIdentChar s.[i] do
                    i <- i + 1

                if i = start + 1 then
                    Error "expected tag name after #"
                else
                    Ok(TagStep(s.Substring(start + 1, i - start - 1)), i)
        elif s.[start] = '*' then
            let isStepEnd i = i >= s.Length || s.[i] = '/'

            if start + 1 < s.Length && s.[start + 1] = '*' && isStepEnd (start + 2) then
                Ok(MultiWild, start + 2)
            elif isStepEnd (start + 1) then
                Ok(SingleWild, start + 1)
            else
                parseIdentifier s start true |> Result.map (fun (name, i) -> NameStep name, i)
        else
            parseIdentifier s start true |> Result.map (fun (name, i) -> NameStep name, i)

    let private parseSteps (s: string) : Result<ExprStep list, string> =
        let rec loop (pos: int) (acc: ExprStep list) : Result<ExprStep list, string> =
            if pos >= s.Length then
                Ok(List.rev acc)
            else
                let pos2 = if s.[pos] = '/' then pos + 1 else pos

                if pos2 >= s.Length then
                    Error "expected step after /"
                else
                    match parseOneStep s pos2 with
                    | Error e -> Error e
                    | Ok(step, next) -> loop next (step :: acc)

        if String.IsNullOrEmpty s then
            Ok []
        else
            loop 0 []

    let private parseBase (s: string) : Result<ExprBase * string, string> =
        if String.IsNullOrEmpty s then
            Error "empty expression"
        elif s.[0] = '/' then
            Ok(WorkspaceRoot, s.Substring 1)
        elif s.[0] = '^' then
            Ok(FileRoot, s.Substring 1)
        elif s.[0] = '.' then
            Ok(FileDir, s.Substring 1)
        elif s.[0] = '@' then
            let colon = s.IndexOf(':', 1)

            if colon < 0 then
                Error "expected ':' after workspace label"
            else
                let label = s.Substring(1, colon - 1).Trim()

                if label.Length = 0 then
                    Error "workspace label required"
                elif label |> Seq.exists (fun c -> not (isTagIdentChar c)) then
                    Error "invalid workspace label"
                else
                    Ok(NamedWorkspace label, s.Substring(colon + 1))
        else
            Error "expected path base"

    let format (expr: PathExpr) : string =
        let formatBase =
            function
            | WorkspaceRoot -> "/"
            | FileRoot -> "^"
            | FileDir -> "."
            | NamedWorkspace label -> "@" + label + ":"

        let formatStep =
            function
            | NameStep name when name.Contains(' ') || name.Contains('/') ->
                "\"" + name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""
            | NameStep name -> name
            | TagStep tag -> "#" + tag
            | SingleWild -> "*"
            | MultiWild -> "**"

        let joinSteps (pathBase: ExprBase) (steps: ExprStep list) =
            match steps with
            | [] -> ""
            | step :: rest ->
                let firstSep =
                    match pathBase with
                    | NamedWorkspace _ -> ""
                    | _ -> "/"

                let first = firstSep + formatStep step

                rest
                |> List.fold (fun acc s -> acc + "/" + formatStep s) first

        match expr with
        | BaseOnly pathBase -> formatBase pathBase
        | Path(pathBase, steps) -> formatBase pathBase + joinSteps pathBase steps

    let parse (input: string) : Result<PathExpr, string> =
        if String.IsNullOrWhiteSpace input then
            Error "empty expression"
        elif input.Length > maxInputLength then
            Error "expression too long"
        else
            let s = normalizeInput input

            match parseBase s with
            | Error e -> Error e
            | Ok(pathBase, rest) ->
                match parseSteps rest with
                | Error e -> Error e
                | Ok steps ->
                    if List.isEmpty steps then
                        Ok(BaseOnly pathBase)
                    else
                        Ok(Path(pathBase, steps))

    let private ownerChildren (parentId: NodeId) (graph: Graph) : Node list =
        graph.nodes.[parentId].children
        |> List.choose (fun child ->
            if child.ref = Ownership.Owner then
                graph.nodes |> Map.tryFind child.id
            else
                None)

    let private segmentName (node: Node) : string option =
        match Filename.tryValue node.name with
        | Some n when not (String.IsNullOrEmpty n) -> Some n
        | _ ->
            if String.IsNullOrEmpty node.text then
                None
            else
                Some node.text

    let private globMatch (pattern: string) (text: string) : bool =
        let rec loop pi ti =
            if pi >= pattern.Length then
                ti >= text.Length
            elif ti >= text.Length then
                pattern.[pi] = '*' && loop (pi + 1) ti
            else
                let pc = Char.ToLowerInvariant pattern.[pi]
                let tc = Char.ToLowerInvariant text.[ti]

                if pc = '*' then
                    loop (pi + 1) ti
                    || (ti < text.Length && loop pi (ti + 1))
                elif pc = '?' then
                    loop (pi + 1) (ti + 1)
                elif pc = tc then
                    loop (pi + 1) (ti + 1)
                else
                    false

        loop 0 0

    let private expandOwnerDescendants (nodes: Node list) (graph: Graph) : Node list =
        let queue = Queue<Node>()
        let seen = HashSet<NodeId>()
        let result = ResizeArray<Node>()

        for n in nodes do
            queue.Enqueue n

        while queue.Count > 0 do
            let n = queue.Dequeue()

            if seen.Add n.id then
                result.Add n |> ignore

                for child in ownerChildren n.id graph do
                    queue.Enqueue child

        result |> Seq.toList

    let private resolveBase (ctx: RefContext) (pathBase: ExprBase) : NodeId option =
        match pathBase with
        | WorkspaceRoot -> ctx.workspaceRoot
        | FileRoot -> ctx.fileRoot
        | FileDir -> ctx.fileDir
        | NamedWorkspace label ->
            ctx.namedWorkspaces
            |> Map.tryFind (label.ToLowerInvariant())

    let private applyStep (graph: Graph) (step: ExprStep) (current: Node list) : Node list =
        match step with
        | MultiWild -> expandOwnerDescendants current graph
        | SingleWild ->
            current
            |> List.collect (fun n -> ownerChildren n.id graph)
        | NameStep pattern ->
            current
            |> List.collect (fun n -> ownerChildren n.id graph)
            |> List.filter (fun child ->
                segmentName child
                |> Option.map (globMatch pattern)
                |> Option.defaultValue false)
        | TagStep tag ->
            current
            |> List.collect (fun n -> ownerChildren n.id graph)
            |> List.filter (fun child -> CssClass.contains tag child.cssClasses)

    let match_ (ctx: RefContext) (graph: Graph) (expr: PathExpr) : Node list =
        let pathBase, steps =
            match expr with
            | BaseOnly b -> b, []
            | Path(b, steps) -> b, steps

        match resolveBase ctx pathBase with
        | None -> []
        | Some rootId ->
            let start =
                graph.nodes
                |> Map.tryFind rootId
                |> Option.map List.singleton
                |> Option.defaultValue []

            steps
            |> List.fold (fun nodes step -> applyStep graph step nodes) start
