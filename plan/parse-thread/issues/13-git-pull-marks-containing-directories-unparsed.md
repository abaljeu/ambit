# 13 — Git pull marks containing Directories Unparsed

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

When a git pull adds or removes a file, the Directory Node that contains that file stays Current. The Workspace Node mark and the one-level reconcile already stand in [Parse thread architecture — story path 18 Pull handoff stays the pull sentence](../arch.md).

## What to build

When a pulled file is added or removed, the pull marks its containing Directory Node Unparsed.

### 1. Containing Directory

An added file marks the Directory Node that will contain it Unparsed. A removed file marks the Directory Node that contained it Unparsed. A renamed file marks both Directory Nodes Unparsed. When the containing Directory Node is absent, the pull marks the nearest ancestor Directory Node that exists. A file at the top of the workspace uses the Workspace Node. A modified file keeps its containing Directory Node Current.

### Acceptance Criteria

1. [ ] **Added file** — An added file marks its containing Directory Node Unparsed.
2. [ ] **Removed file** — A removed file marks its containing Directory Node Unparsed.
3. [ ] **Rename** — A renamed file marks the Directory Node it leaves and the Directory Node it enters Unparsed.
4. [ ] **Top of the workspace** — A file at the top of the workspace uses the Workspace Node.
5. [ ] **Modified file** — A modified file leaves its containing Directory Node Current.
6. [ ] **Nearest existing ancestor** — File `w/x/y/z` is added. Directory Node `w/x/y` is absent. Directory Node `w/x` exists. The pull marks Directory Node `w/x` Unparsed.

## Finish-up

1. [ ] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [ ] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [ ] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved.

## See also

1. [12 — Git pull marks each received file Unparsed](12-git-pull-marks-each-received-file-unparsed.md)
2. [Parse thread spec — story 18 Pull handoff stays the pull sentence](../spec.md)
3. [core-refinement architecture — §5 git pull finish](../../core-refinement/arch.md)
4. [Mark each file a git pull receives Unparsed](https://github.com/abaljeu/ambit/pull/231)
