namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module ExprRun =
    type Plan =
        { ops: Op list
          unfold: bool }

    type Line =
        | Ignore
        | Apply of Plan

    /// Run materialises at most this many Answers. Search paging is separate.
    let maxMaterialisedAnswers = 50

    let private isSignedInteger (seg: string) =
        seg.Length > 0
        && match seg.[0] with
           | c when Char.IsDigit c -> true
           | '+' | '-' -> seg.Length > 1 && Char.IsDigit seg.[1]
           | _ -> false

    let private isNameToken (seg: string) =
        seg.Length > 0
        && seg.[0] <> '.'
        && seg <> "AND"
        && seg <> "OR"
        && seg <> "NOT"
        && not (isSignedInteger seg)
        && not (
            seg
            |> Seq.exists (fun c ->
                c = '/'
                || c = '^'
                || c = '#'
                || c = ':'
                || c = '!'
                || c = '*'
                || Char.IsWhiteSpace c))

    let private classify (line: string) : (string option * string) option =
        let trimmed = line.Trim()
        if trimmed.StartsWith("=") then
            Some(None, trimmed.Substring(1).Trim())
        else
            match trimmed.IndexOf('=') with
            | -1 -> None
            | i ->
                let left = trimmed.Substring(0, i).Trim()
                let right = trimmed.Substring(i + 1).Trim()
                if isNameToken left then Some(Some left, right) else None

    let private replaceChildren (graph: Graph) (parentId: NodeId) kids =
        Op.Replace(parentId, graph.nodes.[parentId].children, kids)

    let private blueletterChild (graph: Graph) (focusId: NodeId) (text: string) : Plan =
        let id = NodeId.New()
        let classes = CssClass.ofList [ "blueletter" ]
        { ops =
            [ Op.NewNode(id, text)
              Op.SetClasses(id, CssClass.empty, classes)
              replaceChildren graph focusId [ ChildNode.owner id ] ]
          unfold = true }

    let private noMatches graph focusId =
        blueletterChild graph focusId "No matches found"

    let private renameOps (graph: Graph) (focusId: NodeId) (name: string) =
        let oldName =
            match Filename.tryValue graph.nodes.[focusId].name with
            | Some s -> s
            | None -> ""

        match Filename.create name with
        | Filename.Ok valid when valid = oldName -> []
        | Filename.Ok valid -> [ Op.SetName(focusId, oldName, valid) ]
        | _ -> []

    let private addAnswer graph (ops, kids) answer =
        match answer with
        | ExprAnswer.Node n when Map.containsKey n.id graph.nodes ->
            ops, ChildNode.reference n.id :: kids
        | ExprAnswer.Text text ->
            let id = NodeId.New()
            Op.NewNode(id, text) :: ops, ChildNode.owner id :: kids
        | _ -> ops, kids

    let private materialise graph focusId answers : Plan =
        let ops, kids = List.fold (addAnswer graph) ([], []) answers
        { ops =
            List.rev ops
            @ [ replaceChildren graph focusId (List.rev kids) ]
          unfold = true }

    let private planFromPred graph focusId pred input =
        match ExprEval.take maxMaterialisedAnswers (pred input) with
        | [], _ -> noMatches graph focusId
        | answers, _ -> materialise graph focusId answers

    let private planExpr graph focusId nameOpt source : Plan =
        let input = ExprAnswer.Node graph.nodes.[focusId]
        let catalog = ExprPrimitive.catalog graph
        let core =
            match ExprCompile.inferType catalog source with
            | Error e when e = "type error" -> blueletterChild graph focusId e
            | Error e -> blueletterChild graph focusId e
            | Ok _ ->
                match ExprCompile.compile catalog source with
                | Error e -> blueletterChild graph focusId e
                | Ok pred -> planFromPred graph focusId pred input

        match nameOpt with
        | None -> core
        | Some name ->
            { core with ops = renameOps graph focusId name @ core.ops }

    let run (focusId: NodeId) (graph: Graph) (line: string) : Line =
        match classify line with
        | None -> Ignore
        | Some(_, "") -> Apply(noMatches graph focusId)
        | Some(nameOpt, source) -> Apply(planExpr graph focusId nameOpt source)
