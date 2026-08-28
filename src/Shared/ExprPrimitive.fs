namespace Gambol.Shared

[<RequireQualifiedAccess>]
module ExprPrimitive =
    let private nodeSig =
        { input = ExprAnswerType.Node
          output = ExprAnswerType.Node }

    let private row spellings slot evaluate : ExprCatalogRow =
        { spellings = spellings
          slot = slot
          signature = nodeSig
          evaluate = evaluate }

    let private requireName eval bound input =
        match bound with
        | ExprBoundSlot.NameGlob glob -> eval glob input
        | _ -> []

    let private requireIndex eval bound input =
        match bound with
        | ExprBoundSlot.IntOrStar n -> eval n input
        | _ -> []

    let private requireQuoted eval bound input =
        match bound with
        | ExprBoundSlot.QuotedText text -> eval text input
        | _ -> []

    let private rootRow graph =
        row [ "root" ] None (fun _ _ ->
            Map.tryFind graph.root graph.nodes
            |> Option.map ExprAnswer.Node
            |> Option.toList)

    let private structuralRow graph =
        row
            [ "/" ]
            (Some ExprSlotKind.NameGlob)
            (requireName (ExprWalk.structuralSearch graph))

    let private treeRow graph =
        row [ "tree"; "**" ] None (fun _ -> ExprWalk.treeAnswers graph)

    let private structuralUpRow graph =
        row [ "^" ] None (fun _ -> ExprWalk.structuralUp graph)

    let private directoryUpRow graph =
        row [ "." ] None (fun _ -> ExprWalk.directoryUp graph)

    let private wsrootRow graph =
        row [ "wsroot" ] None (fun _ -> ExprWalk.workspaceUp graph)

    let private childAtRow graph =
        row
            [ ":" ]
            (Some ExprSlotKind.IntOrStar)
            (requireIndex (ExprWalk.childAt graph))

    let private siblingAtRow graph =
        row
            [ "!" ]
            (Some ExprSlotKind.IntOrStar)
            (requireIndex (ExprWalk.siblingAt graph))

    let private contentRow graph =
        row
            [ "#" ]
            (Some ExprSlotKind.NameGlob)
            (requireName (ExprWalk.contentSearch graph))

    let private childRow graph =
        row [ "child" ] None (fun _ -> ExprWalk.childAt graph None)

    let private descendantRow graph =
        row [ "descendant" ] None (fun _ -> ExprWalk.descendantAnswers graph)

    let private containingRow graph =
        row
            [ "containing" ]
            (Some ExprSlotKind.QuotedText)
            (requireQuoted (ExprWalk.containing graph))

    let catalog (graph: Graph) : ExprCatalog.T =
        ExprCatalog.empty
        |> ExprCatalog.register (rootRow graph)
        |> ExprCatalog.register (structuralRow graph)
        |> ExprCatalog.register (treeRow graph)
        |> ExprCatalog.register (structuralUpRow graph)
        |> ExprCatalog.register (directoryUpRow graph)
        |> ExprCatalog.register (wsrootRow graph)
        |> ExprCatalog.register (childAtRow graph)
        |> ExprCatalog.register (siblingAtRow graph)
        |> ExprCatalog.register (contentRow graph)
        |> ExprCatalog.register (childRow graph)
        |> ExprCatalog.register (descendantRow graph)
        |> ExprCatalog.register (containingRow graph)
