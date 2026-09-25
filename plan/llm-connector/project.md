# llm-connector

Stage: done
Summary: Run an Agent from a Zoom-rooted mixed-format extract and replace Focus Children through ordinary Core Changes.
Updated: 2026-09-25
Started: 2026-09-19
Finished: 2026-09-19
Actual: 35h

## Notes

- 2026-09-25 — Landed [25 — Grok Bot wake auth Bearer](issues/25-grokbot-wake-auth-bearer.md): `applyWakeAuth` sends `Authorization: Bearer {WakeSecret}`. Status `done`. Stage stays `done`.
- 2026-09-25 — Coded [25 — Grok Bot wake auth Bearer](issues/25-grokbot-wake-auth-bearer.md): `applyWakeAuth` sends `Authorization: Bearer {WakeSecret}`. Status `coded`. Stage stays `done`.
- 2026-09-25 — Filed [25 — Grok Bot wake auth Bearer](issues/25-grokbot-wake-auth-bearer.md): lock Ambit → hub wake as `Authorization: Bearer {WakeSecret}`. Settles Unsettled wake-auth on [24 — CloudAgents Grok Bot oneshot stream](issues/24-cloudagents-grokbot-oneshot.md). Status `defined`. Stage stays `done`.
- 2026-09-25 — Landed [24 — CloudAgents Grok Bot oneshot stream](issues/24-cloudagents-grokbot-oneshot.md): sibling `GrokBotRunner` wake + fake stream-until-`RunFinished` + cancel. Cursor `AgentRunner` unchanged. Status `done`. Stage stays `done`.
- 2026-09-25 — Coded [24 — CloudAgents Grok Bot oneshot stream](issues/24-cloudagents-grokbot-oneshot.md): sibling `GrokBotRunner` wake + fake stream-until-`RunFinished` + cancel. Cursor `AgentRunner` unchanged. Status `coded`. Stage stays `done`.
- 2026-09-25 — Landed [23 — Focus stream commit on next <](issues/23-focus-stream-commit-on-next-open.md) on staging: leftover `hold` starting with `<` commits pending. Status `done`. Stage stays `done`.
- 2026-09-25 — Coded [23 — Focus stream commit on next <](issues/23-focus-stream-commit-on-next-open.md): leftover `hold` starting with `<` commits pending. Status `coded`. Stage stays `done`.
- 2026-09-25 — Filed [23 — Focus stream commit on next <](issues/23-focus-stream-commit-on-next-open.md): `<tag>Text<` commits pending (Alan lock 2026-09-24/25). Status `defined`. Stage stays `done`.
- 2026-09-25 — Landed [18 — AI Actor stream](issues/18-ai-actor-stream.md) on staging: Actor consumes CloudAgents stream; pending-buffer `addChild` for `<>` fragments. Status `done`. Stage stays `done`.
- 2026-09-25 — Housekeeping: [17 — CloudAgents Console stream](issues/17-cloudagents-console-stream.md) and [22 — Checked-in cursor-models catalog and Console model/params selection](issues/22-cursor-models-catalog-file.md) already `done` on staging.
- 2026-09-24 — Coded [18 — AI Actor stream](issues/18-ai-actor-stream.md): Actor consumes CloudAgents stream; pending-buffer `addChild` for `<>` fragments; tidy Amb/Plain replace only when no element was committed. Status `coded`. Stage stays `done`.
- 2026-09-23 — Coded [17 — CloudAgents Console stream](issues/17-cloudagents-console-stream.md): DLL `streamUntilComplete` / `setFakeStream`, Cursor SSE, Console deltas. Synthesis [stream-response-switch](reports/stream-response-switch.md). [18 — AI Actor stream](issues/18-ai-actor-stream.md) still `defined`. Status `coded`. Stage stays `done`.
- 2026-09-22 — Closed [22 — Checked-in cursor-models catalog and Console model/params selection](issues/22-cursor-models-catalog-file.md): catalog file + Console `--param` / create `model.params` both landed; synthesis [model-params-parallel-synthesis](reports/model-params-parallel-synthesis.md). Status `coded`. Stage stays `done`.
- 2026-09-22 — Coded catalog half of [22 — Checked-in cursor-models catalog and Console model/params selection](issues/22-cursor-models-catalog-file.md): seeded [cursor-models.json](../../src/CloudAgents/cursor-models.json), fsproj Content copy, removed Development `Models` array. Console selection is sibling-owned. Status `coded`. Stage stays `done`.
- 2026-09-22 — Filed [22 — Checked-in cursor-models catalog and Console model/params selection](issues/22-cursor-models-catalog-file.md): checked-in Cursor models catalog + Console runtime model id / params selection. Status `defined`. Stage stays `done`.
- 2026-09-22 — Landed [21 — Console lists models and fills CLI gaps from appsettings](issues/21-console-lists-models-and-appsettings.md) on staging (Good): Console prints the Cursor catalog at start and fills omitted CLI fields from `appsettings.<level>.json`. Status `done`. Stage stays `done`.
- 2026-09-21 — Coded [21 — Console lists models and fills CLI gaps from appsettings](issues/21-console-lists-models-and-appsettings.md): Console prints the Cursor catalog at start and fills omitted CLI fields from `appsettings.<level>.json`. Status `coded`. Stage stays `done`.
- 2026-09-21 — Filed [21 — Console lists models and fills CLI gaps from appsettings](issues/21-console-lists-models-and-appsettings.md): Console prints the Cursor catalog at start and fills omitted CLI fields from `appsettings.<level>.json`. Status `defined`. Stage stays `done`.
- 2026-09-21 — Landed [19 — AI extract pack is XML with Focus cssClass](issues/19-ai-xml-pack-focus-css.md) on staging (Good): write-only XML CloudAgents document; Focus css class `prompt` on the extract copy. Status `done`. Stage stays `done`.
- 2026-09-21 — Coded [19 — AI extract pack is XML with Focus cssClass](issues/19-ai-xml-pack-focus-css.md): write-only XML CloudAgents document; Focus css class on the extract copy. Status `coded`. Stage stays `done`.
- 2026-09-21 — Filed [19 — AI extract pack is XML with Focus cssClass](issues/19-ai-xml-pack-focus-css.md): CloudAgents document is write-only XML; Focus is css class `prompt` on the extract copy. Status `defined`. Stage stays `done`; this is a follow-up bug-fix.
- 2026-09-21 — Landed [20 — Gitignore Development appsettings](issues/20-gitignore-development-appsettings.md) on staging (Good): Development gitignored like Production; empty placeholders stay in base `appsettings.json`. Status `done`. Stage stays `done`.
- 2026-09-21 — Coded [20 — Gitignore Development appsettings](issues/20-gitignore-development-appsettings.md): Development gitignored like Production; empty placeholders stay in base `appsettings.json`. Status `coded`. Stage stays `done`.
- 2026-09-20 — Filed [17 — CloudAgents Console stream](issues/17-cloudagents-console-stream.md) then [18 — AI Actor stream](issues/18-ai-actor-stream.md): SSE on DLL/Console first; Actor pending-buffer incremental Focus writes second. Status `defined`. Stage stays `done` for the first vertical; these are follow-ups.
- 2026-09-20 — Locked **Focus vs Command on Run** in arch: Focus reply parent may differ from `?` Command; one-Node is hello-only. Client encode: core-creation [51 — Browser Run Focus vs Command](../core-creation/issues/51-browser-run-focus-vs-command.md).
- 2026-09-20 — Follow-up on [16 — AiRepos](issues/16-airepos-from-appsettings.md): `StartArgs` into complete; `AiCommandArgs` for `?ai` parse; AI not Ask in actor facts. Status stays `done`.
- 2026-09-20 — Landed [16 — AiRepos from appsettings](issues/16-airepos-from-appsettings.md) on staging (Good): Server `AiRepos` bind/resolve; `?ai` keyname then optional reponame; no first-repo default; CloudAgents stays settings-blind. Status `coded`. Stage stays `done`.
- 2026-09-20 — Landed [15 — AiKeys from appsettings](issues/15-aikeys-from-appsettings.md) on staging (Good): `AiKeys` bind/resolve; first entry default; `?ai` keyname; `CURSOR_API_KEY` removed. Status `done`. Stage stays `done`.
- 2026-09-19 — Coded [14 — Provider-named AI errors](issues/14-provider-named-ai-errors.md): `ActorFailed of string`; Cursor-named auth message; Client Error labeled AI. Status `done`. Stage stays `done`.
- 2026-09-19 — Filed [14 — Provider-named AI errors](issues/14-provider-named-ai-errors.md) as a follow-up on the done first Agent vertical (Stage stays `done`). Chrome tickets already convey stop results; this ticket names the provider on auth/start failure.
- 2026-09-19 — Run Agent Actor `systemPrompt` is a fixed rules string; Focus in the packed extract is the prompt. CloudAgents `DisplayName` is `AI`.
- 2026-09-19 — Locked AI keys: `AiKeys` section in Server `appsettings*.json` (Production gitignored); `?ai` keyname selects; not DataDir/Graph/cookie.
- 2026-09-19 — Stage `done`: first Agent vertical (08–11, 13) delivered on staging.
- 2026-09-19 — Landed [13 — Vertical proof: Browser Ask from what I see](issues/13-vertical-proof-browser-ask.md) on staging (Good). Status `done`.
- 2026-09-19 — Coded [13 — Vertical proof: Browser Ask from what I see](issues/13-vertical-proof-browser-ask.md): Browser-shaped `?ai` harness, `setFake` Finished, Poll Focus Children, lifecycle drop. Status `coded`.
- 2026-09-19 — Filed [13 — Vertical proof: Browser Ask from what I see](issues/13-vertical-proof-browser-ask.md) (Status `defined`; frontier). Client encode already shared with `?test`.
- 2026-09-19 — Landed AI-Actor Failed preserve ([09](issues/09-agent-failure-preserves-children.md)) on staging (Good). Status `done`.
- 2026-09-19 — Coded [09 — Agent failure preserves children](issues/09-agent-failure-preserves-children.md) AI-Actor erase proof: `setFake` yields Failed; Focus Children preserved; no Error Graph text. Status `coded`.
- 2026-09-19 — CloudAgents `setFake` handler yields `AgentStatus` (`Finished` or `Failed`).
- 2026-09-19 — Cancelled [12 — Replace Focus Children from reply](issues/12-replace-focus-children-from-reply.md): replace landed on 08; nested-tag path abandoned.
- 2026-09-19 — Landed [10 — Cancel by Focus](issues/10-cancel-by-focus.md) on staging (Good). Status `done`.
- 2026-09-19 — Coded [10 — Cancel by Focus](issues/10-cancel-by-focus.md): CoreMailbox `cancelByFocus`, hanging `setFake`, Run Agent cancel token. Status `coded`.
- 2026-09-19 — Landed [08 — Agent ask from what I see](issues/08-agent-ask-from-what-i-see.md) on staging (Good). Status `done`.
- 2026-09-19 — Coded [08 — Agent ask from what I see](issues/08-agent-ask-from-what-i-see.md): `setFake`, Run Agent Actor, Amb pack, Focus-child replace. Status `done`.
- 2026-09-19 — Locked CloudAgents `setFake` on the DLL (`(StartArgs -> AgentStatus) option -> bool`); success tests use fake; live call-reject until API key.
- 2026-09-19 — Landed [11 — Pack extract with Amb (supplied-fragment walk)](issues/11-simple-extract-format.md) on staging (Good). Status `done`.
- 2026-09-19 — Live CloudAgents / API key: until a key exists, only prove call reject; success stays on fake ([[issues/08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]]).
- 2026-09-19 — [09 — Agent failure preserves children](issues/09-agent-failure-preserves-children.md) framework half `done` (TestActor preserve proof). AI-Actor erase proof coded.
- 2026-09-19 — Drop observation: live Focus ids, not secrets ([[issues/09-agent-failure-preserves-children.md|09 — Agent failure preserves children]]).
- 2026-09-19 — Coded [11 — Pack extract with Amb (supplied-fragment walk)](issues/11-simple-extract-format.md): Amb extract-walk write and `Graph.focus`. Status `done`. Stage `build`.
- 2026-09-19 — Replan [11 — Pack extract with Amb (supplied-fragment walk)](issues/11-simple-extract-format.md): Amb extract-walk write; nested-tag abandoned; Fable.SimpleXml rejected. Status `defined`.
- 2026-09-19 — 09 lock: on failure Actor framework does not cause Changes; AI Actor does not erase data (future agentic extensions out of scope).
- 2026-09-19 — `/to-tickets` (tracer-cut): [[issues/08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]] (frontier), [[issues/09-agent-failure-preserves-children.md|09 — Agent failure preserves children]], [[issues/10-cancel-by-focus.md|10 — Cancel by Focus]]. Stage `slice`.
- 2026-09-19 — Arch grill closed: Agent Command `?ai` + args; vertical proof after first implement tickets `defined`; Focus mark spelling deferred to Document ticket; keep CloudAgents DLL; live-Actor chrome on core-creation 21/22 (first Agent vertical = Graph+Poll only).
- 2026-09-19 — Locked Agent Command spelling: `?ai` + optional args invokes Run Agent Actor; args ignored for now.
- 2026-09-19 — Published [[spec.md]] then [[arch.md]] from locked [[issues/06-define-command-run-agent-redesign.md|06]] / [[issues/07-lock-run-agent-architecture.md|07]] (checkboxes for delivered vs open). Stage `arch`.

## Implementation tickets

- [25 — Grok Bot wake auth Bearer](issues/25-grokbot-wake-auth-bearer.md) — Status `done`. Follow-up: lock wake auth as `Authorization: Bearer {WakeSecret}`; replace invented `X-Ambit-Wake-Secret`.
- [24 — CloudAgents Grok Bot oneshot stream](issues/24-cloudagents-grokbot-oneshot.md) — Status `done`. Follow-up: sibling oneshot `GrokBotRunner` (wake ack-only, fake Done, cancel). Wake auth settled on [25 — Grok Bot wake auth Bearer](issues/25-grokbot-wake-auth-bearer.md). Server Actor gbot wiring out of scope.
- [23 — Focus stream commit on next <](issues/23-focus-stream-commit-on-next-open.md) — Status `done`. Follow-up: commit pending when the next `<` arrives; incomplete tag bytes stay in `hold`.
- [18 — AI Actor stream](issues/18-ai-actor-stream.md) — Status `done`. Follow-up: Actor consumes CloudAgents stream; pending-buffer incremental Focus children; tidy replace only when no element was committed.
- [17 — CloudAgents Console stream](issues/17-cloudagents-console-stream.md) — Status `done`. Follow-up: DLL `streamUntilComplete` / `setFakeStream`, Cursor SSE, Console deltas.
- [22 — Checked-in cursor-models catalog and Console model/params selection](issues/22-cursor-models-catalog-file.md) — Status `done`. Follow-up: checked-in `cursor-models.json`; Console loads catalog, validates model/params, emits create `model.params`.
- [21 — Console lists models and fills CLI gaps from appsettings](issues/21-console-lists-models-and-appsettings.md) — Status `done`. Follow-up: Console lists Cursor models at start; CLI gaps from appsettings.
- [20 — Gitignore Development appsettings](issues/20-gitignore-development-appsettings.md) — Status `done`. Follow-up: stop tracking Development so live `AiKeys` stay local.
- [19 — AI extract pack is XML with Focus cssClass](issues/19-ai-xml-pack-focus-css.md) — Status `done`. Follow-up: write-only XML CloudAgents document; Focus css class on the extract copy.
- [16 — AiRepos from appsettings](issues/16-airepos-from-appsettings.md) — Status `done`. Follow-up: Server `AiRepos` from appsettings → `AgentRunner.start` repos; no first-repo default.
- [15 — AiKeys from appsettings](issues/15-aikeys-from-appsettings.md) — Status `done`. Follow-up: Server `AiKeys` from appsettings → `RunnerConfig.ApiKey`; remove `CURSOR_API_KEY` from Run Agent Actor.
- [14 — Provider-named AI errors](issues/14-provider-named-ai-errors.md) — Status `done`. Follow-up: CloudAgents + Actor emit a safe provider-named auth message; Client shows it as Error labeled AI.
- [13 — Vertical proof: Browser Ask from what I see](issues/13-vertical-proof-browser-ask.md) — Status `done`.
- [[issues/11-simple-extract-format.md|11 — Pack extract with Amb (supplied-fragment walk)]] — Status `done`.
- [[issues/08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]] — Status `done`.
- [[issues/12-replace-focus-children-from-reply.md|12 — Replace Focus Children from reply]] — Status `cancelled` (Amb replace on 08; nested-tag abandoned).
- [09 — Agent failure preserves children](issues/09-agent-failure-preserves-children.md) — Status `done`. Report: [framework-failure-preserves-children](reports/framework-failure-preserves-children.md); review: [code-review-framework-failure-preserves-children](reports/code-review-framework-failure-preserves-children.md).
- [[issues/10-cancel-by-focus.md|10 — Cancel by Focus]] — Status `done`.

## Locked Restart

Draft PR #4 (cloud-agent Create slice / POST `/ambit/actors` / Md reply under Focus) was **closed unmerged** on 2026-09-11. [[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]] and the fat vertical slice approach are obsolete as the build frontier.

**Locked design points:** [[reports/agent-redesign-locked-2026-09.md]]

[[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] locks the replacement Agent behavior, including Command-text dispatch and command text `?test hello`. [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] locks the typed boundaries, Event sequence, Authority identities, mailbox lifecycle, recovery, and test seams. The Run Agent Actor orchestrates Document, CloudAgents, and ordinary Core Changes.

The Phase 1 and Phase 1b gates are complete. The Project is at `arch` after publishing spec.md and arch.md; no Agent implementation issue or vertical-proof issue exists yet. The next serial executable work is the provider-neutral Core lifecycle in [[plan/core-creation/issues/Implementation Planning and Record.md]]. After that lifecycle is rebuilt, specify the smallest vendor-neutral CloudAgents implementation increment from the locked behavior and architecture.
