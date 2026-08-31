# Graph view

Category: PKM navigation
Status: Planned — design drafted; no implementation started
See also: [[plan/graph-view/graph-view-draft-proposal.md]], [[doc/current/workspace-graph.md]], [[on-demand-graph-residency]], [[plan/selective-client-loading/project.md]], [[doc/arch.md]]

An alternate navigation surface beside the outline SiteMap: a **focus-centric radial tree** of Owned children with **Ref edges** as a secondary overlay. Tree layout is authoritative; Ref edges do not move Nodes. Design detail lives in [[plan/graph-view/graph-view-draft-proposal.md]].

## What it gives you

- A radial layout rooted at the current **focus** Node: Owned descendants on concentric rings in sibling order (deterministic angles, no force simulation).
- **Internal** Ref edges (both ends in the visible subtree) routed on an outer annulus outside the outermost ring.
- **Outbound** Ref edges (target outside the subtree) grouped into **portals** on the hull — labeled chips such as parent chain, sibling branch, or project cluster — instead of one stub per distant Node.
- **Drill-through** navigation: click a portal or Node to change focus and replace the primary radial (Pattern C in the draft).
- Optional later: **satellite** radials per portal cluster for exploration (Pattern A) and side-by-side comparison tiles (Pattern B).
- Styling driven by Node and edge attributes: tree edges thin and neutral; Ref edges dashed at lower opacity; inter-radial connectors light and never through Node boxes.

## What it avoids for now

- Force-directed or physics layouts (not Obsidian Graph View).
- Mermaid or DOT for the live UI (export tree-only snapshots for docs is optional later).
- Ref edges that reposition Nodes or compete with the tree spine.
- Drawing off-subtree targets at arbitrary angles — external targets use portals, not fake placement.
- Full **mosaic** multi-radial comparison (Pattern B) until drill-through (Pattern C) is proven.
- Automatic satellite spawn on every portal (Pattern A) until hover/click expansion is chosen and tested.
- Replacing the outline SiteMap; graph view is an additional surface sharing the same Graph, selection, and zoom/focus model.

## Open decisions (lock before tickets)

Record answers in [[plan/graph-view/graph-view-draft-proposal.md]] or a follow-on spec slice.

1. **Focus closure** — descendants-only vs include ancestors when focus ≠ ROOT (affects inbound Ref routing).
2. **Portal clustering** — lowest common ancestor (LCA) branch is the draft default; confirm or replace.
3. **Satellite trigger** — always visible vs on hover/click on a portal (C vs A).
4. **Angular allocation** — equal sibling slices vs weighted by subtree size.

## Minimal state / API / ops

- **Focus** — same Node as outline zoom/selection anchor; changing focus recomputes `subtree(focus)` with preserved child order.
- **Visible set** — Owned descendants of focus (plus optional bounded ancestor context per decision 1).
- **Edge classes** — internal Ref (both ends visible), outbound Ref (source visible, target outside), inbound Ref (source outside, target visible).
- **Layout pipeline** — deterministic radial positions from sibling index; classify Ref edges touching the visible set; assign annulus lanes by target angle with monotonic radius; bucket external Ref edges into portals; optionally layout satellite radials per expanded bucket.
- **Client-only rendering** — pure layout over resident Graph + SiteMap fold state; no new server ops. Unloaded boundaries from [[plan/selective-client-loading/project.md]] surface as portal stubs or load affordances, not empty trees.
- **Interaction** — focus change, portal expand, optional pin/compare; reuse existing navigation commands where possible.

## Implementation steps

1. **Shared layout model** — types for radial positions, annulus lanes, portal buckets, and edge class; unit-test layout on small fixed graphs (no DOM).
2. **Pattern C slice** — single radial + outbound portals + focus drill-through in a new Client view module; wire focus to existing selection/zoom; prove internal Ref arcs and portal chips on a medium fixture graph.
3. **Inbound and ancestor context** — after decision 1, add inner-ring or hull portals for inbound Ref edges and optional shallow ancestor radial.
4. **Satellite radials** — after decision 3, spawn scaled-down radials for expanded portal clusters with inter-radial connectors (Pattern A).
5. **Mosaic comparison** — optional Pattern B tiles for pinned focuses; defer until A/C are stable.
6. **Scale integration** — align portal/load behavior with [[on-demand-graph-residency]] and selective loading so off-document targets never imply false emptiness.

## Tests

- Shared: sibling order → fixed angles; internal Ref lanes do not overlap at monotonic annulus radius; LCA bucketing groups outbound Ref edges as specified.
- Shared: empty subtree, single child, deep chain, and wide sibling fan preserve deterministic layout.
- Client: focus change replaces primary radial; portal click sets new focus; internal Ref arcs attach to visible endpoints only.
- Integration: with Unloaded child lists, hull shows load/portal affordance without treating Unknown as empty children.
