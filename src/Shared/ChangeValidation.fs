namespace Gambol.Shared

/// Validation and apply functions for Change operations with ownership semantics.
[<RequireQualifiedAccess>]
module ChangeValidation =
    let private validateOwnershipSemantics
        (graph: Graph)
        (childIdsScope: Set<NodeId> option)
        : Result<unit, string * NodeId> =
        let allChildren =
            graph.childMap
            |> Map.toList
            |> List.collect (fun (parentId, children) ->
                children |> List.map (fun child -> parentId, child))

        let allChildIds =
            allChildren |> List.map (fun (_, child) -> child.id) |> Set.ofList

        let ownerByChildId =
            allChildren
            |> List.choose (fun (parentId, child) ->
                match Node.childOwnership graph parentId child with
                | Ownership.Owner -> Some(child.id, parentId)
                | Ownership.Ref -> None)
            |> List.groupBy fst
            |> List.map (fun (childId, pairs) -> childId, (pairs |> List.map snd))
            |> Map.ofList

        let childIdsToCheck =
            match childIdsScope with
            | None -> allChildIds
            | Some ids -> Set.intersect ids allChildIds

        let locatedChildDetail (id: NodeId) : string =
            let text =
                match Map.tryFind id graph.nodes with
                | Some n when n.text.Length <= 80 -> n.text
                | Some n -> n.text.Substring(0, 80) + "..."
                | None -> ""
            let tail = NodeId.GuidTail8 id.Value
            $"text='{text}' id={tail}"

        // Prefer Owner parent from Loaded lists; else Node.owner (resident claim).
        let ownerParentOf (childId: NodeId) : NodeId option =
            match Map.tryFind childId ownerByChildId with
            | Some (parentId :: _) -> Some parentId
            | Some []
            | None ->
                Map.tryFind childId graph.nodes
                |> Option.map (fun n -> n.owner)

        // Proven missing only when a non-ROOT claimed owner is Loaded without
        // an Owner edge. ROOT (Create/appendChildren default) is incomplete under
        // selective load: the real Owner parent may be Unloaded elsewhere.
        let isProvenMissingOwner (childId: NodeId) : bool =
            match Map.tryFind childId ownerByChildId with
            | Some _ -> false
            | None ->
                match Map.tryFind childId graph.nodes with
                | None -> false
                | Some childNode when childNode.owner = graph.root -> false
                | Some childNode ->
                    match Map.tryFind childNode.owner graph.nodes with
                    | None -> false
                    | Some _ when not (GraphChildren.isLoaded graph childNode.owner) ->
                        false
                    | Some _ -> true

        let childIdsMissingOwner =
            childIdsToCheck
            |> Seq.filter isProvenMissingOwner
            |> Seq.toList

        if not childIdsMissingOwner.IsEmpty then
            let childId = List.head childIdsMissingOwner
            Error (
                $"invalid ownership semantics: missing owner occurrence [{locatedChildDetail childId}]",
                childId)
        else
            let childIdsWithMultipleOwners =
                childIdsToCheck
                |> Seq.filter (fun childId ->
                    match Map.tryFind childId ownerByChildId with
                    | None -> false
                    | Some owners -> owners.Length <> 1)
                |> Seq.toList

            if not childIdsWithMultipleOwners.IsEmpty then
                let childId = List.head childIdsWithMultipleOwners
                let owners = ownerByChildId.[childId]
                let ownersStr =
                    owners
                    |> List.map (fun id -> NodeId.GuidTail8 id.Value)
                    |> String.concat ","
                Error (
                    $"invalid ownership semantics: expected exactly one owner occurrence [{locatedChildDetail childId} owners={ownersStr}]",
                    childId)
            else
                // true = reaches root; false = cycle/broken; None = incomplete hop
                let rec reachesRootWithoutCycle
                    (currentId: NodeId)
                    (visited: Set<NodeId>)
                    : bool option =
                    if currentId = graph.root then
                        Some true
                    elif Set.contains currentId visited then
                        Some false
                    else
                        match Map.tryFind currentId graph.nodes with
                        | None -> None
                        | Some _ ->
                            match ownerParentOf currentId with
                            | None -> None
                            | Some parentId ->
                                match Map.tryFind parentId graph.nodes with
                                | None when parentId <> graph.root -> None
                                | _ ->
                                    reachesRootWithoutCycle
                                        parentId
                                        (Set.add currentId visited)

                let ownerChainIds (startId: NodeId) : NodeId list =
                    let rec loop currentId visited acc =
                        if Set.contains currentId visited then
                            List.rev (currentId :: acc)
                        else
                            let nextAcc = currentId :: acc
                            let nextVisited = Set.add currentId visited
                            if currentId = graph.root then
                                List.rev nextAcc
                            else
                                match ownerParentOf currentId with
                                | Some parentId ->
                                    loop parentId nextVisited nextAcc
                                | None -> List.rev nextAcc
                    loop startId Set.empty []

                let formatOwnerChain (ids: NodeId list) : string =
                    ids
                    |> List.map (fun id -> NodeId.GuidTail8 id.Value)
                    |> String.concat " -> "

                let brokenOwnerChainChild =
                    childIdsToCheck
                    |> Seq.tryFind (fun childId ->
                        match ownerParentOf childId with
                        | None -> false
                        | Some ownerParent ->
                            reachesRootWithoutCycle ownerParent Set.empty = Some false)

                match brokenOwnerChainChild with
                | Some childId ->
                    let chain = formatOwnerChain (ownerChainIds childId)
                    Error (
                        $"invalid ownership semantics: owner chain does not reach root [{locatedChildDetail childId} chain={chain}]",
                        childId)
                | None ->
                    match childIdsScope with
                    | Some _ -> Ok ()
                    | None ->
                        let invalidPlacementChild =
                            allChildren
                            |> Seq.tryPick (fun (parentId, child) ->
                                match
                                    Node.childOwnership graph parentId child,
                                    Map.tryFind child.id graph.nodes
                                with
                                | Ownership.Owner,
                                  Some { kind = Special (File | Directory) }
                                    when not (Graph.isSystemDirectoryNode child.id) ->
                                    if not (
                                        GraphQuery.containerOrDescendant graph parentId) then
                                        Some child.id
                                    else
                                        None
                                | _ -> None)

                        match invalidPlacementChild with
                        | Some childId ->
                            Error (
                                "invalid ownership semantics: File and Directory nodes must have a Workspace or Directory owner ancestor (not under a File)",
                                childId)
                        | None ->
                            match GraphQuery.tryFindArtifactNameDuplicate graph with
                            | Some dupId ->
                                let name =
                                    Map.tryFind dupId graph.nodes
                                    |> Option.bind (fun n -> Filename.tryValue n.name)
                                    |> Option.defaultValue "?"
                                Error (
                                    $"invalid ownership semantics: duplicate name '{name}' in artifact directory",
                                    dupId)
                            | None -> Ok ()

    let validateOwnershipLocated (graph: Graph) : Result<unit, string * NodeId> =
        validateOwnershipSemantics graph None

    let validateOwnership (graph: Graph) : Result<unit, string> =
        match validateOwnershipLocated graph with
        | Ok () -> Ok ()
        | Error (msg, _) -> Error msg

    let private opChangesGraphShape =
        function
        | Op.Replace _ | Op.NewNode _ | Op.NewSpecialNode _ -> true
        | Op.SetText _ | Op.SetClasses _ | Op.SetName _
        | Op.SetUpdateTime _ -> false

    let private invalidOwnedFileDirectoryPlacement
        (graph: Graph)
        (parentId: NodeId)
        (newChildren: ChildNode list)
        : bool
        =
        GraphQuery.invalidOwnedFileDirectoryPlacement graph parentId newChildren

    let private introducedChildren (oldChildren: ChildNode list) (newChildren: ChildNode list) =
        newChildren
        |> List.filter (fun nc ->
            oldChildren
            |> List.exists (fun oc -> oc.id = nc.id && oc.ref = nc.ref)
            |> not)

    let private removedChildren (oldChildren: ChildNode list) (newChildren: ChildNode list) =
        oldChildren
        |> List.filter (fun oc ->
            newChildren
            |> List.exists (fun nc -> nc.id = oc.id && nc.ref = oc.ref)
            |> not)

    let private validateOwnershipForOps (graph: Graph) (ops: Op list) : Result<unit, string> =
        let shapeOps = ops |> List.filter opChangesGraphShape

        if List.isEmpty shapeOps then
            Ok ()
        else
            let childIds =
                shapeOps
                |> List.collect (Op.involvedNodeIds graph)
                |> Set.ofList

            match validateOwnershipSemantics graph (Some childIds) with
            | Error (msg, _) -> Error msg
            | Ok () ->
                shapeOps
                |> List.tryPick (fun op ->
                    match op with
                    | Op.Replace(parentId, oldChildren, newChildren) ->
                        let introduced = introducedChildren oldChildren newChildren
                        if invalidOwnedFileDirectoryPlacement graph parentId introduced then
                            Some
                                "invalid ownership semantics: File and Directory nodes must have a Workspace or Directory owner ancestor (not under a File)"
                        elif
                            introduced
                            |> List.exists (fun c ->
                                Node.childOwnership graph parentId c = Ownership.Owner)
                            && GraphQuery.artifactNameConflict graph parentId introduced
                        then
                            let name =
                                introduced
                                |> List.tryPick (fun c ->
                                    Map.tryFind c.id graph.nodes
                                    |> Option.bind (fun n -> Filename.tryValue n.name))
                                |> Option.defaultValue "?"
                            Some
                                $"invalid ownership semantics: duplicate name '{name}' in artifact directory"
                        else
                            None
                    | _ -> None)
                |> Option.map Error
                |> Option.defaultValue (Ok ())

    let internal applyOpsAllowing guardUnparsed changedVersion ops state =
        match Op.applyAllAllowing guardUnparsed changedVersion ops state with
        | ApplyResult.Invalid _ as err -> err
        | ApplyResult.Unchanged s -> ApplyResult.Unchanged s
        | ApplyResult.Changed s ->
            match validateOwnershipForOps s.graph ops with
            | Error msg -> ApplyResult.Invalid(state, msg)
            | Ok () -> ApplyResult.Changed s

    let applyOpsTrusted (ops: Op list) (state: State) : ApplyResult =
        Op.applyAll ops state

    let applyOps (ops: Op list) (state: State) : ApplyResult =
        applyOpsAllowing true (Some EventId.zero) ops state

