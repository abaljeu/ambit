# 24 — Keep navigation and Find resident-only

**What to build:** Keep Zoom, Find, result commit, folds, traversal, and range behavior entirely within the resident projection so these interactions remain synchronous and never become hidden loading paths.

**Blocked by:** 19 — Bootstrap fresh sessions with complete ROOT.

**See also:** [[.scratch/selective-client-loading/spec.md]] (resident-only Zoom, Find, folds, traversal); [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]] (no implicit loading from navigation surfaces).

**Status:** ready-for-agent

- [ ] Zoom treats a resident header with Unloaded children as an ordinary leaf and emits no request for additional residency.
- [ ] Find returns only matches available from resident headers and Loaded child closures, with no server or loading effect.
- [ ] Committing a Find result delegates to normal Zoom behavior and does not hydrate the result's Workspace.
- [ ] Fold restoration and interactive fold or unfold behavior consume only resident content and never request residency.
- [ ] Traversal and range commands use the projected child lists, naturally stop or continue at Unloaded boundaries, and never invoke Load.
- [ ] Explicit bootstrap and Load remain the only observable ways these navigation surfaces gain new resident content.
