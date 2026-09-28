# Parse Actor

Stage: chart
Summary: A continuous Server Parse Actor turns file-shaped disk into Graph, takes priority from Browser wants, and emits Changes.
Updated: 2026-09-28

**Part of:** [[plan/roadmap/epics/chapters/automatic-parse.md]]

## Notes

- File-shaped file→Graph stays on the Server. The Browser and the App do not Parse from Workspace Download or Workspace Upload.
- Browser wants come from [[plan/browser-residency/project.md]]. File transit stays on [[plan/transport-layer/project.md]].
- 2026-09-28 — Alan lock (chat), filed on [[plan/github-transport/project.md]]: one Parse Actor with a stack of files to parse; anybody may push. File fine lock is Unparsed; Parse clears it when that file’s Graph update is done. Directory lock is Reconciling on Directory nodes. Long parse must not block a later git Load pull. Pipeline: github → file → parse → (merge) graph → file. See [17 — Post-pull cascade and gate handoff](plan/github-transport/issues/17-post-pull-cascade-and-gate-handoff.md) and [18 — Parse Actor stack and file-lock ownership](plan/github-transport/issues/18-parse-actor-stack-and-file-lock-ownership.md).
