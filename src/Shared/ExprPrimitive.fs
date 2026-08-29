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
        | _ -> ExprEval.empty

    let private requireIndex eval bound input =
        match bound with
        | ExprBoundSlot.IntOrStar n -> eval n input
        | _ -> ExprEval.empty

    let private requireQuoted eval bound input =
        match bound with
        | ExprBoundSlot.QuotedText text -> eval text input
        | _ -> ExprEval.empty

    let private rootRow graph =
        row [ "root" ] None (fun _ _ ->
            Map.tryFind graph.root graph.nodes
            |> Option.map ExprAnswer.Node
            |> ExprEval.ofOption)

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
            [ "subsection"; "#" ]
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

    let private reRow graph =
        row
            [ "re" ]
            (Some ExprSlotKind.QuotedText)
            (requireQuoted (ExprWalk.re graph))

    let private reiRow graph =
        row
            [ "rei" ]
            (Some ExprSlotKind.QuotedText)
            (requireQuoted (ExprWalk.rei graph))

    let private namedRow graph =
        row
            [ "named" ]
            (Some ExprSlotKind.QuotedText)
            (requireQuoted (ExprWalk.named graph))

    let private wsRow graph =
        row [ "ws" ] None (fun _ -> ExprWalk.ws graph)

    let private dirRow graph =
        row [ "dir" ] None (fun _ -> ExprWalk.dir graph)

    let private fileRow graph =
        row [ "file" ] None (fun _ -> ExprWalk.file graph)

    let private normalRow graph =
        row [ "normal" ] None (fun _ -> ExprWalk.normal graph)

    let private sectionRow graph =
        row [ "section" ] None (fun _ -> ExprWalk.section graph)

    let private classRow graph =
        row
            [ "class" ]
            (Some ExprSlotKind.QuotedText)
            (requireQuoted (ExprWalk.classMember graph))

    let private textRow graph : ExprCatalogRow =
        { spellings = [ "text" ]
          slot = None
          signature =
            { input = ExprAnswerType.Node
              output = ExprAnswerType.Text }
          evaluate =
            fun _ input ->
                match ExprWalk.tryGraphNode graph input with
                | Some node -> ExprEval.singleton (ExprAnswer.Text node.text)
                | None -> ExprEval.empty }

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
        |> ExprCatalog.register (reRow graph)
        |> ExprCatalog.register (reiRow graph)
        |> ExprCatalog.register (namedRow graph)
        |> ExprCatalog.register (wsRow graph)
        |> ExprCatalog.register (dirRow graph)
        |> ExprCatalog.register (fileRow graph)
        |> ExprCatalog.register (normalRow graph)
        |> ExprCatalog.register (sectionRow graph)
        |> ExprCatalog.register (classRow graph)
        |> ExprCatalog.register (textRow graph)
