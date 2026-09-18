# Code review — 04 Write core-creation arch.md last

Independent review. Not approval. Ticket [04 — Write core-creation arch.md last](plan/single-event-source/issues/04-write-core-creation-arch-md-last.md) stays **Status:** `coded`.

Range: `origin/staging...HEAD` (three-dot). Tip `57ba7359`. Base `38e1ef6d`. Non-empty. 4 files. One commit: `Align core-creation arch with Event-only architecture (04)`. Docs/arch only. [plan/core-creation/arch.md](plan/core-creation/arch.md) is in the range.

Mechanical scan (`python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`): `scan: none`. No FILE/LONG/TAB/MUTABLE/BARE_ID/BLANK_BLANK hits.

Alan locks applied: Leave Status `coded`. Not approval. No GitHub PR. Docs/arch only. Hello stories 1–3 stay unless the created architecture changed them. No drive-by rewrite of unrelated Actor design. Labeled links `[label](path)`. Disposable `cursor/*` only. No staging push.

## Standards

Range `origin/staging...HEAD` is not empty (4 files). Tip `57ba7359`. Base `origin/staging` `38e1ef6d`. Mechanical scan: `scan: none`.

**Hard documented violations**

Labeled-link path form ([markdown-writing.md](.agents/rules/markdown-writing.md): `[label](path)`; same-directory uses the file name; other local files use a path from the repo root).

1. New Time line on [04 — Write core-creation arch.md last](plan/single-event-source/issues/04-write-core-creation-arch-md-last.md): `[core-creation arch](../../core-creation/arch.md)`. That is not a repo-root path. Use `plan/core-creation/arch.md`.
2. New `[label](path)` conversions on [core-creation arch](plan/core-creation/arch.md) keep file-relative `issues/…` (not same directory):
   - `[Prove TestActor hello](issues/29-prove-testactor-hello.md)`
   - `[34b — Outside Core lifecycle proof](issues/34b-outside-core-lifecycle-proof.md)`
   - `[35b — Browser Run hello](issues/35b-browser-run-hello.md)`
   - `[One CoreMsg loop parameterized persist](issues/31-one-coremsg-loop-parameterized-persist.md)`
   - `[Move persist agents under CoreMailbox](issues/32-move-persist-agents-under-coremailbox.md)`
   Those targets are `plan/core-creation/issues/…`. Syntax `[[path|label]]` → `[label](path)` is correct; the path form is not.

**Compliant (checked, no hit)**

No new `[[path|label]]`. No wrap / double-blank (scan empty). New prose names tickets ([04 — Write core-creation arch.md last](plan/single-event-source/issues/04-write-core-creation-arch-md-last.md), [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md)) per [refer-by-name.md](.agents/rules/refer-by-name.md). No branch names ([planning-docs.md](.agents/rules/planning-docs.md)). Ticket `**Status:** coded`, `**Actual:** 1.5h` matches `## Time`; [project.md](plan/single-event-source/project.md) `Actual: 21h`, Stage `build` ([issue-tracker.md](doc/agents/issue-tracker.md), [triage-labels.md](doc/agents/triage-labels.md)).

**Pre-existing on a touched line (not introduced)**

[core-creation arch](plan/core-creation/arch.md) **Chosen** still says `tickets 30–32` with no names. The hunk only swapped `PostChange` → `PostEvent`. [core-agent-behavior.md](.agents/rules/core-agent-behavior.md) says do not clean adjacent text.

**Judgement smells (not hard)**

None quoted from a new hunk. Repeated Event-only facts across Feature / stories / Module map / Alternative 6 follow the existing arch shape (not new Duplicated Code). Four-file edit is the ticket (not Shotgun Surgery).

## Spec

Range `origin/staging...HEAD` is non-empty (4 files, tip `57ba7359`). Spec is [04 — Write core-creation arch.md last](plan/single-event-source/issues/04-write-core-creation-arch-md-last.md). Docs/arch only.

[plan/core-creation/arch.md](plan/core-creation/arch.md) matches created Event-only doors in [single-event-source arch](plan/single-event-source/arch.md) / [map](plan/single-event-source/map.md): no leftover `{ id; submissionId; ops }` / `module Change` / `ofChange`/`asChange` / `Revision` / `getRevision` / `postGraphOnlyChange` / `PendingChange`. KEEP names stay (`EventBody.Change`, `ChangeValidation`, `ChangeAmendment`, HTTP Change). Doors: `postEvents` / `postGraphOnly` / `postEvent`; PersistHandlers `getEventId` / `applyEvent` / `snapshotDone`; one serial `eventId`/`getEventId`; builders mint Ev.

Hello 1–3 stay except door hops the created architecture renamed (`PostChange`→`PostEvent`; Story 3 hop 3 `postChange`→`postEvents`). Story 2 hop 6 still “mailbox History”; Story 3 still Browser Change posts / Change submit / encodes Change (KEEP HTTP / EventBody names; Event-only did not rename History). Alternative 1 Actor text is the same except `PostChange`→`PostEvent`.

**(a) Missing or partial**

Spec: “update [[plan/core-creation/arch.md]] to match what was created.” Feature blurb still cites field shapes only for `postEvent` (created doors are also `postEvents` / `postGraphOnly`). Hop 6 “mailbox History” is leftover EventLog naming, not a Change/Revision leftover; sentence 2 keeps hello unless architecture changed them, so this is not a leftover-Change miss.

**(b) Scope creep**

Spec: “update [[plan/core-creation/arch.md]] to match what was created.” Wikilink → `[label](path)` on Prove TestActor hello / 34b / 35b / persist 31–32 is link-style, not Event-only alignment. Status/`Actual`/checkbox 1.3.4 are ticket bookkeeping.

**(c) Looks implemented, looks wrong**

None on leftover vs KEEP or deleted doors. Hello Change wording is HTTP/body, not the deleted record.

## Summary

Standards: 6 hard findings (labeled-link path form: one `../../` Time line, five `issues/…` conversions), 0 judgement smells; worst is the new Time line `[core-creation arch](../../core-creation/arch.md)` plus five `issues/…` links that are not repo-root.

Spec: 2 findings (1 partial Feature-blurb `postEvent`-only field shapes; 1 scope link-style conversions) plus 0 wrong leftover/door claims; worst is the Feature blurb still citing field shapes only for `postEvent`.
