# Selective client loading

## Destination

Resolve every product, domain, and architectural decision needed for `/to-spec` to produce a complete, coherent, implementation-ready selective client loading specification. Implementation itself is outside this map.

## Notes

- This is a client-only phase of the broader [on-demand graph residency roadmap](../../doc/roadmap/on-demand-graph-residency.md). Its chronological position is not decided or important, and the server remains fully resident throughout this effort.
- The [preliminary `.amb` concept](../../doc/roadmap/selective%20client%20loading.amb) is a set of hypotheses to litigate, not established requirements. The shift from everything-present to partial client presence must be challenged for completeness and coherence.
- Residency is monotonic within one webpage session: unloaded may become loaded, but loaded never becomes unloaded. Refresh starts a new session. Eviction and re-unloading are outside this effort.
- “Loaded” means the client holds an authoritative, current snapshot of a node’s complete direct-child list, not merely that a request was attempted. An unloaded node and a loaded leaf are distinct states.
- Mechanism and trigger choice are separate: one pure server function expands the three fixed load modes, while each client trigger directly chooses its mode and targets without a client-wide loading planner.
- The current trigger mapping must cover startup, explicit Load, navigation, search selection, move targets, relevant edits, session restoration, and synchronization. Do not design a configurable policy framework.
- A client starts with a zoom-root. Its owner chain identifies the workspace that anchors the startup loading decision.
- `Direct`, `ArtifactClosure`, and `Workspace` are the complete current load-mode set, not an extensibility interface.
- Every ticket session should consult the grilling and domain-modeling skills. Use codebase-design when module or API shape is the decision, and prototype only when a runnable or visual artifact is genuinely needed.
- [Inspect Selective loading](../../tmp/Inspect%20Selective%20loading.md) is a supporting factual baseline, not the canonical decision store.

## Decisions so far

- [Represent authoritative child-list residency](issues/01-represent-child-list-residency.md): child-list and owner knowledge are explicit; only `Loaded` lists contribute complete Owner/Ref edges and derived indexes.
- [Set partial-graph and document boundaries](issues/02-set-partial-graph-boundaries.md): loaded lists carry every target header; Artifact closures stop at nested artifacts, Workspace closures stop at nested Workspaces, and zoom retains exact ingress ancestry.
- [Define the load snapshot protocol](issues/03-define-load-snapshot-protocol.md): no-revision Workspace bootstrap and later Direct, ArtifactClosure, or Workspace batches install revision-current authoritative snapshots atomically.
- [Choose startup bootstrap scope](issues/04-choose-startup-bootstrap-scope.md): first render waits for Workspace-mode ROOT and zoom-Workspace targets; SYSTEM and TRASH follow implicitly from ROOT ownership.
- [Restore sessions under partial residency](issues/05-restore-session-partial-residency.md): save zoom, owning Workspace, and fold preferences; bootstrap around that Workspace, fall back on stale navigation, and skip non-resident folds.
- [Define unloaded navigation and UI semantics](issues/06-define-unloaded-navigation-ui.md): hollow-circle and framing navigation use Direct; a successful hollow-circle parent load unfolds, while traversal remains resident-only.
- [Define search across selective residency](issues/07-define-selective-residency-search.md): Find remains resident-only; committed unknown picks load their Artifact closure before framing through the first discovery occurrence.
- [Define move and edit residency dependencies](issues/08-define-move-edit-dependencies.md): Move loads only an unknown destination list and relies on server validation; other edits use resident dependencies, while Delete avoids global client loading.
- [Define synchronization and revision correctness](issues/09-define-sync-revision-correctness.md): poll and load responses atomically catch up represented facts, supplement partial tails, and use base-revision and mutation-epoch guards without invalidating loadedness.
- [Set explicit Load command responsibility](issues/10-set-explicit-load-command.md): rename Upload to Load and run upload, parse/reconciliation, then one non-mixed Workspace or ArtifactClosure residency request for the full selection.
- [Unify the loading decision function](issues/11-unify-loading-decision-function.md): reject a client-wide planner; client triggers choose fixed modes directly, one pure server function expands them, and generic handling installs and coalesces responses.
- [Place selective-loading module seams](issues/12-place-selective-loading-seams.md): Shared owns pure expansion, transaction application, and load coordination; Client and Server remain thin trigger, I/O, and atomic-capture adapters.

## Not yet specified

## Out of scope

- Implementing selective client loading or producing implementation slices.
- Partial server residency, lazy server cache admission, server startup de-residency, or server eviction. A server endpoint needed by the client mechanism remains eligible while the server graph stays fully resident.
- Client eviction, re-unloading, or passive reclamation during a webpage session.
- A configurable loading-policy framework, alternative policies, or speculative future loading scopes.
