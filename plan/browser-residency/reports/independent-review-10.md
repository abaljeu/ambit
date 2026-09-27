# Independent code review: 10 — Migrate Browser Poll, post-Event, and Boot

Range: `origin/staging...HEAD`

Sources: [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md), [Browser residency architecture](plan/browser-residency/arch.md), and [Browser residency specification](plan/browser-residency/spec.md).

## 1. Verdict — Must-fixes

1. **Good — Browser residency behavior** — Poll is a POST and Poll plus every post-Event path computes `Want.compose` from the current ViewModel at send time, including `[]`. Boot restores local Zoom and Fold before its Poll computes Want. Poll, post-Event, boot, and explicit Load consume edges plus Nodes. SyncPlanner remains flight-only, Load remains wired, and Find remains residence-only.
2. **Must-fixes — One Standards finding** — Replace the one-off submit-reconciliation tuple and its related Boolean parameter with one named cohesive result type.
3. **Nice-to-haves — One Spec finding** — Keep ticket 10’s answer-only adaptation in Browser code rather than extending the Shared apply surface owned by 08 — Migrate Shared wire.

## 2. Standards — One must-fix

### 2.1 Name the submit-reconciliation result

[F# source rules](.agents/rules/fsharp-source.md) require related parameters to use a named reused type and say not to invent a one-off tuple. [Browser Update](src/Client/Update.fs) adds `reconcileSubmit : bool * AckReconcile`, then passes that Boolean as `useExternal` beside the related `AppliedSubmit` to `finishAppliedSubmit`. Put `useExternal` and the reconciliation outcome in one named result shape, or carry `useExternal` on the existing `AppliedSubmit` shape, so this flow has one cohesive interface.

### 2.2 Nice-to-haves — None

No separate Standards nice-to-have survives review. The repeated Poll branches have different failure and follow-up behavior, so this report does not require a speculative shared helper.

### 2.3 Good — Mechanical standards

The mechanical scan found no binding over 40 lines and no added F# line over 100 characters. The smell baseline produced no separate judgment-call finding.

## 3. Spec — No must-fixes

### 3.1 Must-fixes — None

No required Browser behavior is missing or wrong.

### 3.2 Nice-to-have — Keep Shared apply ownership on 08 — Migrate Shared wire

[10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md) says “Shared apply stays on 08” and assigns this batch Browser transport. This range adds `SyncLogic.applySyncAnswer`, `changeSuccessAnswerToSync`, and `applyChangeSuccessAnswer` in [Shared SyncLogic](src/Shared/SyncLogic.fs), plus a Shared test. These are thin answer-only wrappers over the existing ticket-08 apply path, so they do not change behavior, but they extend the Shared interface from ticket 10. Prefer a Browser-local adapter that clears Events and calls the existing Shared apply door.

### 3.3 Good — Focus checks

1. **Poll and post-Event Want — Good** — [Browser App](src/Client/App.fs) POSTs Poll and builds Poll and Change requests through `currentPollRequest` and `currentChangeRequest`; [Browser Update helpers](src/Client/UpdateHelpers.fs) calls `Want.compose` from the current Graph, SiteMap, and Zoom, so empty and repeated Wants are sent without command or click state.
2. **Boot restore and Want — Good** — [Browser Program](src/Client/Program.fs) dispatches State first; the runtime restores session Zoom and Fold synchronously, then `runBootPoll` reads the restored ViewModel and sends its Want. The Browser consumes only the State Graph supplied by the Server and does not expand it locally before restore.
3. **SyncPlanner and Load — Good** — SyncPlanner flight effects remain unchanged. `runLoadServer` maps the current wire `nodes` and `childMap` answer through `loadResponseToSync`, and `LoadDone` installs those fields. The legacy `LoadResponse` member names remain only for [12 — Contract old Load Fetch packages](plan/browser-residency/issues/12-contract-old-load-fetch-packages.md). The Load command and hollow-circle path remain wired.
4. **Find — Good** — The range does not change Find, add Server Find, or make Find commit run Load.
5. **Scope — Good outside the noted wrapper** — No Server file or ticket-09 production door changes. No Bullet or Included reader work from 11 — Migrate Bullet and Included readers and no legacy Load-field contraction from 12 — Contract old Load Fetch packages enters this range.

## 4. Verification — Focused proof passes

1. **Shared proof — Good** — `dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj -c Debug --filter "FullyQualifiedName~SyncLogicTests|FullyQualifiedName~WantTests|FullyQualifiedName~SerializationTests|FullyQualifiedName~SyncPlannerTests" --verbosity normal` passed 135 of 135 tests.
2. **Browser proof — Good** — `source scripts/client.sh build` completed the Fable compile and esbuild bundle with no error.
3. **Diff proof — Good** — `git diff --check origin/staging...HEAD` and the report-only whitespace check passed.

## 5. Status — Coded

[10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md) stays `coded`. This report is not approval.

## 6. Summary — One Standards, one Spec

Standards has one finding; the worst issue is the one-off submit-reconciliation tuple interface. Spec has one nice-to-have finding; the worst issue is the small Shared apply-surface extension owned by 08 — Migrate Shared wire.
