# Parse Actor

Stage: chart
Summary: A continuous Server Parse Actor turns file-shaped disk into Graph, takes priority from Browser wants, and emits Changes.
Updated: 2026-09-28

**Part of:** [[plan/roadmap/epics/chapters/automatic-parse.md]]

## Notes

- File-shaped file→Graph stays on the Server. The Browser and the App do not Parse from Workspace Download or Workspace Upload.
- Browser wants come from [[plan/browser-residency/project.md]]. File transit stays on [[plan/transport-layer/project.md]].
- 2026-09-28 — Alan lock (chat), filed on [[plan/github-transport/project.md]]: one long-lived Parse actor. Nobody starts an Actor after pull. Core pushes a reconcile target onto the stack; others may push too. Parse = disk → graph. Workspace/directory Parse reconciles immediate members only, then Unparsed on children that need work, then Parsed. See [17 — Git Load: Unparsed then Parse stack](plan/github-transport/issues/17-post-pull-cascade-and-gate-handoff.md), [18 — One Parse actor stack](plan/github-transport/issues/18-parse-actor-stack-and-file-lock-ownership.md), and [19 — Parsed/Unparsed and Persisted/Unpersisted](plan/github-transport/issues/19-file-newer-graph-newer.md).
