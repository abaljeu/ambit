# 14 — Route Load and Save by path pre-pick

**Type:** coding
**Status:** done
**Blocked by:** [11 — Pick git or desk for plain Load and Save](11-pick-git-or-desk-for-plain-load-save.md); [13 — Run git Load and Save through the Server Peer Actor](13-actor-runs-git-load-save.md)
Actual: 2h20m

## Context

A person runs Load or Save and can choose plain, git, or desk behavior. Plain behavior must inspect the Workspace remote and choose a path. An explicit git or desk pre-pick must use that path directly. The person still uses the existing Load and Save Command names.

## What to build

### 1. Command Load/Save

Extend **Command Load/Save** as defined by the Module map in [[../arch.md]]. This capability supports the **Load**, **Save**, **git Load**, **git Save**, **desk Load**, **desk Save**, **Plain Load prefers git**, **Plain Save prefers git**, and **No automatic pull or push** Story paths.

- [x] 1.1.2 Represent the path pre-pick — A Load or Save request carries Plain, Git, or Desk without adding a new primary Command name.
- [x] 1.2.2 Pick a path for plain commands — Plain Load and Save get the Workspace remote fact and ask PathPick.
- [x] 1.2.3 Honor explicit git and desk — Explicit git or desk skips PathPick and uses the selected path.
- [x] 1.2.4 Start only from a person Command — No schedule, post-Persist, or post-Download path starts git pull or push.
- [x] 1.2.5 Use the command-request door — Load and Save send a load/save command request through the mailbox to the actor pool, not through Run or `?git`.
- [x] 1.2.6 Preserve Load completion — Each Load form keeps today's Parse / graph-push coupling after files land; this ticket does not expand selection-scoped Parse. **Superseded 2026-09-28.** Current truth is Unparsed → push onto the one Parse actor ([02 — Git Load: Unparsed then Parse stack](../../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md)). Selection is push-on-stack ([05 — Selection-scoped Parse after whole-tree git Load](../../core-refinement/issues/05-selection-scoped-parse-after-whole-tree-git-load.md)). This line records what shipped; it is not current required acceptance.
- [x] 1.3.1 Use PathPick only for plain commands — Tests prove a remote chooses Git, no remote chooses Desk, and each explicit pre-pick bypasses the chooser.
- [x] 1.3.2 Invoke the Peer Actor for git — Git Load and Save reach the Server Peer Actor through the actor pool with Focus.
- [x] 1.3.3 Preserve the desk path — Desk Load and Save continue to use the existing WebDAV and desk behavior.
- [x] 1.3.4 Preserve existing Parse hops — Load still reaches `parseFileOp`, directory reconciliation, and Fetch+Poll as applicable after either transport path. **Superseded 2026-09-28.** `parseFileOp` / directory reconciliation are not current required hops. Current truth is Unparsed → push onto the one Parse actor; Fetch+Poll stays for residency. This line records what shipped.

## See also

- [github-transport architecture](../arch.md)
- [02 — Actor command surface](02-actor-command-surface.md)

## Time

- 2026-09-27 1h — implemented and reviewed path pre-picks, mailbox and actor-pool routing, Client command variants, desk fallback, and routing tests
- 2026-09-27 30m — added independent-review proof for actual Desk continuations, routed Git Save, and routed remote-first Load reconciliation/Poll events
- 2026-09-27 30m — drove Desk continuations through final HTTP requests and stabilized routed Git completion under full-suite load
- 2026-09-27 15m — replaced delegate-only Desk proof with real HttpClient handler POST observations and removed the orphaned inventory wrapper
- 2026-09-28 5m — annotated 1.2.6 / 1.3.4 `parseFileOp` / directory-reconciliation hops as superseded by Unparsed → push onto Parse (#151)
