namespace Gambol.Shared

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
        | Some ExprSlotKind.IntOrStar, Some _ ->
            Error ExprParse.missingArgument

    let private isReserved word =
        word = "AND" || word = "OR" || word = "NOT"

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

    let private compose (left: ExprSignature) (right: ExprSignature) =
        if left.output = right.input then
            Ok
                { input = left.input
                  output = right.output }
        else
            Error "type error"

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

    let private composeList infer first rest =
        List.fold
            (fun acc item ->
                acc
                |> Result.bind (fun left ->
                    infer item |> Result.bind (compose left)))
            (infer first)
            rest

    let private clusterSig catalog steps =
        match steps with
        | [] -> Error "empty cluster"
        | head :: tail -> composeList (stepSig catalog) head tail

    let private termSig catalog term =
        match term with
        | ExprTerm.Word(word, _) when isReserved word ->
            Error(unknownWord word)
        | ExprTerm.Word(word, _) -> lookupSig catalog word
        | ExprTerm.Cluster(steps, _) -> clusterSig catalog steps

    let inferType catalog source =
        ExprParse.parseExpr source
        |> Result.bind (fun terms ->
            match terms with
            | [] -> Error "empty expression"
            | head :: tail -> composeList (termSig catalog) head tail)

    let compile catalog source =
        ExprParse.parseExpr source
        |> Result.bind (fun terms ->
            match terms with
            | [] -> Error "empty expression"
            | head :: tail ->
                List.fold
                    (bindNext (compileTerm catalog))
                    (compileTerm catalog head)
                    tail)

    type Outcome =
        | Hits of ExprAnswerType * ExprAnswer list
        | ParseFailed of string
        | TypeFailed of string

    let evalOutcome graph input source =
        let catalog = ExprPrimitive.catalog graph
        match inferType catalog source with
        | Error e when e = "type error" -> TypeFailed e
        | Error e -> ParseFailed e
        | Ok signature ->
            match compile catalog source with
            | Error e -> ParseFailed e
            | Ok pred -> Hits(signature.output, pred input)

    let eval graph input source =
        compile (ExprPrimitive.catalog graph) source
        |> Result.map (fun pred -> pred input)
