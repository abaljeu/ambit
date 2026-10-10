# 09 — Grill reconcile discovery depth

**Status:** `done`
**Type:** grilling
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

Alan expected one change after a git pull. Reconcile walks one directory level. That change is `SearchOption.AllDirectories` to `SearchOption.TopDirectoryOnly` in `discoverArtifactRelatives`. One line is not enough. The same function feeds cold bootstrap through `readAllDocuments`. The same function feeds Directory reconcile, Workspace reconcile, and the git changed-list union. `EnumerateFiles` lists files. `EnumerateFiles` does not list child directories.

Two attempts were abandoned. They closed unmerged as pull request 227 — Git Load reconcile discovers top level only. These review findings stay open.

1. **Child directories with files** — A child directory that contains files is not discovered. Nothing marks that Directory Node Unparsed. Pull handoff stays the pull sentence says the pull reconciles immediate members.
2. **Nested files the pull changed** — git pull finish says the git changed list is the discovery. Matching File Nodes and Directory Nodes are marked Unparsed. A top-level walk drops nested files the pull changed.
3. **Argument Count** — A positional `SearchOption` argument hits the Argument Count smell.
4. **Held Current child Directory Node** — An earlier attempt marked every held Current child Directory Node Unparsed on every parent reconcile. That mark blocks editing inside those Directory Nodes until a Load. Alan rejected that mark as too broad.

This ticket grills Alan and settles the design. This ticket writes no code. 24 — Command Load/Save waits until this ticket is done, and until each one-fix coding ticket this ticket writes is done.

## What to build

Grill Alan on reconcile discovery depth. Record each answer on this ticket. Write one coding ticket for each fix. Each coding ticket is one fix.

1. [x] **Which Load walks one level** — Which reconcile calls walk one level: git Load only, Directory Load, or Workspace Load?
2. [x] **Immediate member** — What counts as an immediate member: files only, or files and child directories? What state does a newly discovered child Directory Node get?
3. [x] **Held Current child Directory Node** — What happens to a child Directory Node the Graph already holds as Current?
4. [x] **Git changed list and nested files** — Does the git changed list drive discovery for deeper paths? How does a nested changed file surface? One option is to mark the owning top-level child Unparsed.
5. [x] **Cold bootstrap depth** — Does cold bootstrap stay recursive? `readAllDocuments` is one caller. Directory reconcile, Workspace reconcile, and the git changed-list union use the same discovery. Is any other caller in scope?
6. [x] **Depth without a new positional argument** — How does discovery depth pass without a new positional argument?
7. [x] **One-fix coding tickets** — What are the follow-up coding tickets? Each ticket is one fix.

### Acceptance Criteria

1. [x] **Each question answered** — Alan answered each question in What to build.
2. [x] **Answers on this ticket** — The decisions are recorded on this ticket. Status becomes `done` when Alan accepts those answers.
3. [x] **One-fix coding tickets** — The follow-up coding tickets are written from those answers. Each ticket is one fix.
4. [x] **24 waits** — 24 — Command Load/Save stays blocked by this ticket and by each of those coding tickets.

## Finish-up

1. [x] **Piece answers** — Changes during the grill are written on this ticket, or the ticket records that none changed.
2. [x] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [x] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved. doc/current and doc/user do not state this discovery depth.

## See also

1. [Parse thread architecture — story path 18 Pull handoff stays the pull sentence](../arch.md)
2. [Parse thread spec — story 18 Pull handoff stays the pull sentence](../spec.md)
3. [core-refinement architecture — §5 item 4 git pull finish](../../core-refinement/arch.md)
4. [02 — Git Load: Unparsed then Parse stack](../../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md)
5. [24 — Command Load/Save](../../github-transport/issues/24-load-save-on-events.md)
6. [Argument Count](../../../.agents/skills/code-review/SMELLS.md)
7. [Document persist path](../../../src/Server/DocumentPersistPath.fs)
8. [Git Load reconcile discovers top level only](https://github.com/abaljeu/ambit/pull/227)
9. [10 — Add discoverImmediateMembers](10-add-discover-immediate-members.md)
10. [11 — Load reconciles walk one level](11-load-reconciles-walk-one-level.md)
11. [12 — Git pull marks each received file Unparsed](12-git-pull-marks-each-received-file-unparsed.md)

## Answer

Locked 2026-10-10 (Alan).

1. **Which Load walks one level** — git Load, Directory Load, and Workspace Load all walk one level.
2. **Immediate member** — Immediate members are files and child directories. A newly discovered child Directory Node is Unparsed. Its contents stay unread until that Directory Node is Loaded.
3. **Held Current child Directory Node** — A child Directory Node the Graph already holds as Current is left alone on the parent reconcile.
4. **Git changed list and nested files** — A git pull marks each file it received Unparsed. Each file is an entry in the git changed list, at any depth. The changed list does not drive discovery otherwise.
5. **Cold bootstrap depth** — Cold bootstrap through `readAllDocuments` stays recursive. Only the Load reconciles walk one level.
6. **Depth without a new positional argument** — Two named functions. `discoverArtifactRelatives` stays recursive for cold bootstrap. `discoverImmediateMembers` returns the direct files and the child directories for the Load reconciles. There is no new positional argument.
7. **One-fix coding tickets** — 10 — Add discoverImmediateMembers. 11 — Load reconciles walk one level. 12 — Git pull marks each received file Unparsed. Each ticket is one fix. Each blocks 24 — Command Load/Save.

## Comments

- 2026-10-10 — Filed for a grilling session with Alan. Status `needs-info`. The next action is that session. No code until the answers are recorded and the one-fix coding tickets exist.
- 2026-10-10 — Alan answered each question. Status `done`. The coding tickets are 10 — Add discoverImmediateMembers, 11 — Load reconciles walk one level, and 12 — Git pull marks each received file Unparsed.
