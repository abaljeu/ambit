# Standards review of [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md)

## 1. App.fs file length (hard)

[fsharp-source.md](.agents/rules/fsharp-source.md) limits each file to 800 lines. If a file is already longer, restructure only when the change increases the file. Split after the project edits, in a later operation. [App.fs](src/Client/App.fs) grew from 854 lines to 866 lines.

## 2. Branch name as delivery status (hard)

[planning-docs.md](.agents/rules/planning-docs.md) records what is implemented by ticket, section, or Point. Do not use git branch names as delivery status in plan docs. The Comments block of [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md) states that [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md) is done on `staging`:

```
- 2026-09-26: [07 — Expand Want and edges/Nodes package](07-expand-want-and-edges-nodes-package.md) is `done` on staging. Shared codecs already carried `want` / `nodes` / `childMap`. This ticket installs the Want answer after the Event tail and keeps Load `packages`.
```

## 3. Empty SyncResponse copies (smell: Duplicated Code)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Duplicated Code is a judgement call, not a hard violation. [App.fs](src/Client/App.fs) writes the same empty [SyncResponse](src/Shared/ApiResponses.fs) three times in `createRuntime`. That shape also appears in [SyncLogic.fs](src/Shared/SyncLogic.fs) `applyServerTail`. The three copies added 12 lines to a file that is already over the limit.

```
                            { events = []
                              packages = []
                              packageChildMap = Map.empty
                              nodes = []
                              childMap = Map.empty },
```
