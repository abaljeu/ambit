# Issue 02 — Replace conflict clarification

Answers: does Server Replace validate its target, and how do we know a second Replace does not conflict with a prior Replace on the same parent's children?

Sources: [[../issues/02-independent-concurrent-changes-succeed.md]], [[02-implement-plan.md]], [[../../relaxed-concurrency/spec.md]], [[../../relaxed-concurrency/design.md]], [[../details/conflict-resolution.md]], [[../details/as-implemented-facts.md]], [[../../../src/Shared/GraphMutate.fs]], [[../../../src/Shared/History.fs]], [[../../../src/Server/FileAgent.fs]], [[../../../src/Server/DbAgent.fs]].

## 1. What Replace compares today (old span / CAS) on apply

Yes — the target is validated on every apply. The Server does not add a separate Replace check. Both backends fold Changes through Shared apply only:

- [[../../../src/Server/FileAgent.fs]] — `applyBatch` → `History.applyChange`
- [[../../../src/Server/DbAgent.fs]] — `applyBatch` → `History.applyChange`

`History.applyChange` routes `Op.Replace(parentId, index, oldChildren, newChildren)` to `Graph.replace` ([[../../../src/Shared/History.fs]]). On failure it returns `ApplyResult.Invalid` with the Shared error string; agents surface that as the batch Error (HTTP 400 today).

`Graph.replace` in [[../../../src/Shared/GraphMutate.fs]] validates, in order:

1. Parent exists.
2. `index` and `index + length(oldChildren)` are in bounds of the live child list.
3. Every id in `newChildren` exists in the graph.
4. Placement rules (Workspace under Workspaces; File/Directory owner ancestry).
5. **Span CAS:** the live slice `children[index .. index+oldCount)` must equal `oldChildren` by full-value list equality (`existing <> oldChildren` → `"old span does not match"`).
6. After a successful span match, sibling/artifact owned-name conflicts on introduced owners, plus a few root/special placement guards.

So the compare-and-swap target is the **positional full-value child span** on one parent — not a Node id alone, and not the global revision. Attribute Ops use the same Shared pattern (`oldText` / `oldClasses` / …). Fact summary: [[../details/as-implemented-facts.md]].

## 2. Whether that catches two concurrent Replaces on the same parent's children

Yes, for races that leave a stale or overlapping span against the live list.

Apply is serialized in each agent. Change A that mutates parent P's children commits first. Change B that still carries `oldChildren` / `index` from the common prior then fails step 5 above when its span no longer equals the live slice — typically `"old span does not match"`, or bounds errors if the list shrank.

That is the intentional collision boundary in the relaxed-concurrency slice:

- Spec: same-parent structural races fail on span mismatch; unrelated parents succeed when each span matches ([[../../relaxed-concurrency/spec.md]]).
- Design matrix: stale same-parent Replace → Reject / 400 / `"old span does not match"` ([[../../relaxed-concurrency/design.md]]).
- Issue 02 plan test 5: same-parent structural collision expects 400 and parent's children matching A ([[02-implement-plan.md]]).

Caveat for later tickets: Kind 3 in [[../details/conflict-resolution.md]] wants occurrence-bag **Accept Both** (and Server amend of the newest Replace) for independent inserts under one parent. That is **not** how apply works today. Today, any second Replace whose planned span does not still match the live list Rejects — including many "independent insert" races under the same parent after the first has shifted or rewritten children. Full-value span CAS is load-bearing; id-only matching is rejected ([[../../relaxed-concurrency/spec.md]]).

Different parents are independent axes: each Replace CAS only reads that parent's child list, so two Replaces under distinct parents can both succeed once the global revision gate is gone.

## 3. What issue 02 deliberately does NOT change

Issue 02 only removes the **global revision gate** in FileAgent/DbAgent (`change.id <> s.revision` → Error). It does **not**:

- Edit [[../../../src/Shared/GraphMutate.fs]] or [[../../../src/Shared/History.fs]]
- Soften or merge same-parent Replace races
- Invent field or child-list merge / Server amend (that belongs to later tickets — e.g. 03 recoverable field collisions; Kind 3 Accept Both)
- Change Reject for real per-Op CAS failures (`old span does not match`, attribute old-value mismatch, auth, malformed)

Same-target attribute CAS and same-parent Replace CAS **still Reject** after issue 02. Acceptance criteria say Auth and malformed remain Reject and the ticket does not invent merge ([[../issues/02-independent-concurrent-changes-succeed.md]]). Plan defaults: keep CAS Reject for colliding Ops; keep one global Server revision sequence for poll/load ([[02-implement-plan.md]]).

## Short verdict

Server Replace **does** validate its target — via Shared `Graph.replace` span CAS on apply, not via a Server-only check. A second concurrent Replace on the **same** parent's children is caught when its `oldChildren` span no longer matches the live list after the first apply. Issue 02 lets unrelated (different-parent / different-field) Changes succeed despite a stale global revision; it leaves same-parent structural overlap as Reject until later amendment work.
