# 35b — Browser Run hello

**Status:** defined
**Blocked by:** None — [[34b-outside-core-lifecycle-proof.md|34b — Outside Core lifecycle proof]] is `done`.

## Context

The outside-Core proof establishes the TestActor `hello` lifecycle without Browser HTTP. This ticket connects that lifecycle to the existing Browser Run action. When the current Node text starts with `?`, Run sends a one-Node Command through HTTP and Core. The Browser then shows one Owned child with text `hello` under the current Focus.

This ticket follows Story path **Browser Run hello** in [[plan/core-creation/arch.md|Core creation architecture]]. The architecture Module map owns State, Interface, and Uses details. Client supplies `graphIds` for the unfolded Included context; server does not Zoom-expand or Fold-walk for Actor start.

## What to build

Make the existing Browser Run action complete one successful `?test hello` path. The Browser creates the Command request and credentials, the HTTP Adapter calls the CoreMailbox door, Core runs TestActor through the ordered lifecycle, and the universal response lets the Browser show the resulting Owned child. Keep non-`?` Run behavior unchanged.

### 1. Unfolded Included context id list

Provide the shared id list used by Browser Command requests. The Client supplies `graphIds` from the unfolded Included context (Zoom + unfolded children), not a server-side Zoom-expand or Fold-walk. Follow module **Loaded descendant id list** in [[plan/core-creation/arch.md|Core creation architecture]].

1. [ ] Include Zoom root — return a flat NodeId list that starts with the requested Zoom root.
2. [ ] Walk unfolded children — recurse through unfolded child lists (Included context) and include every child id.
3. [ ] Stop at folded children — do not descend through folded child lists.
4. [ ] Ignore ownership — do not filter or branch on child ownership.
5. [ ] Return ids only — return no Graph, edge, or ownership data.

### 2. Browser Run

Turn the existing Run action into the Browser entry point for a Command. Follow module **Browser Run** in [[plan/core-creation/arch.md|Core creation architecture]].

1. [ ] Detect Command text — when the current Node text starts with literal `?`, use the Command path.
2. [ ] Use one Node — use the current Node as Command, Zoom root, and Focus.
3. [ ] Send the request — include caller credentials, `zoomId`, `focusId`, `commandId`, and the unfolded Included context `graphIds` (Client supplies this; server does not Zoom-expand or Fold-walk).
4. [ ] Preserve AmbleRun — keep existing behavior for text that does not start with `?`.

### 3. HTTP Adapter

Carry the Command request across the transport boundary. Follow module **HTTP Adapter** in [[plan/core-creation/arch.md|Core creation architecture]].

1. [ ] Decode Command and credentials — accept the Browser request with the same named ids used by Core.
2. [ ] Call CoreMailbox — submit the request through `startActor`.
3. [ ] Encode the universal response — return `{ nodes; events; latestId }` when this Command path runs.

### 4. CoreMailbox / CoreMsg / CoreActorPool

Run start, output admission, and stop through the Core modules named in [[plan/core-creation/arch.md|Core creation architecture]].

1. [ ] Start through CoreMailbox — validate the caller and secret before handing StartActor to CoreActorPool.
2. [ ] Start through CoreActorPool — expand `graphIds` to a Graph, select registered Actor `test` (the Actor selection, distinct from interpreting `hello` from command text), create the live row, append ActorStarted, and schedule the body; register finishes before the mailbox starts; Core owns the pool; Actors are injected at startup.
3. [ ] Admit before PostChange — accept TestActor output only while its live identity and secret are valid.
4. [ ] Finish after output — process `ActorStop ActorSucceeded` after the Actor Change.

### 5. TestActor / History / CoreRuntime

Produce `hello`, record the lifecycle, and compose the registered Actor. Follow the named modules in [[plan/core-creation/arch.md|Core creation architecture]].

1. [ ] Register TestActor — make Actor name `test` available at startup (Core owns pool; Actors injected at startup).
2. [ ] Interpret hello — read the command Node and select the `hello` behavior (interpretation is distinct from Actor selection of `test`).
3. [ ] Post the child — add one Owned child with text `hello` under Focus.
4. [ ] Stop successfully — queue `ActorStop ActorSucceeded` after the Change.
5. [ ] Record completion — append one ActorFinished and remove the live row and secret (Events via lifecycle/History; getState / State returns Graph, not Events).

### 6. History durability

Make mailbox-owned History survive process restart.

1. [ ] Persist mailbox History — save Change and Actor Events (ChangeEvent, ActorStarted, ActorFinished) so the audit sequence survives restart.
2. [ ] Load mailbox History — restore the past/future sequence on mailbox startup.
3. [ ] Graph/EventLog durability separate — Graph and EventLog persist stays distinct; this slice covers mailbox History only.
4. [ ] Undo stays Change-only — do not make Actor lifecycle Events Undo targets; only Changes are undoable.

### 7. Browser proof

Verify the complete Story path from the user-visible boundary.

1. [ ] Run hello — use existing Browser Run on a current Node with text `?test hello`.
2. [ ] Show one result — show one Owned child with text `hello` under the current Focus.
3. [ ] Use no Agent service — complete the proof without a live external Agent service or key.

## See also

[[plan/core-creation/arch.md|Core creation architecture]]

[[Implementation Planning and Record.md|Implementation Planning and Record]]

## Comments

- 2026-09-14 — Updated to align with Alan's locks: Client supplies `graphIds` from unfolded Included context (Fold); server does not Zoom-expand or Fold-walk. The walk is **unfolded vs folded** (Included context / Fold), **not** loaded vs unloaded residency. Actor select `test` is distinct from interpreting `hello` from command text. Register-then-start; Core owns pool; Actors injected at startup. getState / State = Graph; Events via lifecycle/History. Arch module still named "Loaded descendant id list" — rename debt to "Unfolded Included context id list" or similar when arch is next edited for this Project.
- 2026-09-14 — Added §6 History durability (persist/load mailbox History for restart survival); moved off 34b where mailbox History was process-lifetime only.
