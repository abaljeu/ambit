namespace Gambol.Shared

open System

type FileSearchResult =
    { nodeId: NodeId
      text: string
      name: Filename
      pathLabel: string
      kind: NodeKind }

[<RequireQualifiedAccess>]
module ViewModelFileSearch =

    let private nodeToResult (graph: Graph) (node: Node) : FileSearchResult =
        let pathLabel =
            NodeDesktopPath.pathForNodeId graph node.id
            |> Option.orElseWith (fun () -> node.name |> Filename.tryValue)
            |> Option.defaultValue node.text

        { nodeId = node.id
          text = node.text
          name = node.name
          pathLabel = pathLabel
          kind = node.kind }

    let private containsNormalized (needle: string) (haystack: string) : bool =
        haystack.ToLowerInvariant().IndexOf(needle) >= 0

    let private nodeMatchesPart (normalizedPart: string) (node: Node) : bool =
        node.name
        |> Filename.tryValue
        |> Option.exists (containsNormalized normalizedPart)

    let private parseSearchParts (query: string) : string list option =
        match ViewModelSearch.parseSearchTerm query with
        | None -> None
        | Some term ->
            let parts =
                term.Split([| ' '; '\t'; '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
                |> Array.toList

            if parts.IsEmpty then None else Some parts

    type private PartFilter =
        { normalizedPart: string
          refIds: Set<NodeId> }

    let private buildPartFilter (ctx: RefContext) (part: string) (graph: Graph) : PartFilter =
        let refIds =
            match RefExpr.parse part with
            | Error _ -> Set.empty
            | Ok expr ->
                RefExpr.match_ ctx graph expr
                |> List.map (fun r -> r.nodeId)
                |> Set.ofList

        { normalizedPart = part.ToLowerInvariant()
          refIds = refIds }

    let private nodeMatchesPartFilter (pf: PartFilter) (nodeId: NodeId) (node: Node) : bool =
        nodeMatchesPart pf.normalizedPart node || Set.contains nodeId pf.refIds

    let private nodeMatchesAllParts (parts: PartFilter list) (nodeId: NodeId) (node: Node) : bool =
        parts |> List.forall (fun pf -> nodeMatchesPartFilter pf nodeId node)

    /// Peers in the persistence uniqueness set for the insert focus:
    /// same ownedArtifactsInUniquenessScope set as rename/move duplicate checks,
    /// scoped via resolveOwnedFileDirectoryInsert (do not descend into nested containers).
    let private resolveScopedArtifacts (focusNodeId: NodeId) (graph: Graph) : NodeId list option =
        match GraphQuery.resolveOwnedFileDirectoryInsert graph focusNodeId with
        | None -> None
        | Some(parentId, _) ->
            Some(GraphQuery.ownedArtifactsInUniquenessScope graph parentId None)

    type FileSearchCursor =
        private
            { graph: Graph
              filters: PartFilter list
              remaining: NodeId list }

    let startFind
        (query: string)
        (focusNodeId: NodeId)
        (graph: Graph)
        : FileSearchCursor option =
        match resolveScopedArtifacts focusNodeId graph with
        | None -> None
        | Some artifacts ->
            let filters =
                match parseSearchParts query with
                | None -> []
                | Some parts ->
                    let ctx = RefExpr.refContext focusNodeId graph
                    parts |> List.map (fun part -> buildPartFilter ctx part graph)

            Some
                { graph = graph
                  filters = filters
                  remaining = artifacts }

    let takeResults
        (count: int)
        (cursor: FileSearchCursor)
        : FileSearchResult list * FileSearchCursor option =
        let rec collect remaining resultsRev queue =
            if remaining <= 0 then
                List.rev resultsRev, Some { cursor with remaining = queue }
            else
                match queue with
                | [] -> List.rev resultsRev, None
                | nid :: rest ->
                    match Map.tryFind nid cursor.graph.nodes with
                    | Some node when GraphQuery.isArtifact node
                                     && nodeMatchesAllParts cursor.filters nid node ->
                        collect
                            (remaining - 1)
                            (nodeToResult cursor.graph node :: resultsRev)
                            rest
                    | _ -> collect remaining resultsRev rest

        collect count [] cursor.remaining

    let findInScope (query: string) (focusNodeId: NodeId) (graph: Graph) : FileSearchResult list =
        match startFind query focusNodeId graph with
        | None -> []
        | Some cursor -> takeResults Int32.MaxValue cursor |> fst

    let tryResultAtDisplayIndex
        (query: string)
        (focusNodeId: NodeId)
        (graph: Graph)
        (selectedIndex: int)
        : FileSearchResult option =
        let results = findInScope query focusNodeId graph

        if results.IsEmpty then
            None
        else
            let i =
                selectedIndex
                |> min (results.Length - 1)
                |> max 0

            List.tryItem i results
