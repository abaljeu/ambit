namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module ExprCompile =
    let unknownWord (word: string) = $"unknown word '{word}'"

    let private invoke catalog spelling bound =
        match ExprCatalog.lookup spelling catalog with
        | None -> Error(unknownWord spelling)
        | Some found ->
            Ok(fun input -> ExprCatalog.invoke bound found input)

    let private compileStep catalog step =
        match step with
        | ClusterStep.Root ->
            invoke catalog "root" ExprBoundSlot.NoArgument
        | ClusterStep.Structural name ->
            invoke catalog "/" (ExprBoundSlot.NameGlob name)
        | ClusterStep.Content name ->
            invoke catalog "#" (ExprBoundSlot.NameGlob name)
        | ClusterStep.StructuralUp ->
            invoke catalog "^" ExprBoundSlot.NoArgument
        | ClusterStep.DirectoryUp ->
            invoke catalog "." ExprBoundSlot.NoArgument
        | ClusterStep.Tree ->
            invoke catalog "**" ExprBoundSlot.NoArgument
        | ClusterStep.ChildAt n ->
            invoke catalog ":" (ExprBoundSlot.IntOrStar n)
        | ClusterStep.SiblingAt n ->
            invoke catalog "!" (ExprBoundSlot.IntOrStar n)

    let private bindNext compileOne acc item =
        acc
        |> Result.bind (fun pred ->
            compileOne item |> Result.map (ExprEval.bind pred))

    let private compileCluster catalog steps =
        match steps with
        | [] -> Error "empty cluster"
        | head :: tail ->
            List.fold
                (bindNext (compileStep catalog))
                (compileStep catalog head)
                tail

    let private boundOf slot literal =
        match slot, literal with
        | None, None -> Ok ExprBoundSlot.NoArgument
        | None, Some _ -> Error "unexpected literal"
        | Some ExprSlotKind.QuotedText, Some text ->
            Ok(ExprBoundSlot.QuotedText text)
        | Some ExprSlotKind.NameGlob, Some name ->
            Ok(ExprBoundSlot.NameGlob name)
        | Some _, None -> Error ExprParse.missingArgument
        | Some ExprSlotKind.Int, Some raw ->
            match Int32.TryParse raw with
            | true, n -> Ok(ExprBoundSlot.Int n)
            | _ -> Error ExprParse.numberOnlyOperand
        | Some ExprSlotKind.IntOrStar, Some _ ->
            Error ExprParse.missingArgument

    let private isReserved word =
        word = "AND"
        || word = "OR"
        || word = "NOT"
        || word = "OUTER"
        || word = "IF"
        || word = "IS"

    let private compileWord catalog word literal =
        if isReserved word then
            Error(unknownWord word)
        else
            match ExprCatalog.lookup word catalog with
            | None -> Error(unknownWord word)
            | Some found ->
                boundOf found.slot literal
                |> Result.map (fun bound input ->
                    ExprCatalog.invoke bound found input)

    let private compileTerm catalog term =
        match term with
        | ExprTerm.Word(word, literal) -> compileWord catalog word literal
        | ExprTerm.Cluster(steps, _) -> compileCluster catalog steps
        | ExprTerm.Text text ->
            Ok(fun _ -> ExprEval.singleton (ExprAnswer.Text text))

    let rec private compileExpr graph catalog expr =
        match expr with
        | Expr.Term t -> compileTerm catalog t
        | Expr.Pipe [] -> Error "empty expression"
        | Expr.Pipe (head :: tail) ->
            List.fold
                (bindNext (compileExpr graph catalog))
                (compileExpr graph catalog head)
                tail
        | Expr.Not inner ->
            compileExpr graph catalog inner
            |> Result.map ExprEval.notEval
        | Expr.If inner ->
            compileExpr graph catalog inner
            |> Result.map ExprEval.ifEval
        | Expr.Outer inner ->
            compileExpr graph catalog inner
            |> Result.map (ExprWalk.outerAnswers graph)
        | Expr.And (left, right) ->
            compileExpr graph catalog left
            |> Result.bind (fun lpred ->
                compileExpr graph catalog right
                |> Result.map (ExprEval.andEval lpred))
        | Expr.Or (left, right) ->
            compileExpr graph catalog left
            |> Result.bind (fun lpred ->
                compileExpr graph catalog right
                |> Result.map (ExprEval.orEval lpred))
        | Expr.Is (left, right) ->
            compileExpr graph catalog left
            |> Result.bind (fun lpred ->
                compileExpr graph catalog right
                |> Result.map (ExprEval.isEval lpred))

    /// Types flow left to right: each term is offered the type of the Answer reaching
    /// it and reports the type it yields. A `Same` row takes whichever type arrives.
    let private applySig (input: ExprAnswerType) (signature: ExprSignature) =
        match signature with
        | ExprSignature.Fixed(expected, output) ->
            if expected = input then Ok output else Error "type error"
        | ExprSignature.Same -> Ok input

    let private lookupSig catalog spelling =
        match ExprCatalog.lookup spelling catalog with
        | None -> Error(unknownWord spelling)
        | Some row -> Ok row.signature

    let private stepSig catalog step =
        match step with
        | ClusterStep.Root -> lookupSig catalog "root"
        | ClusterStep.Structural _ -> lookupSig catalog "/"
        | ClusterStep.Content _ -> lookupSig catalog "#"
        | ClusterStep.StructuralUp -> lookupSig catalog "^"
        | ClusterStep.DirectoryUp -> lookupSig catalog "."
        | ClusterStep.Tree -> lookupSig catalog "**"
        | ClusterStep.ChildAt _ -> lookupSig catalog ":"
        | ClusterStep.SiblingAt _ -> lookupSig catalog "!"

    let private typeList infer input items =
        List.fold
            (fun acc item -> acc |> Result.bind (fun left -> infer left item))
            (Ok input)
            items

    let private clusterType catalog input steps =
        match steps with
        | [] -> Error "empty cluster"
        | _ ->
            typeList
                (fun left step -> stepSig catalog step |> Result.bind (applySig left))
                input
                steps

    let private termType catalog input term =
        match term with
        | ExprTerm.Word(word, _) when isReserved word ->
            Error(unknownWord word)
        | ExprTerm.Word(word, _) ->
            lookupSig catalog word |> Result.bind (applySig input)
        | ExprTerm.Cluster(steps, _) -> clusterType catalog input steps
        | ExprTerm.Text _ -> Ok ExprAnswerType.Text

    let rec private inferExpr catalog input expr =
        match expr with
        | Expr.Term t -> termType catalog input t
        | Expr.Pipe [] -> Error "empty expression"
        | Expr.Pipe items -> typeList (inferExpr catalog) input items
        | Expr.Not inner
        | Expr.If inner ->
            inferExpr catalog input inner |> Result.map (fun _ -> input)
        | Expr.Outer inner ->
            if input = ExprAnswerType.Node then
                inferExpr catalog ExprAnswerType.Node inner
                |> Result.map (fun _ -> ExprAnswerType.Node)
            else
                Error "type error"
        | Expr.And (left, right)
        | Expr.Or (left, right)
        | Expr.Is (left, right) ->
            inferExpr catalog input left
            |> Result.bind (fun lout ->
                inferExpr catalog input right
                |> Result.bind (fun rout ->
                    if lout = rout then Ok lout else Error "type error"))

    /// Every consumer applies the Expression to a Node Answer (spec chapter 8).
    let inferType catalog source =
        ExprParse.parseExpr source
        |> Result.bind (inferExpr catalog ExprAnswerType.Node)

    let compile graph catalog source =
        ExprParse.parseExpr source
        |> Result.bind (compileExpr graph catalog)

    type Outcome =
        | Hits of ExprAnswerType * ExprAnswer list
        | ParseFailed of string
        | TypeFailed of string

    let evalOutcome graph input source =
        let catalog = ExprPrimitive.catalog graph
        match inferType catalog source with
        | Error e when e = "type error" -> TypeFailed e
        | Error e -> ParseFailed e
        | Ok output ->
            match compile graph catalog source with
            | Error e -> ParseFailed e
            | Ok pred -> Hits(output, ExprEval.toList (pred input))

    let eval graph input source =
        let catalog = ExprPrimitive.catalog graph
        compile graph catalog source
        |> Result.map (fun pred -> ExprEval.toList (pred input))
