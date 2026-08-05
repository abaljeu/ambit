# 27 — Document delivered selective-loading baseline

**Context:** The selective client-loading work is complete. Documentation must describe the delivered system. Put one home document under [[doc/current/]]. Other docs must link to that home. Keep only later server-residency work in the roadmap. For this client phase, write Unloaded and Loaded. Do not write Unknown.

**What to build:** Write the delivered selective client-loading baseline as one document under [[doc/current/]]. Point [[doc/index.md]] and related references to that document. Leave only unimplemented server-residency work in the roadmap. Use Unloaded and Loaded for the client phase.

**Blocked by:** 20 — Restore saved zoom Workspace during bootstrap; 23 — Make hollow-circle clicks invoke Load; 24 — Keep navigation and Find resident-only; 26 — Forbid Unloaded destinations in the Move dialog.

**See also:** [[.scratch/selective-client-loading/spec.md]] (Further Notes documentation promotion); [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]] (Authority).

**Status:** ready-for-agent

- [ ] One [[doc/current/]] document states the delivered facts: complete-Workspace client residency, bootstrap scope, explicit Load, resident-only navigation, ChangeRequest sync, canonical owners, structural guards including MoveSelected, and Move-dialog Unloaded destination rules.
- [ ] [[doc/index.md]] presents selective client loading as current behavior and points to that document.
- [ ] Sync, persistence, workspace-graph, architecture, API, and command references agree with that baseline. They link to it instead of restating competing models.
- [ ] The roadmap keeps only unimplemented work: partial-server residency, per-document versions, projection patches, hybrid search, and reclamation.
- [ ] Current and roadmap material use Unloaded and Loaded. They do not present Unknown as the implemented client model.
- [ ] Docs do not claim that this phase has partial server residency, automatic loading, eviction, or multiple loading modes.
- [ ] document `?scope=full` in `doc/api.md` when selective-loading baseline is promoted (ticket 19)