# 35b — Browser Run hello

**Status:** coded
**Blocked by:** None — [34b — Outside Core lifecycle proof](34b-outside-core-lifecycle-proof.md) is `done`.
Actual: 7h

## Context

The outside-Core proof establishes the TestActor `hello` lifecycle without Browser HTTP. This ticket connects that lifecycle to the existing Browser Run action. When the current Node text starts with `?`, Run sends a one-Node Command through HTTP and Core. The Browser then shows one Owned child with text `hello` under the current Focus.

This ticket follows Story path **Browser Run hello** in [Core creation architecture](plan/core-creation/arch.md). The architecture Module map owns State, Interface, and Uses details. Client supplies `graphIds` for the unfolded Included context; server does not Zoom-expand or Fold-walk for Actor start.

## What to build

Make the existing Browser Run action complete one successful `?test hello` path. The Browser creates the Command request and credentials, the HTTP Adapter calls the CoreMailbox door, Core runs TestActor through the ordered lifecycle, and the universal response lets the Browser show the resulting Owned child. Keep non-`?` Run behavior unchanged.

### 1. Unfolded Included context id list

Provide the shared id list used by Browser Command requests. The Client supplies `graphIds` from the unfolded Included context (Zoom + unfolded children), not a server-side Zoom-expand or Fold-walk. Follow module **Loaded descendant id list** in [Core creation architecture](plan/core-creation/arch.md).

1. [x] Include Zoom root — return a flat NodeId list that starts with the requested Zoom root.
2. [x] Walk unfolded children — recurse through unfolded child lists (Included context) and include every child id.
3. [x] Stop at folded children — do not descend through folded child lists.
4. [x] Ignore ownership — do not filter or branch on child ownership.
5. [x] Return ids only — return no Graph, edge, or ownership data.

### 2. Browser Run

Turn the existing Run action into the Browser entry point for a Command. Follow module **Browser Run** in [Core creation architecture](plan/core-creation/arch.md).

1. [x] Detect Command text — when the current Node text starts with literal `?`, use the Command path.
2. [x] Use one Node — use the current Node as Command, Zoom root, and Focus.
3. [x] Send the request — include caller credentials, `zoomId`, `focusId`, `commandId`, and the unfolded Included context `graphIds` (Client supplies this; server does not Zoom-expand or Fold-walk).
4. [x] Preserve AmbleRun — keep existing behavior for text that does not start with `?`.

### 3. HTTP Adapter

Carry the Command request across the transport boundary. Follow module **HTTP Adapter** in [Core creation architecture](plan/core-creation/arch.md).

1. [x] Decode Command and credentials — accept the Browser request with the same named ids used by Core.
2. [x] Call CoreMailbox — submit the request through `startActor`.
3. [x] Encode the universal response — return `{ nodes; events; latestId }` when this Command path runs.

### 4. CoreMailbox / CoreMsg / CoreActorPool

Run start, output admission, and stop through the Core modules named in [Core creation architecture](plan/core-creation/arch.md).

1. [x] Start through CoreMailbox — validate the caller and secret before handing StartActor to CoreActorPool.
2. [x] Start through CoreActorPool — expand `graphIds` to a Graph, select registered Actor `test` (the Actor selection, distinct from interpreting `hello` from command text), create the live row, append ActorStarted, and schedule the body; register finishes before the mailbox starts; Core owns the pool; Actors are injected at startup.
3. [x] Unregistered Actor name — first token after `?` that is not a registered Actor name fails start/Command (not a silent no-op, not AmbleRun). Membership in the registered set; not a special-case string.
4. [x] Admit before PostChange — accept TestActor output only while its live identity and secret are valid.
5. [x] Finish after output — process `ActorStop ActorSucceeded` after the Actor Change.

### 5. TestActor / History / CoreRuntime

Produce `hello`, record the lifecycle, and compose the registered Actor. Follow the named modules in [Core creation architecture](plan/core-creation/arch.md).

1. [x] Register TestActor — make Actor name `test` available at startup (Core owns pool; Actors injected at startup).
2. [x] Interpret hello — read the command Node and select the `hello` behavior (interpretation is distinct from Actor selection of `test`).
3. [x] Non-hello command — after Actor `test` is selected, any command text that is not `hello` fails as `ActorFailed`; no Owned child; no empty `ActorSucceeded`. Allowed-command match; not a special-case string.
4. [x] Post the child — add one Owned child with text `hello` under Focus.
5. [x] Stop successfully — queue `ActorStop ActorSucceeded` after the Change.
6. [x] Record completion — append one ActorFinished and remove the live row and secret (Events via lifecycle/History; getState / State returns Graph, not Events).

### 6. History durability — moved

Moved to [46 — Mailbox History durability](46-mailbox-history-durability.md). Not required for [§7 Browser proof](35b-browser-run-hello.md).

### 7. Browser proof

Verify the complete Story path from the user-visible boundary. Does **not** require [46 — Mailbox History durability](46-mailbox-history-durability.md).

1. [x] Run hello — use existing Browser Run on a current Node with text `?test hello`.
2. [x] Show one result — show one Owned child with text `hello` under the current Focus.
3. [x] Use no Agent service — complete the proof without a live external Agent service or key.

## See also

[Core creation architecture](plan/core-creation/arch.md)

[Implementation Planning and Record](Implementation Planning and Record.md)

## Comments

- 2026-09-18 — [§6 History durability](35b-browser-run-hello.md) split to [46 — Mailbox History durability](46-mailbox-history-durability.md). [§7 Browser proof](35b-browser-run-hello.md) can run without 46.
- 2026-09-14 — Updated to align with Alan's locks: Client supplies `graphIds` from unfolded Included context (Fold); server does not Zoom-expand or Fold-walk. The walk is **unfolded vs folded** (Included context / Fold), **not** loaded vs unloaded residency. Actor select `test` is distinct from interpreting `hello` from command text. Register-then-start; Core owns pool; Actors injected at startup. getState / State = Graph; Events via lifecycle/History. Arch module still named "Loaded descendant id list" — rename debt to "Unfolded Included context id list" or similar when arch is next edited for this Project.
- 2026-09-14 — Added §6 History durability (persist/load mailbox History for restart survival); moved off 34b where mailbox History was process-lifetime only.
- 2026-09-18 — Slice 1 coded. [§1 Unfolded Included context id list](35b-browser-run-hello.md) is implemented in [IncludedDescendantIds](src/Shared/IncludedDescendantIds.fs): `expand` walks SiteMap Fold (not `childrenStatus` residency). Shared.Tests green. Whole ticket Status stays `ready-for-agent`. Report: [35b slice 1 graphIds](plan/core-creation/reports/35b-slice1-graphids.md).
- 2026-09-18 — Slices 2–3 coded. [§2 Browser Run](35b-browser-run-hello.md) and [§3 HTTP Adapter](35b-browser-run-hello.md) are implemented: `?` Run builds one-Node [ActorStart](src/Shared/History.fs) with [IncludedDescendantIds](src/Shared/IncludedDescendantIds.fs) `graphIds`; POST `/ambit/command` decodes cookie Caller + named ids, calls [CoreMailbox.startActor](src/Server/Core/CoreMailbox.fs), returns `{ nodes; events; latestId }`. Did not register TestActor at production boot. Did not implement [§4 CoreMailbox / CoreMsg / CoreActorPool](35b-browser-run-hello.md), [§5 TestActor / History / CoreRuntime](35b-browser-run-hello.md), [§6 History durability](35b-browser-run-hello.md), or headed [§7 Browser proof](35b-browser-run-hello.md). Whole ticket Status stays `ready-for-agent`. Report: [35b slices 2–3 HTTP and Browser Run](plan/core-creation/reports/35b-slice2-3-http-browser-run.md).
- 2026-09-18 — Slices 4+5+7 coded. [§4 CoreMailbox / CoreMsg / CoreActorPool](35b-browser-run-hello.md) fills Actor select from `?test hello` and `fromExtracted` for Browser `graphIds`. [§5 TestActor / History / CoreRuntime](35b-browser-run-hello.md) moves [TestActor](src/Server/TestActor.fs) into Server and injects Actor `test` at production boot. [§6 History durability](35b-browser-run-hello.md) stays on [46 — Mailbox History durability](46-mailbox-history-durability.md). [§7 Browser proof](35b-browser-run-hello.md) is the production HTTP Run of `?test hello` plus headed `:5215` when the env can host it. Status `coded`. Report: [35b slices 4+5+7 Core TestActor and Browser proof](plan/core-creation/reports/35b-slice4-5-7-core-testactor-browser-proof.md).
- 2026-09-18 — Origin Spec review corrections. [§4 CoreMailbox / CoreMsg / CoreActorPool](35b-browser-run-hello.md) Unregistered Actor name: first token after `?` that is not a registered Actor name fails start/Command (registered-set membership; examples `?unknown` / `?nope`). [§5 TestActor / History / CoreRuntime](35b-browser-run-hello.md) Non-hello command: after Actor `test` is selected, any command text that is not `hello` is `ActorFailed` with no Owned child (allowed-command match; examples `?test unknown` / `?test nope`). Production [TestActor](src/Server/TestActor.fs) drops the 34b `throw` / `failwith` / try-with harness. Status stays `coded`. Report: [35b slices 4+5+7 standards and Spec corrections](plan/core-creation/reports/35b-slice4-5-7-standards-spec-corrections.md).

## Time

- 2026-09-18 1h30m — Slice 1 Shared `graphIds` Fold walk + Shared.Tests (from chat)
- 2026-09-18 2h — Slices 2–3 HTTP Adapter + Browser Run Command path (from chat)
- 2026-09-18 2h — Slices 4+5+7 Core TestActor register, `?test hello` parse, production HTTP proof (from chat)
- 2026-09-18 1h30m — Origin Spec review: unregistered Actor name and non-hello command fail; TestActor no exception path (from chat)
- Actual: 7h
