# 24 — Keep navigation and Find resident-only

**Context:** Zoom, Find, folds, traversal, and range commands must stay inside the resident projection. They must stay synchronous. They must not load content. Only bootstrap and Load may add resident content.

**What to build:** No code changes are expected.  Add tests that these operations never become hidden Load paths: Zoom, Find, Find-result commit, folds, traversal, and range behavior inside the resident projection.

**Blocked by:** 19 — Bootstrap fresh sessions with complete ROOT.

**See also:** [[.scratch/selective-client-loading/spec.md]] (resident-only Zoom, Find, folds, traversal); [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]] (no implicit loading from navigation surfaces).

**Status:** ready-for-agent

- [ ] Zoom treats a resident header with Unloaded children as an ordinary leaf. Zoom does not request more residency.
- [ ] Find returns only matches from resident headers and Loaded child lists. Find has no Server or Load effect.
- [ ] A Find-result commit uses normal Zoom behavior. It does not Load the result's Workspace.
- [ ] Fold restore and fold or unfold use only resident content. They never request residency.
- [ ] Traversal and range commands use projected child lists. They stop or continue at Unloaded boundaries. They never run Load.
- [ ] Only bootstrap and Load add resident content that these navigation surfaces can then use.
