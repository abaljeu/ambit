# Standards review 07

Hard documented-standard violations: none. Mechanical scan bindings in [ApiResponseSerialization.fs](src/Shared/ApiResponseSerialization.fs), [ApiResponses.fs](src/Shared/ApiResponses.fs), [ResidentProjection.fs](src/Shared/ResidentProjection.fs), and [Want.fs](src/Shared/Want.fs) stay under [fsharp-source.md](.agents/rules/fsharp-source.md) (40 lines/function, 800 lines/file, 100 chars/line).

## 1. Feature Envy (judgement)

[fsharp-source.md](.agents/rules/fsharp-source.md) says: before a custom graph walk in Shared, check `GraphQuery` helpers; `enclosing` walks up the owner chain. [ResidentProjection.fs](src/Shared/ResidentProjection.fs) `ownerAncestorIds` walks `graph.ownerParentByChild` itself:

```
let private ownerAncestorIds (graph: Graph) (nodeId: NodeId) : NodeId list =
    let rec walk acc current visited =
        if Set.contains current visited then
            acc
        else
            match Map.tryFind current graph.ownerParentByChild with
            | None -> acc
            | Some parent ->
                walk
                    (parent :: acc)
                    parent
                    (Set.add current visited)
```

This is the same owner-chain walk as [GraphQuery.enclosing](src/Shared/GraphQuery.fs). Collect-ancestors belongs on GraphQuery.

## 2. Duplicated Code (judgement)

[ResidentProjection.fs](src/Shared/ResidentProjection.fs) `visibleClosureGraph` rebuilds a node Map and calls `Graph.fromNodes`. That is the same merge as `installWantAnswer`. The Shared test already installs `visibleClosureWantAnswer` through `installWantAnswer`.

```
let edges, nodes = visibleClosureWantAnswer savedZoom graph
let nodeMap =
    nodes
    |> List.fold (fun acc node -> Map.add node.id node acc) Map.empty
Graph.fromNodes graph.root nodeMap edges
```
