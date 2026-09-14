# 35 — Browser Run hello

**Status:** ready-for-agent
**Blocked by:** [[34-outside-core-lifecycle-proof.md|Outside Core lifecycle proof]].

## 1. Context

Outside Core can already prove TestActor `hello` through the mailbox lifecycle (ticket 34). A user still needs the same hello through existing Browser Run: current Node text starting with `?` becomes a one-Node Command, travels HTTP, and lands one Owned child text `hello` under Focus. Architecture names that path **Browser Run hello** on [[plan/core-creation/arch.md|Core creation architecture]].

Architecture (Story paths, Module map, Seams): [[plan/core-creation/arch.md|Core creation architecture]]. This ticket holds acceptance for that Browser path; do not restate Module map Interface here. Parent umbrella [[29-prove-testactor-hello.md|Prove TestActor hello]] stays as written; this ticket is the takeable cut for Story path 1 once 34 is done.

## 2. What to build

Make Browser Run hello end-to-end: Browser Run (`?` → one-Node Command with credentials and `graphIds` from **Loaded descendant id list**) → HTTP Adapter encodes Command + credentials → CoreMailbox `startActor` → CoreMsg validates and hands off → CoreActorPool.startActor (expand, select `test`, create, ActorStarted, schedule) → TestActor interprets → `hello` → CoreMsg admit-before-`PostChange` → CoreMsg `ActorStop` of `ActorSucceeded` → History appends ActorFinished → universal response when that path is exercised → Browser shows one Owned child text `hello`. Follow arch Story path **Browser Run hello**. Point at arch Module map for State / Interface / Uses.

Command text remains `?test hello` per [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. Reuse existing Run UI; add no new Browser chrome or control. Existing Run for text not starting with `?` stays unchanged (AmbleRun).

### What this increment avoids

Rebuilding the outside Core lifecycle already delivered by [[34-outside-core-lifecycle-proof.md|Outside Core lifecycle proof]], live query, cancel, host-stop, fail, post-twice, duplicate terminal, Interrupted restart, new Browser chrome or controls, and Actor definitions other than TestActor.

### 1. Loaded descendant id list

Shared Zoom-rooted Loaded id walk. Contracts on arch **Loaded descendant id list**; Seam **Loaded descendant id list**.

1. [ ] Flat id list from Zoom root — given Graph and start NodeId, return flat `NodeId` list including the start Node
2. [ ] Loaded children only — recurse only through `childrenStatus = Loaded`; do not descend into `Unloaded`
3. [ ] Ownership ignored — do not filter on Owner vs other child kinds
4. [ ] Ids only — result is ids, not a Graph or edges; reusable by Browser Command `graphIds` and later callers

### 2. Browser Run

Client trigger. Contracts on arch **Browser Run**.

1. [ ] `?` trigger — when existing Browser Run and current Node text starts with literal `?` (no trim/normalize; no role/Kind/CSS trigger), send one-Node Command
2. [ ] Same Node roles — current Node is Command, Zoom root, and Focus
3. [ ] Payload fields — caller credentials plus `zoomId`, `focusId`, `commandId`, and `graphIds` from Loaded descendant id list at that Zoom root
4. [ ] Non-`?` unchanged — text not starting with `?` stays AmbleRun
5. [ ] No new chrome — reuse existing Run UI; add no new control

### 3. HTTP Adapter

Command transport. Contracts on arch **HTTP Adapter**.

1. [ ] Encode Command + credentials — decode Browser Command; transport fields `zoomId`, `focusId`, `commandId`, `graphIds` match Core door and CoreActorPool.startActor
2. [ ] Call Core door — call Core through CoreMailbox or CoreRuntime-bound members (`startActor`)
3. [ ] Universal response — encode `{ nodes; events; latestId }` for Command when that path is exercised
4. [ ] Adapter stays decode and status — admission and Actor selection are not re-implemented in the Adapter

### 4. CoreMailbox / CoreMsg / CoreActorPool / TestActor / History

Production path through modules already proven outside by ticket 34. Contracts on arch **CoreMailbox**, **CoreMsg / CoreMailboxBackend**, **CoreActorPool**, **TestActor**, **History**.

1. [ ] StartActor through door — CoreMailbox `startActor` → CoreMsg validates and hands off → CoreActorPool.startActor
2. [ ] `hello` lifecycle — select `test`, create, ActorStarted, schedule; TestActor `hello`; admit-before-`PostChange`; `ActorStop ActorSucceeded`; History ActorFinished
3. [ ] Browser-visible result — one Owned child text `hello` under Focus reaches the Browser via the universal response / existing sync path

### 5. Manual proof

1. [ ] Server + Browser — current Node `?test hello`, existing Run → one Owned child text `hello`
2. [ ] No live Agent — manual proof uses no live external Agent service or key

## 3. See also

[[plan/core-creation/arch.md|Core creation architecture]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], [[34-outside-core-lifecycle-proof.md]], [[29-prove-testactor-hello.md]], [[33-credentialed-browser-change-posts.md]], [[Implementation Planning and Record.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]

## 4. Comments

- 2026-09-14 — Filed via `/to-tickets` for arch Story path **Browser Run hello** (tracer-cut). Blocked by [[34-outside-core-lifecycle-proof.md|Outside Core lifecycle proof]]. Parent [[29-prove-testactor-hello.md|Prove TestActor hello]] left unchanged per no-retrofit.
