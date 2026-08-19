# Replace span compare-and-swap feasibility

## Bottom line

**Full-value span comparison is already enforced in `Graph.replace`** (`src/Shared/GraphMutate.fs:241-247`: compares `List.skip index >> List.take oldCount` against `oldChildren`; mismatch → `"old span does not match"`). Production edit paths overwhelmingly plan against the live graph and treat `oldChildren = []` as a zero-width insert (valid at any index, including non-zero). **Enforcing (or keeping) this check would not break normal client/server flows today**, and the suite already assumes it in places (`HistoryTests.fs:261-271`, reorder tests using live `oldChildren` in `ModelTests.fs:405-413`, `648-653`). The main **production gap** is `ViewModelJoinOps.removeCurrentOp`, which fabricates `ChildNode.owner` instead of reading the live edge at the removal index — that would fail full-value CAS when join runs on a **Ref** occurrence. **Id-only comparison would paper over that join bug** but would weaken CAS for any edit where only the ownership flag differs (duplicate-link rows, parse ref restoration, promote-ref delete prelude). For relaxing the global `FileAgent` revision gate (`src/Server/FileAgent.fs:150-152`), Replace-level CAS is necessary but not sufficient: `SetText` / `SetName` / etc. still have no analogous guard.

---

## 1. Every `Op.Replace` producer in `src/` (production)

| Module | Lines | Verdict | Notes |
|--------|-------|---------|-------|
| `src/Client/UpdateMove.fs` | 112 | **Faithful** | Same-parent reorder: `oldChildren = graph.nodes.[parentId].children` (103), full-list replace at index 0. |
| `src/Client/UpdateMove.fs` | 114-115 | **Faithful** | Cross-parent move: remove uses `selectedChildren` from `rangeChildren` (81); insert uses `[]` (pure insert at dest). |
| `src/Client/UpdatePaste.fs` | 85-89 | **Faithful** | Select-mode paste: `selectedChildren` from `rangeChildren` (83). |
| `src/Client/UpdatePaste.fs` | 109, 143 | **Faithful** | Edit-mode sibling insert: `oldChildren = []` (zero-width insert after caret). |
| `src/Client/UpdatePaste.fs` | 218 | **Faithful** | Cut remove: `selectedChildren` from `rangeChildren`. |
| `src/Client/UpdateHelpers.fs` | 305 | **Faithful** | Split: `[]` insert at computed index (blank sibling / new child / first child under expanded node). |
| `src/Client/UpdateOps.fs` | 433 | **Faithful** | Duplicate: `[]` insert at `sel.range.endd`; `newChildren` intentionally use `Ref` (430-432). |
| `src/Shared/FileNodeOps.fs` | 20 | **Faithful** | Create owned artifact: `[]` append/insert at `fileTreeInsertIndex` or directory insert index. |
| `src/Shared/FileNodeOps.fs` | 78 | **Faithful** | Insert file ref: `[]` at focus index (pure insert; no-op if duplicate ref already there, 68-76). |
| `src/Shared/Paste.fs` | 48 | **Faithful** | Internal paste wiring: parent is a freshly `NewNode`'d id; children list empty → `[]` at 0. |
| `src/Shared/Paste.fs` | 136 | **Faithful** | Clipboard deep-copy: remapped new parent ids, empty children → `[]` at 0. Ref flags copied from clipboard snapshot into **newChildren** only. |
| `src/Shared/ImportText.fs` | 79 | **Faithful*** | `existingChildren` passed in by caller; must be live at plan time. Production callers read focus node children. |
| `src/Shared/ImportText.fs` | 131 | **Faithful** | Directory merge append: `[]` at `existingChildren.Length`. |
| `src/Shared/AmbleRun.fs` | 22 | **Faithful** | `replaceAllChildrenOp`: `existing = graph.nodes.[parentId].children` (21). |
| `src/Shared/ViewModelJoinOps.fs` | 32 | **Unfaithful (ref)** | `removeCurrentOp`: `ownedChildren [ currentId ]` — always `Owner`; live row may be `Ref`. |
| `src/Shared/ViewModelJoinOps.fs` | 87 | **Faithful** | Reparent current's children onto previous: `[]` append at `prevNode.children.Length`; copies `currentNode.children` verbatim. |
| `src/Shared/ViewModelDeleteOps.fs` | 142 | **Faithful** | Promote: `[ oldChild ]` from classification (actual graph child). |
| `src/Shared/ViewModelDeleteOps.fs` | 193 | **Faithful** | TRASH append: `[]` at `trashLen`. |
| `src/Shared/ViewModelDeleteOps.fs` | 221, 341 | **Faithful** | Hard-delete subtree: `(pid, 0, children, remaining)` — full live `children` from graph. |
| `src/Shared/ViewModelDeleteOps.fs` | 237 | **Faithful** | Span remove: `selectedChildren` from live parent slice (234-235). |
| `src/Shared/ViewModelDeleteOps.fs` | 389-393 | **Faithful** | Dropped-owned batch remove: `[ item.child ]` from classification; indices sorted descending (387) so later ops see updated graph. |
| `src/Shared/documents/DocumentColdParse.fs` | 217 | **Faithful*** | `remainingOld` from `before` graph (163-166, 210-212), filtered for drops handled by preceding delete ops in same batch (288: `nodeOps @ deleteOps @ childOps`). |
| `src/Shared/dotnet/DocumentParseOps.fs` | — | (planner) | Delegates to `DocumentColdParse` / warm path → `planOpsFromGraphs`; no direct `Op.Replace`. |
| `src/Shared/dotnet/LazyLoadReconciliation.fs` | 81-85 | **Faithful** | Ref replacement: `[ oldChild ]` from `getAllOccurrences` (78). |
| `src/Shared/dotnet/LazyLoadReconciliation.fs` | 111-112 | **Faithful** | Trash move: `[ ownerChild ] = parent.children.[index]` (103); TRASH append `[]` at `trashIndex`. |
| `src/Shared/dotnet/LazyLoadReconciliation.fs` | 189-198 | **Faithful** | Reparent on disk rename: `[ ownerChild ]` from `oldParent.children.[oldIndex]` (186). |
| `src/Shared/History.fs` | 281 | (invert) | `invertOp` swaps old/new for undo — see §4. |
| `src/Shared/Serialization.fs` | 237, 292 | (codec) | Round-trip; does not plan edits. |

\*Faithful contingent on batch ordering / caller supplying current children (see §3).

**Client delete** does not construct `Op.Replace` directly; it calls `ViewModelDeleteOps.planDeleteOps` (`src/Client/UpdateOps.fs:466`).

**Indent / outdent / move up-down** all route through `UpdateMove.tryMoveNodeFromTo` → `replaceOpsForMove` (no separate Replace sites).

---

## 2. `oldChildren = []` at non-zero index; ref mismatches; id-only vs full-value

### `oldChildren = []` with non-zero index

This is the **insert idiom**, not an unfaithful shortcut. `Graph.replace` takes `oldCount = 0` children at `index` (`GraphMutate.fs:241-244`); an empty span always matches `[]`. Used at non-zero indices by:

- `UpdatePaste.fs:109,143` (after focused sibling)
- `UpdateOps.fs:433` (duplicate at `endd`)
- `UpdateHelpers.fs:305` (split insert)
- `FileNodeOps.fs:20,78` (when index is not end — still a zero-width insert **before** existing tail)
- `ImportText.fs:131` (append at `existingChildren.Length` — index equals child count, also valid)

**Not a CAS problem.**

### Producers with possible **ref** mismatch in `oldChildren`

| Location | Issue |
|----------|-------|
| `ViewModelJoinOps.fs:32` | **Only production gap found.** Uses `ChildNode.owner currentId` for removal; if the focused row is a **Ref** occurrence (`Node.childOwnership = Ref`), live span is `{ ref = Ref; id = currentId }` → full-value CAS fails. |
| `ViewModelDeleteOps.fs:142` | Uses actual `oldChild` from graph; `newChild` flips ref to Owner — old side faithful. |
| `Paste.buildPasteOpsFromClipboard` (`Paste.fs:133-135`) | Remaps ids/refs into **newChildren** on empty parents — old side `[]`. |
| `DocumentColdParse` (`restoreMatchingRefs`, `DocumentColdParse.fs:134-154`) | Adjusts **newChildren** ref flags from prior graph; `oldChildren`/`remainingOld` come from live before graph. |

### Id-only vs full-value comparison

| Approach | Effect |
|----------|--------|
| **Full-value** (current) | Correct CAS for ownership-sensitive structure (Refs vs Owners at same id). Catches stale plans and ref/owner confusion. |
| **Id-only** | Would allow join-on-Ref to keep working without fixing `removeCurrentOp`. Would **not** detect ref-vs-owner drift; weakens compare-and-swap to "same ids in order" only. |

**Recommendation:** keep full-value comparison; fix `ViewModelJoinOps.removeCurrentOp` to use the live child at `(parentId, indexInParent)` (as delete/paste already do).

---

## 3. Multiple `Replace` ops on the same parent in one `Change`

### Fold semantics

`Change.apply` (`src/Shared/History.fs:312-333`) folds **`change.ops` left-to-right**, each `Op.apply` seeing the graph produced by prior ops in the **same change**. `History.applyChange` adds ownership validation after the full fold (`632-639`). Undo folds **reversed** ops (`335-350`).

### Production sequences (same parent)

| Path | Pattern | Safe under CAS? |
|------|---------|-----------------|
| `UpdateMove` cross-parent | Two ops, **different** parents | Yes |
| `UpdateMove` same-parent reorder | Single full-list Replace | Yes |
| `ViewModelDeleteOps.planDeleteOps` | Promote (often other parent) → span remove → trash append → hard-delete | Yes; span remove uses live slice; multi-index removes on one parent use **descending** indices (387) |
| `DocumentColdParse.planOpsFromGraphs` | `nodeOps @ deleteOps @ childOps` | **Designed** for CAS: `remainingOld` excludes ids removed by `deleteOps` (210-212, 286); delete runs before child Replace (288) |
| `ImportText.buildImportChange` | `package.ops` (nested new nodes) then attach Replace on focus | Yes if focus unchanged by nested ops (nested parents are new ids) |
| `UpdatePaste` select mode | `nested @ [ replaceOp ]` | Yes: nested Replace targets new paste ids; final replace uses live selection slice |
| `LazyLoadReconciliation` trash | Remove from owner parent + append TRASH | Different parents |

### Hand-written test staging (same parent, one change)

`tests/Server.Tests/DatabaseProjectionTests.fs:122-123`: second Replace's `oldChildren` matches **post-first-op** state, not original — documents required staging.

**Risk:** any planner that emits two Replace ops on the same parent where the second's `oldChildren` was computed from the **pre-change** graph (without full-list or descending-index discipline) would fail CAS. No production producer found with that bug besides the join ref case above.

---

## 4. Op rewriting / restamping / undo

| Mechanism | Location | Touches `oldChildren`? |
|-----------|----------|------------------------|
| `PersistStamp.appendToLast` / `appendToChange` | `History.fs:663-676` | **No** — appends `SetUpdateTime` only |
| `overlayFresh` | `FileAgent.fs:107-118` | **No** — merges stamped changes by `changeId`; Replace ops unchanged |
| `Change.invert` / `invertOp` | `History.fs:275-281,307-310` | Swaps old↔new for undo record; forward apply already succeeded under CAS |
| `Op.undo` for Replace | `History.fs:235-237` | Calls `Graph.replace` with swapped lists; CAS checks live span equals forward **newChildren** |
| `Serialization` encode/decode | `Serialization.fs:237,292` | Preserves all four Replace fields |

**Conclusion:** no restamp path desynchronizes `oldChildren` from the graph state at apply time.

---

## 5. Test blast radius (static review; suite not run)

### Already aligned with CAS

- **`HistoryTests.fs:261-271`** — expects rejection when remove uses wrong `oldChildren` (`[ second ]` at index 0).
- **`ModelTests.fs:405-413,648-653`** — reorder tests pass live `oldChildren` from graph.
- **`HistoryTests.fs:862-900`** — duplicate / mid-list insert with `[]` (insert idiom).
- **`DocumentColdParseTests.fs:121-150`** — applies `nested @ [ replaceOp ]` via `History.applyChange`; uses live `selected` slice.
- **`ViewModelJoinOpsTests.fs`** — all flat fixtures use **Owner** children; matches current join planner.
- **Most server/client tests** — `Op.Replace(..., [], ...)` insert-only or `Graph.replace ... []` setup helpers.

### Tests using hand-written / synthetic `oldChildren`

| File | Approx. `Op.Replace(` count | Likely CAS outcome |
|------|---------------------------|-------------------|
| `HistoryTests.fs` | 22 | Pass (includes intentional invalid + faithful moves) |
| `ModelTests.fs` | 2 in Change + many direct `Graph.replace` | Pass (reorder uses live lists) |
| `ImportTextTests.fs` | 8 | Pass (mostly `[]` insert/append; full replace uses caller-supplied `existing`) |
| `DatabaseProjectionTests.fs` | 6 | Pass (staged same-parent pair at 122-123) |
| `LazyLoadReconciliationTests.fs` | 8 | Pass (faithful patterns) |
| `ViewModelJoinOpsTests.fs` | 6 | Pass today (Owner-only fixtures) |
| `GraphProjectionTests.fs`, `StateEndpointTests.fs`, `DbAgentTests.fs`, etc. | mostly `[]` inserts | Pass |

### Gap / would fail if behavior exercised

- **No test** asserts `"old span does not match"` directly (only a comment at `ModelTests.fs:253`; error string exists at `GraphMutate.fs:247`).
- **Join on Ref occurrence** — no test; would fail full-value CAS until `removeCurrentOp` is fixed.
- **`ImportTextTests.fs:88`** — uses synthetic `existing = owned [ NodeId.New() ]` for change **shape** assertions; tests that **apply** use live `[]` or real children.

**Rough blast radius if CAS were newly enabled:** low for the current suite (~0–1 failing tests unless join-on-Ref is covered). **With CAS already enabled in `GraphMutate.fs`,** the suite is already written for it; remaining risk is production join-on-Ref, not mass test breakage.

---

## Ranked code paths to fix first (before relying on Replace CAS for relaxed revision gating)

1. **`ViewModelJoinOps.removeCurrentOp`** (`ViewModelJoinOps.fs:31-32`) — read live `parent.children.[indexInParent]` instead of `ownedChildren [ currentId ]`.
2. **`ImportText.buildImportChange` callers** — ensure `existingChildren` is always read immediately before planning (already true in client import paths; keep as invariant).
3. **`DocumentColdParse.planOpsFromGraphs` batch** — keep `deleteOps` before child `Replace`; add regression test if concurrent same-parent edits become common under relaxed revision checks.
4. **Future: non-Replace ops** — `SetText`/`SetName`/… still lack span CAS; relaxing `FileAgent.fs:150` revision check requires separate staleness strategy for those op types.

---

## Reference: current `Graph.replace` CAS (already present)

```241:247:src/Shared/GraphMutate.fs
                    let existing =
                        children
                        |> List.skip index
                        |> List.take oldCount

                    if existing <> oldChildren then
                        Error "old span does not match"
```

```312:326:src/Shared/History.fs
    let apply (change: Change) (state: State) : ApplyResult =
        let step (accState, hasChanged) op =
            match Op.apply op accState with
            ...
        let result =
            change.ops
            |> List.fold
                (fun acc op ->
                    ...
                (Ok(state, false))
```

```150:152:src/Server/FileAgent.fs
                | None when change.id <> s.revision.Value ->
                    Error
                        $"Revision mismatch: server is at revision {s.revision.Value}, but this Change targets base revision {change.id}."
```
