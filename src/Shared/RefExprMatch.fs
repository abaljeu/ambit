namespace Gambol.Shared

open System
open System.Collections.Generic

[<RequireQualifiedAccess>]
module RefExprMatch =

    let private parentChildren (parentId: NodeId) (graph: Graph) : Node list =
        graph.nodes.[parentId].children
        |> List.choose (fun child -> graph.nodes |> Map.tryFind child.id)

    let private ownerChildren (parentId: NodeId) (graph: Graph) : Node list =
        graph.nodes.[parentId].children
        |> List.choose (fun child ->
            if child.ref = Ownership.Owner then
                graph.nodes |> Map.tryFind child.id
            else
                None)

    let private isStructuralBoundary (node: Node) : bool =
        match node.kind with
        | Special (Directory | Workspace) -> true
        | _ -> false

    let private nodeFileName (node: Node) : string option =
        match node.kind, Filename.tryValue node.name with
        | Special File, Some n when not (String.IsNullOrEmpty n) -> Some n
        | _ -> None

    let private nodeDirName (node: Node) : string option =
        match node.kind, Filename.tryValue node.name with
        | Special (Directory | Workspace), Some n when not (String.IsNullOrEmpty n) -> Some n
        | _ -> None

    let private globMatch (pattern: string) (text: string) : bool =
        let rec loop pi ti =
            if pi >= pattern.Length then
                ti >= text.Length
            elif ti >= text.Length then
                pattern.[pi] = '*' && loop (pi + 1) ti
            else
                let pc = System.Char.ToLowerInvariant pattern.[pi]
                let tc = System.Char.ToLowerInvariant text.[ti]

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

    let private pathStepMatches (step: ExprStep) (node: Node) : bool =
        match step with
        | DirStep pattern ->
            nodeDirName node
            |> Option.map (globMatch pattern)
            |> Option.defaultValue false
        | FileStep pattern ->
            nodeFileName node
            |> Option.map (globMatch pattern)
            |> Option.defaultValue false
        | TagStep _
        | MultiWild
        | IndexStep _
        | ChildStep _ -> false

    let private pathSearchFrom (graph: Graph) (step: ExprStep) (bases: Node list) : Node list =
        let results = ResizeArray<Node>()
        let seen = HashSet<NodeId>()

        let rec visit (node: Node) =
            if seen.Add node.id then
                if pathStepMatches step node then
                    results.Add node |> ignore

                for child in parentChildren node.id graph do
                    visit child

        for baseNode in bases do
            for child in parentChildren baseNode.id graph do
                visit child

            if pathStepMatches step baseNode then
                if seen.Add baseNode.id then
                    results.Add baseNode |> ignore

        results |> Seq.toList

    let private pathScopeDescendants (graph: Graph) (bases: Node list) : Node list =
        let results = ResizeArray<Node>()
        let seen = HashSet<NodeId>()

        let rec visit (node: Node) =
            if seen.Add node.id then
                results.Add node |> ignore

                if not (isStructuralBoundary node) then
                    for child in ownerChildren node.id graph do
                        visit child

        for baseNode in bases do
            visit baseNode

        results |> Seq.toList

    let private contentOwner (graph: Graph) (node: Node) : Node option =
        let rec go (id: NodeId) (seen: Set<NodeId>) =
            if Set.contains id seen || not (Map.containsKey id graph.nodes) then
                None
            else
                let n = graph.nodes.[id]

                match n.kind with
                | Special (Workspace | Directory | File) -> Some n
                | _ -> go n.owner (Set.add id seen)

        go node.id Set.empty

    let private tagSearchFrom (graph: Graph) (pattern: string) (bases: Node list) : Node list =
        let results = ResizeArray<Node>()
        let seen = HashSet<NodeId>()

        let rec visitTagged (node: Node) =
            if seen.Add node.id then
                match node.kind, Filename.tryValue node.name with
                | Normal, Some name when globMatch pattern name -> results.Add node |> ignore
                | _ -> ()

                for child in parentChildren node.id graph do
                    visitTagged child

        for baseNode in bases do
            match baseNode.kind with
            | Normal ->
                for child in parentChildren baseNode.id graph do
                    visitTagged child
            | _ ->
                match contentOwner graph baseNode with
                | None -> ()
                | Some owner ->
                    for child in parentChildren owner.id graph do
                        visitTagged child

        results |> Seq.toList

    let private nodeToSearchResult (node: Node) : NodeSearchResult =
        { nodeId = node.id
          text = node.text
          name = node.name }

    let private hasNodeName (node: Node) : bool =
        Filename.tryValue node.name |> Option.isSome

    let private walkOwnerChain (contextNode: NodeId) (graph: Graph) =
        let rec go
            id
            seen
            workspaceRoot
            currentDir
            structural
            tagged
            =
            if Set.contains id seen || not (Map.containsKey id graph.nodes) then
                workspaceRoot, currentDir, structural, tagged
            else
                let node = graph.nodes.[id]

                let workspaceRoot2 =
                    match workspaceRoot, node.kind with
                    | None, Special Workspace -> Some id
                    | _ -> workspaceRoot

                let currentDir2 =
                    match currentDir, node.kind with
                    | None, Special (Directory | Workspace) -> Some id
                    | _ -> currentDir

                let structural2 =
                    match structural, node.kind with
                    | None, Special (File | Directory | Workspace) -> Some id
                    | _ -> structural

                let tagged2 =
                    match tagged, node.kind with
                    | None, Normal when hasNodeName node -> Some id
                    | _ -> tagged

                if id = node.owner then
                    workspaceRoot2, currentDir2, structural2, tagged2
                else
                    go node.owner (Set.add id seen) workspaceRoot2 currentDir2 structural2 tagged2

        go contextNode Set.empty None None None None

    let refContext (contextNode: NodeId) (graph: Graph) : RefContext =
        let workspaceRoot, currentDir, structural, tagged = walkOwnerChain contextNode graph

        { contextNode = contextNode
          workspaceRoot = workspaceRoot
          currentDir = currentDir
          structural = structural
          tagged = tagged }

    let private resolveAnchor (ctx: RefContext) (anchor: ExprAnchor) : NodeId option =
        match anchor with
        | Context -> Some ctx.contextNode
        | WorkspaceRoot -> ctx.workspaceRoot
        | GlobalRoot -> Some Graph.rootId
        | CurrentDir -> ctx.currentDir
        | Structural -> ctx.structural
        | Tagged -> ctx.tagged

    let private siblingAtOffset (graph: Graph) (node: Node) (offset: int) : Node option =
        match Graph.tryFindParentAndIndex node.id graph with
        | None -> None
        | Some (parentId, index) ->
            parentChildren parentId graph
            |> List.tryItem (index + offset)

    let private indexStepFrom (graph: Graph) (offset: int option) (bases: Node list) : Node list =
        match offset with
        | None -> bases
        | Some n ->
            bases
            |> List.choose (fun node -> siblingAtOffset graph node n)

    let private childAtIndex (graph: Graph) (node: Node) (index: int) : Node option =
        parentChildren node.id graph
        |> List.tryItem index

    let private childStepFrom (graph: Graph) (index: int option) (bases: Node list) : Node list =
        match index with
        | None ->
            bases
            |> List.collect (fun node -> parentChildren node.id graph)
        | Some n ->
            bases
            |> List.choose (fun node -> childAtIndex graph node n)

    let private applyStep (graph: Graph) (step: ExprStep) (current: Node list) : Node list =
        match step with
        | MultiWild -> pathScopeDescendants graph current
        | TagStep pattern -> tagSearchFrom graph pattern current
        | IndexStep offset -> indexStepFrom graph offset current
        | ChildStep index -> childStepFrom graph index current
        | DirStep _
        | FileStep _ -> pathSearchFrom graph step current

    let match_ (ctx: RefContext) (graph: Graph) (expr: PathExpr) : NodeSearchResult list =
        let anchor, steps =
            match expr with
            | AnchorOnly a -> a, []
            | Path(a, steps) -> a, steps

        match resolveAnchor ctx anchor with
        | None -> []
        | Some rootId ->
            let start =
                graph.nodes
                |> Map.tryFind rootId
                |> Option.map List.singleton
                |> Option.defaultValue []

            steps
            |> List.fold (fun nodes step -> applyStep graph step nodes) start
            |> List.map nodeToSearchResult
