# Independent review — 08 — Migrate Shared wire

Range: three-dot diff of this PR against its base. One commit: Install Want answer after Event tail on Shared sync apply. Ticket [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md) is **Status:** `coded`. Axes: [Standards review 08](plan/browser-residency/reports/standards-review-08.md), [Spec review 08](plan/browser-residency/reports/spec-review-08.md).

## Verdict

**Good.** No must-fixes. Ticket Status stays `coded` until review approval.

## Claims

1. **SyncResponse carries Want answer** — Confirmed. [ApiResponses.fs](src/Shared/ApiResponses.fs) `SyncResponse` adds `nodes` and `childMap` beside Load `packages`. `changeSuccessToSync` copies them from `ChangeSuccessResponse`. `loadResponseToSync` sends empty Want fields.
2. **applySyncResponse = Events → installWantAnswer → packages** — Confirmed. [SyncLogic.fs](src/Shared/SyncLogic.fs) folds the Event tail, then `graphAfterWant` (`ResidentProjection.installWantAnswer` unless both Want fields are empty), then `graphAfterPackages`.
3. **App.fs only empty fields for compile** — Confirmed. [App.fs](src/Client/App.fs) fills `nodes = []` and `childMap = Map.empty` on three LoadDone error stubs. It does not attach Want on Poll or post-Event. Production Poll still dispatches `poll.events` only. Client apply still uses `applyServerTail` (Events only) in [Update.fs](src/Client/Update.fs), [Program.fs](src/Client/Program.fs), and [UpdateActorLive.fs](src/Client/UpdateActorLive.fs). That is [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md), not this ticket.
4. **Focused Shared.Tests 110 pass** — Confirmed. `dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter FullyQualifiedName~SyncLogicTests|SerializationTests|LoadCaptureTests|WantTests` — Passed 110, Failed 0, Skipped 0.
5. **Full Shared 1722 pass / 1 skip / 1 AmbDocument fail unrelated** — Confirmed. Full [Gambol.Shared.Tests](tests/Shared.Tests/Gambol.Shared.Tests.fsproj): Passed 1722, Skipped 1 (`WorkspaceLocalInventoryTests.listForPush without git still returns walk`), Failed 1 (`AmbDocumentTests.read ambiguous owner-link candidates keeps map order`, expected a fixed NodeId, got a new one). This PR does not touch AmbDocument tests or `src/Shared/documents`.
6. **Fable compile** — Confirmed with `dotnet fable src/Client --outDir src/Server/wwwroot --sourceMaps` (exit 0). `./scripts/client.sh` was not executable here (exit 126), so the compile gate ran as `dotnet fable` directly.

Ticket bar:

1. **Codecs carry Want without dropping Changes; empty Want `[]`** — Confirmed. Encode/decode for `want` / `nodes` / `childMap` already landed in [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md). This PR adds round-trips that keep Changes beside Want. Empty Want `[]` remains required on Poll and post-Event (`PollRequest always encodes want including empty` still in the 110). Locked wire: Want is a NodeId list; answer is `nodes` + `childMap`; `ApiVersion.current` is 13.
2. **SyncLogic installWantAnswer after Event tail; Load packages still apply** — Confirmed. `loadResponseToSync` still maps `packages` / `packageChildMap`. Dual-run test keeps both Want edges and package snapshots.
3. **Outcome stamps unchanged** — Confirmed. `getPollOutcome` is not in the diff. It still keys on `apiVersion` and event id. New test with Want fields still returns None / DataOutdated / CodeOutdated from those keys only.
4. **Do not migrate 09–11 or contract 12** — Confirmed. No Server door edits. No Browser Want attach. [LoadResponse](src/Shared/ApiResponses.fs) `packages` stay.
5. **Arch/ticket checkmarks honest** — Confirmed. Ticket 08 items are checked. Arch checks only story 21.4, SyncLogic 9.2.2, and the `installWantAnswer` use. Shared segment 2 (Poll and post-Event carry Want) and Browser/Server doors stay unchecked. Ticket comment states 07 already carried the codecs and that this ticket installs the Want answer after the Event tail.

## Standards

Axis report: [Standards review 08](plan/browser-residency/reports/standards-review-08.md). Scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff` against the PR base. Function sizes for `graphAfterWant` (12), `graphAfterPackages` (9), and `changeSuccessToSync` (7) are under the 40-line limit — not findings.

1. **App.fs file length (hard).** [fsharp-source.md](.agents/rules/fsharp-source.md) limits each file to 800 lines. If a file is already longer, restructure only when the change increases the file. Split after the project edits, in a later operation. [App.fs](src/Client/App.fs) grew from 854 lines to 866 lines.
2. **Branch name as delivery status (hard).** [planning-docs.md](.agents/rules/planning-docs.md) records what is implemented by ticket, section, or Point. Do not use git branch names as delivery status in plan docs. The Comments block of [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md) names a long-lived place as the drop for [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md) `done`.
3. **Empty SyncResponse copies (smell: Duplicated Code, judgement).** [SMELLS.md](.agents/skills/code-review/SMELLS.md). [App.fs](src/Client/App.fs) writes the same empty `SyncResponse` three times in `createRuntime`. That shape also appears in [SyncLogic.fs](src/Shared/SyncLogic.fs) `applyServerTail`. The three copies added 12 lines to a file that is already over the limit.

## Spec

Axis report: [Spec review 08](plan/browser-residency/reports/spec-review-08.md).

No spec findings.

## Summary

Standards: 3 findings (worst: [App.fs](src/Client/App.fs) 854→866 over the 800-line file limit). Spec: 0 findings.
