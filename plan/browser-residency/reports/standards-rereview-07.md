# Standards re-review — origin/staging...HEAD

Range: `git diff origin/staging...HEAD`. Issue [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md).

## (a) Documented-standard violations

1. **Bare number (hard).** [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md): never refer by only the id or number; always include the name.
- [spec-review-07.md](spec-review-07.md) writes "Ticket 07 asked" and omits the issue name.
- [independent-review-07.md](independent-review-07.md) writes "item 2.4" and omits the list-item name bootstrapGraph beside old. The same report writes "Ticket 07 Status stays `coded`."

## (b) Smells (judgement)

1. **Feature Envy.** [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md): check GraphQuery before a custom Shared walk; `enclosing` walks the owner chain. [ResidentProjection.fs](src/Shared/ResidentProjection.fs) `ownerAncestorIds` reads `graph.ownerParentByChild`:

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

2. **Duplicated Code.** `visibleClosureGraph` repeats the `installWantAnswer` node-map fold and `Graph.fromNodes` merge:

```
let edges, nodes = visibleClosureWantAnswer savedZoom graph
let nodeMap =
    nodes
    |> List.fold (fun acc node -> Map.add node.id node acc) Map.empty
Graph.fromNodes graph.root nodeMap edges
```
