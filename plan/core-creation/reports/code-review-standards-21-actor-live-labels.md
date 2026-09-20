# Code review — Standards — Actor live labels

Range: `origin/staging...HEAD` (merge-base `0c716ef3`, HEAD `d280520a`). Code: [`ActorLive.fs`](src/Shared/ActorLive.fs), [`UpdateActorLive.fs`](src/Client/UpdateActorLive.fs), [`ActorLiveTests.fs`](tests/Shared.Tests/ActorLiveTests.fs), [`AgentAuthErrorTests.fs`](tests/Server.Tests/AgentAuthErrorTests.fs). Plan markdown in the same range.

## Hard violations

**[Refer by name](.agents/rules/refer-by-name.md)** — mechanical scan `BARE_ID` hits in [plan-or-doc-change — Actor live labels](plan-or-doc-change-21-actor-live-labels.md): line 3 `Ticket 50`; line 5 `#79`; line 11 `ticket 21` and `ticket 50`; line 18 `ticket 14`; line 35 `Ticket 21`; line 36 `Ticket 50`; line 46 `#79`. Same rule on other new plan text: [Core creation](../project.md) notes and Issues list say “folded into 21”; [50 — Actor live result labels from Command node](../issues/50-actor-live-labels-from-command.md) has “(14 stays)”, “llm-connector 17/18”, “+ 21 rework”, link label `[21]`, and “21 done”.

**[Markdown writing](.agents/rules/markdown-writing.md)** — labeled links must be `[label](path)`, not Obsidian `[[path|label]]`. New [Core creation](../project.md) notes use `[[issues/…|21]]` and `[[issues/…|50]]` with number-only labels. Changed See also on [21 — Client shows live Actor](../issues/21-client-shows-lock-present.md) uses `[[../arch.md|core-creation architecture]]`. [50 — Actor live result labels from Command node](../issues/50-actor-live-labels-from-command.md) says “Client” for the Browser project ([CONTEXT.md](CONTEXT.md) **Browser**).

**[Planning docs](.agents/rules/planning-docs.md)** — plan text must not discuss branches as delivery. [plan-or-doc-change — Actor live labels](plan-or-doc-change-21-actor-live-labels.md) names workplace branch `cursor/actor-live-cmd-result-label` and “draft PR #79”.

**[F# source](.agents/rules/fsharp-source.md)** — group related parameters into a named reused type. `graph`, `focusId`, and `zoomRoot` travel together through `runnableTextOnPath`, `displayLabel`, and `startResult`; `lastCmdResult` and `resultOf` lengthen the list with `graph` plus `zoomRoot` instead of one scan-context type.

## Judgement (smells)

**Duplicated Code** — `firstToken` in [`ActorLive.fs`](src/Shared/ActorLive.fs) repeats private `firstToken` in [`CommandRequest.fs`](src/Shared/CommandRequest.fs). Test fixtures `graphWithCommand`, `graphWithCommandChild`, and `graphWithCommandMidChild` repeat the same `Graph.replace` / `createNodes` shape.

**Mysterious Name** — private `resultOf` does not say it maps one Event to a `CmdLastResult`; the stop path binds `chip` while start uses `label`.

## Clean

Function sizes are under 40 lines (`resultOf` is 21). No added line over 100 characters. [`ActorLive.fs`](src/Shared/ActorLive.fs) is 164 lines (under 800). No tabs. The owner walk uses `GraphQuery.enclosing` ([F# source](.agents/rules/fsharp-source.md) GraphQuery-first). [`UpdateActorLive.fs`](src/Client/UpdateActorLive.fs) is a one-line call-site change. Tests’ `failwith` matches existing Shared test style. [Core API](.agents/rules/core-api.md) is not in this diff. [No retrofit](.agents/rules/no-retrofit.md): [14 — Provider-named AI errors](../../llm-connector/issues/14-provider-named-ai-errors.md) stays done.
