See also: [[doc/roadmap/graph-view.md]], [[plan/graph-view/project.md]]

Your constraints pin down a clean architecture: **tree layout is authoritative; backlinks are overlay; focus changes what “the tree” means.**

## What you’ve fixed

| Given | Implication |
|-------|-------------|
| Tree + child order fixed | Reingold–Tilford / radial — no force sim, no edge picking |
| Backlinks secondary | Don’t let them move nodes; route in margins / outer annulus |
| Radial from non-root | View = **subtree rooted at focus** (plus maybe 1 hop of context) |
| Backlinks cross subtree boundary | Target often **not in current render** → need a **multi-radial** model |

That last point is the insight: one radial isn’t enough unless you only ever focus on the vault root.

---

## Radial from a focus node

Treat each render as:

```text
Focus F
  → nodes = subtree(F) in tree order on concentric rings
  → tree edges = parent/child, drawn as radial spokes or annular segments
  → backlinks = only edges with at least one endpoint in the visible set
```

**Within the visible subtree:** route backlinks on an **outer annulus** (arcs outside the outermost ring). Tree stays the spine; backlinks orbit it. Same idea as margin lanes, but curved.

**Leaving the visible subtree:** the target isn’t placed — you don’t fake it at a random angle. You need an **external reference**, not a missing node drawn anyway.

---

## Three kinds of backlink (route each differently)

1. **Internal** — both ends in `subtree(F)` → annulus arc between ports on the two nodes.
2. **Outbound** — source visible, target outside → exit to a **portal** on the hull (grouped by *where* outside).
3. **Inbound** — source outside, target visible → enter from a portal (less common if view is descendant-only; matters if you include ancestors in the set).

Portals are small labeled chips on the outer ring: “↑ parent chain”, “→ sibling branch B”, “↗ project X”, not one stub per distant node when there are hundreds.

---

## “Cluster of radial tree renders”

That’s the right mental model. Not one graph — a **constellation of focus-centric views** linked by backlinks/portals.

### Pattern A: Focus + satellites (good default)

- **Primary radial:** `subtree(focus)`.
- **Satellite radials:** one small radial per **portal cluster** (same parent, same top ancestor, same tag, same manual cluster — pick one rule).
- **Inter-radial edges:** thin lines from node/port on main disc to center or rim of satellite.

User sees: “I’m here; these are my local children; these three regions elsewhere link in.”

### Pattern B: Mosaic / comparison

- Several radials at once (siblings, recent focuses, pinned nodes).
- Backlinks **between** radials when both endpoints are rendered in different tiles.
- Useful for “how do these three outlines relate?” not for navigation.

### Pattern C: Drill-through (minimal clutter)

- Single radial; outbound portals only.
- Click portal → **replace or stack** focus → new primary radial.
- Satellites appear only when comparing, not always.

For Ambit, **A for exploration, C for daily use** is a sensible split.

---

## Handling “back into subtree” vs “off visible tree”

Same geometry, different semantics:

| Target location | UI |
|-----------------|-----|
| Descendant of focus | Normal internal backlink arc |
| Ancestor of focus | Portal on inner ring or top of hull; optional tiny **ancestor radial** (depth-limited upward) |
| Other branch | Portal + satellite radial rooted at **lowest common ancestor** or at **link target** |
| Unknown / filtered | Portal count badge; expand on demand |

**Lowest common ancestor (LCA)** is useful for clustering: all outbound links whose target lies under the same child of LCA(focus, target) share one portal / one satellite.

---

## Layout pipeline (concrete)

```text
1. focus := user selection
2. primaryNodes := subtree(focus)   // preserve child order on each ring
3. layout primary radial (deterministic angles from sibling index)
4. classify backlinks touching primaryNodes
5. internal → assign annulus lanes (by target angle, monotonic radius)
6. external → bucket by cluster rule → portal on hull
7. for each bucket with count > 0 or user expanded:
     satellite radial(bucketRepresentative or LCA)
8. draw inter-radial connectors (secondary stroke, dashed)
```

Child order → **fixed angular slots** on each ring (no physics). Backlink lanes → **increment radius** on the annulus so arcs don’t overlap.

---

## Styling (your annotation idea)

Keep semantics on the model; renderer maps:

- **Tree edge** — thin, neutral, solid  
- **Internal backlink** — dashed / lower opacity, annulus  
- **Portal** — chip with count + cluster label  
- **Satellite** — scaled-down radial, same angular rules  
- **Inter-radial** — light curved connector, never through primary node boxes  

Node attrs drive color/shape; edge attrs drive dash/weight — router only sees ports and obstacle rings.

---

## Design choices worth deciding once

1. **Descendants-only vs include ancestors** when focus ≠ root — affects inbound backlinks.
2. **Portal clustering rule** — by LCA branch is usually the sweet spot.
3. **When to spawn a satellite** — always vs on hover/click on portal (C vs A).
4. **Angular allocation** — equal slices per sibling vs weighted by subtree size (large subtrees get more arc).

---

## Relation to Obsidian / Mermaid

- Not Obsidian Graph View (force hairball).
- Not Mermaid for the live UI (backlinks + portals are too custom).
- Optional: export **tree only** to DOT/Mermaid for docs; keep radial + annulus routing in-app.

---

**Bottom line:** radial tree per focus, backlinks as annulus overlay inside the disc, **portals + optional satellite radials** for anything outside `subtree(focus)`. That matches “secondary backlinks” and “sometimes off visible tree” without breaking the given hierarchy.

If you want to go one level more concrete next: define whether focus view is **descendants-only** or **ancestors + descendants**, and whether satellites root at **link target** or **LCA** — those two choices mostly determine how cluttered the constellation gets.