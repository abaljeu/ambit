# 02 — Document watch: debug URL and hard-reload after esbuild

**Status:** ready-for-agent
**Blocked by:** None — can start immediately.

## Context

[[doc/reference/dev-debug-workflow.md]] covers Fable watch and `/ambit?debug=1` for unbundled modules. It does not yet say that after an esbuild rebuild the person must hard-reload, and that Ack on CodeOutdated does not unblock stale bundle code.

## What to build

The dev-debug reference tells a person on watch how to load debug modules and how to pick up an esbuild rebuild.

- [ ] Prefer `/ambit?debug=1` when source maps / unbundled Fable modules are needed.
- [ ] After esbuild rebuild, hard-reload the page.
- [ ] Ack on CodeOutdated does not unblock; it is not a reload.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md.

## See also

[[doc/reference/dev-debug-workflow.md]], [[.vscode/tasks.json]]
