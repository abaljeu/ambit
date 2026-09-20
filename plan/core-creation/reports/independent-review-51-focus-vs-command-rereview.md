# Independent re-review — [51 — Browser Run: Focus reply parent, Command is runnable ancestor](plan/core-creation/issues/51-browser-run-focus-vs-command.md)

Reviewer did not write the implementation. Axis files [Standards re-review — Browser Run Focus vs Command](code-review-standards-51-focus-vs-command-rereview.md) and [Spec re-review — Browser Run Focus vs Command](code-review-spec-51-focus-vs-command-rereview.md) are parallel opinions. This report is not approval. Ticket Status stays `coded`. Do not land onto staging or ready.

**Range:** `origin/staging...HEAD` at `1186d6177df05ea37a5bd93be50d75003f1a8423`. Base `origin/staging` `b484d99ef9b3afd76dc706b740f4a5a35f5c9144`. Command: `git diff origin/staging...HEAD`. Diff is non-empty (13 files, +611 / −31). Draft PR [51: Focus vs Command on product Run](https://github.com/abaljeu/ambit/pull/81).

**Spec:** [51 — Browser Run: Focus reply parent, Command is runnable ancestor](plan/core-creation/issues/51-browser-run-focus-vs-command.md) after Alan’s lock. [core-creation architecture](plan/core-creation/arch.md) **Browser Run** product item. [llm-connector architecture](plan/llm-connector/arch.md) Locked **Focus vs Command on Run**. Scan-stop = first Focus→zoom `?` or `=`. Actor Command = `?` only. `=` = Amble Run only; not ActorStart.

**Prior review:** [Independent review — 51 Focus vs Command](independent-review-51-focus-vs-command.md) Spec Needs work (`=` encoded ActorStart). This re-review does not reuse that axis text.

**Commits** (`origin/staging..HEAD`):

- `1186d617` 51: treat = scan-stop as Amble, not ActorStart.
- `97d68430` 51: independent Standards/Spec review; Status stays coded.
- `6570a866` 51: encode distinct Focus, Command, and Zoom on Run.
- `3c5943a5` 51: Type coding.
- `748702b9` Amend: runnable Command is ? or contains =; scan stops.
- `bdceba60` plan-or-doc-change: Focus vs Command on product Run.

Focused tests (Spec axis): `CommandRequestTests` 18 passed; `TestActorHelloTests` 9 passed.

## Standards

Standards axis only. Range as pinned. Scan command: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (`python` is not on PATH).

### Mechanical scan

```
plan/core-creation/reports/code-review-spec-51-focus-vs-command.md:3  .agents/rules/refer-by-name.md  BARE_ID  #81
plan/core-creation/reports/code-review-spec-51-focus-vs-command.md:5  .agents/rules/refer-by-name.md  BARE_ID  item 3
plan/core-creation/reports/code-review-spec-51-focus-vs-command.md:5  .agents/rules/refer-by-name.md  BARE_ID  item 8
plan/core-creation/reports/code-review-spec-51-focus-vs-command.md:19  .agents/rules/refer-by-name.md  BARE_ID  item 8
plan/core-creation/reports/code-review-spec-51-focus-vs-command.md:19  .agents/rules/refer-by-name.md  BARE_ID  item 4
plan/core-creation/reports/code-review-standards-51-focus-vs-command.md:8  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
plan/core-creation/reports/code-review-standards-51-focus-vs-command.md:9  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
plan/core-creation/reports/code-review-standards-51-focus-vs-command.md:10  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
plan/core-creation/reports/code-review-standards-51-focus-vs-command.md:24  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
plan/core-creation/reports/independent-review-51-focus-vs-command.md:25  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
plan/core-creation/reports/independent-review-51-focus-vs-command.md:26  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
plan/core-creation/reports/independent-review-51-focus-vs-command.md:27  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
plan/core-creation/reports/independent-review-51-focus-vs-command.md:41  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
plan/core-creation/reports/independent-review-51-focus-vs-command.md:88  .agents/rules/refer-by-name.md  BARE_ID  item 8
plan/core-creation/reports/independent-review-51-focus-vs-command.md:88  .agents/rules/refer-by-name.md  BARE_ID  item 4
plan/core-creation/reports/independent-review-51-focus-vs-command.md:96  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
plan/core-creation/reports/plan-or-doc-change-51-focus-vs-command.md:30  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
plan/core-creation/reports/plan-or-doc-change-51-focus-vs-command.md:44  .agents/rules/refer-by-name.md  BARE_ID  ticket 51
plan/core-creation/reports/plan-or-doc-change-51-focus-vs-command.md:48  .agents/rules/refer-by-name.md  BARE_ID  ticket 51
tests/Server.Tests/TestActorHelloTests.fs  .agents/rules/fsharp-source.md  FILE 481->538  already over 400 or new file over 400; change increased it
--- measure-fs-size ---
src/Client/Commands.fs::execAmbleRunOp: lines 62-85 (24 lines)
src/Shared/CommandRequest.fs::isScanStopText: lines 12-14 (3 lines)
src/Shared/CommandRequest.fs::noRunnableCommand: lines 45-47 (3 lines)
src/Shared/CommandRequest.fs::ownerPathToZoom: lines 48-65 (18 lines)
src/Shared/CommandRequest.fs::firstScanStop: lines 66-72 (7 lines)
src/Shared/CommandRequest.fs::nodeText: lines 73-75 (3 lines)
src/Shared/CommandRequest.fs::scanStopOnOwnerPath: lines 77-84 (8 lines)
src/Shared/CommandRequest.fs::commandOnOwnerPath: lines 86-97 (12 lines)
src/Shared/CommandRequest.fs::isAmbleScanStop: lines 99-106 (8 lines)
src/Shared/CommandRequest.fs::tryStart: lines 108-124 (17 lines)
```

### Hard violations

**Refer by name** ([.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md)): scan BARE_ID hits on the first-review reports and [plan-or-doc-change-51-focus-vs-command](plan/core-creation/reports/plan-or-doc-change-51-focus-vs-command.md). Extra: [llm-connector project](plan/llm-connector/project.md) Note `[51](...)` and the plan-or-doc-change Scope ticket line are number-only. Ticket See also `[llm-connector 06]` / `[llm-connector 08]` omit issue names.

**Labeled links** ([.agents/rules/markdown-writing.md](.agents/rules/markdown-writing.md)): [core-creation project](plan/core-creation/project.md) Filed note uses Obsidian `[[path|label]]`.

### Not-hit

FILE 481→538 on [TestActorHelloTests](tests/Server.Tests/TestActorHelloTests.fs): [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) 800/400 does not apply to tests. Measured bindings stay under 40 lines. Surgical under-100-line preference is not a script fail ([.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md)). [.agents/rules/core-api.md](.agents/rules/core-api.md) unused. Stage stays `build`. [no-retrofit](.agents/rules/no-retrofit.md) holds.

### Judgement-call smells

**Feature Envy / custom walk:** `ownerPathToZoom` walks owner parents. [GraphQuery.enclosing](src/Shared/GraphQuery.fs) has no Zoom bound, so not a hard fail.

**Duplicated Code:** `isAmbleScanStop` walks the owner path twice (`scanStopOnOwnerPath` and `commandOnOwnerPath`).

**Parameter Explosion:** `tryStart` takes five arguments. `ActorStart` is the output. Nit.

**Mysterious Name:** Public `tryStart` is one word; `CommandRequest.tryStart` supplies context. Nit.

Standards axis: hard hits are refer-by-name / labeled-link nits (scan BARE_ID on first-review and plan-or-doc reports, plus nameless `[51]` and Obsidian labeled link). Judgement 4. Worst: refer-by-name BARE_ID on planning/review Markdown.

**Verdict (Standards):** Approve with nits.

## Spec

Spec: locked ticket What to build plus **Browser Run** product item and Locked **Focus vs Command on Run**. `=` is Amble Run, not ActorStart.

### (a) Missing or partial

None. Client Run sets `focusId` from selection Focus, `commandId` from a `?` scan-stop only, `zoomId` to the Included Zoom root. Scan still stops at first `?` or `=`. `?` → `tryStart` Ok and `SubmitCommand`. `=` stop → `isAmbleScanStop` / `execAmbleRunOp`; `tryStart` Error; no `SubmitCommand`. Shared `count=1+2` proofs cover Focus-on-equals and Focus-under-equals. Server hello still places Owned children under the question Focus. Plan/arch/ticket updated.

### (b) Behaviour not asked for

None in product code. `execAmbleRunOp` is the prior Amble body. `oneNodeStart` stays for hello. Non-goals (Amb write algorithm, Actor live label TitleCase, Core door widen) are not in the code diff.

### (c) Implemented but wrong

None. `commandOnOwnerPath` returns the scan-stop only when text starts with `?`. Text that starts with `?` and also contains `=` stays Actor Command (arch **Browser Run** item 3: “If that node starts with `?`, it is the Actor Command”).

Spec axis: (a) 0 / (b) 0 / (c) 0. Worst: none.

**Verdict (Spec):** Approve.

## Summary

Standards: hard nits are name/link only; 4 judgement; worst: BARE_ID on first-review and plan-or-doc Markdown. Spec: 0 missing, 0 creep, 0 wrong; worst: none.

**For Alan:** Approve with nits (Standards docs only). Spec Approve — the first-review `=` ActorStart miss is closed. Status stays `coded`. Do not squash-land.
