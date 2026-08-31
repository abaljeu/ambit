# Replace amendment spec — report

## Deliverable

Created [[../details/replace-amendment.md]] — complete specification for full-list Replace, three-way resolve, minimal acceptBoth, Server amend, producer rule, undo, hard Reject guards, three worked examples, migration open decision, and non-goals.

## Sections covered

1. **Replace shape** — `Replace(parentId, oldList, newList)` as Actor contract; parallel to `SetClasses`; span form marked behavior to beat with [[as-implemented-facts.md]] cross-ref.
2. **Three-way resolve** — `current`, `intent`, `context`, `target`; fast path; success when `target ≠ newList`; `externalChanges` rule; mermaid flowchart.
3. **diff extraction** — bag walk on full `ChildNode` value equality.
4. **acceptBoth** — order invariants (context order, intent add order, honored removes, no id cancel); spine-from-current deterministic construction; issue 10 interleaving polish only.
5. **Server amend** — rewrite to `Replace(parentId, current, target)`; mirrors ticket 03; [[merge-invariant.md]] order.
6. **Producer rule** — full lists, one Replace per parent; [[plan/relaxed-concurrency/replace-span-cas-feasibility.md]] as migration scope.
7. **Undo** — `Replace(P, newList, oldList)`; amended undo uses applied pair.
8. **Hard Reject** — placement, ownership, missing nodes, auth/malformed unchanged.
9. **Worked examples** — index staleness, same-slot collision (StateEndpointTests pattern), disjoint append.
10. **Migration** — wire format open decision documented.
11. **Non-goals** — issue 10, delete-against-edit, id-anchored Replace, amb-conflict on lists.

## Files touched

| File | Change |
| --- | --- |
| [[../details/replace-amendment.md]] | **Created** — primary spec |
| [[../issues/05-child-list-accept-both.md]] | See also link; What to build mentions full-list shape and three-way resolve |
| [[../details/conflict-resolution.md]] | Kind 3 points to replace-amendment; Default line is full-list Replace |
| [[../details/merge-invariant.md]] | What this does not decide: link instead of "approximation algorithm is later work" |
| [[../details/open-questions.md]] | Accepted child lists bullet updated |
| [[../project.md]] | Details list entry added |
| [[../details/vocabulary.md]] | No edit — Replace not mentioned |

## Not done

- No code implementation.
- No commit.
- No index regeneration (stage unchanged).

## Update — order invariants (issue 05)

[[../details/replace-amendment.md]] §4 now locks five order invariants and replaces the old `oldList` + `A*` concatenation rule with a **spine-from-current** construction: drop `R*`, then insert `intent.add` occurrences anchor-relative per `newList`. This guarantees the merge does not randomize context (`current`) or intent order. Issue 10 scope narrowed to interleaving polish among valid orderings (e.g. same-slot `[cA,cB]` vs `[cB,cA]`). Worked examples (a)–(c) updated; [[../details/conflict-resolution.md]] Kind 3 and [[../issues/10-child-list-approximation-polish.md]] aligned.
