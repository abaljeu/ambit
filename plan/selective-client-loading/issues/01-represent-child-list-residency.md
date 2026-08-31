# Represent authoritative child-list residency

Type: grilling
Status: resolved

## Question

What client graph model should represent direct-child residency so an unloaded node cannot be mistaken for a loaded leaf, “loaded” means an authoritative, current, complete direct child list, transient loading remains outside graph data, and the current `Node`, `Graph`, and `DocumentState` invariants remain coherent?

## Answer

- `Node.children` is `Unknown | Loaded of ChildNode list`. `Loaded []` is a loaded leaf; `Unknown` makes no claim about direct children. A `Loaded` list is authoritative, current, and complete across both `Owner` and `Ref` edges.
- Request progress, failure, and retry state remain outside graph data. `DocumentState` remains source freshness and is independent of residency.
- `Node.owner` is `Unknown | Known of NodeId`; unknown owner identity never defaults to ROOT. The canonical root retains its explicit self-owner.
- Applying a `Loaded` list repairs each listed `Owner` child's owner to `Known parentId`. `ownerParentByChild` contains only known owner relationships, while `parentByChild` contains only occurrences exposed by `Loaded` lists. An absent index entry means unknown, not ROOT or leaf.
- Node presence does not imply that its owner node or owner list is resident. An `Unknown` child list contributes no edges or indexes. At one coherent graph revision, loaded Owner edges, known owner values, and derived owner indexes agree with the existing single-owner invariant.
- The required node/header closure for targets named by loaded lists is decided by ticket 02.
