# Spec review: Extract postBrowserChange

Spec: one-file test-harness smell fix (Duplicated Code). Spec question: does the diff only extract the shared envelope without changing seed behavior / public harness face? Range: `origin/staging...HEAD` (`33541cce`). Subject: [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs). No product ticket.

## (a) Missing or partial

None. Required extract is present: private `postBrowserChange` in [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs); call sites `seedAskTree`, `seedCommand`, and `seedBrowserCommand`; envelope `EventId.zero`, `Authority "Browser"`, empty `commandName`, `postGraphOnly` + `requireOk`. Spec: "Ops/return shapes unchanged." Each seed keeps its Op list and return value. Spec: "`postOwnedChild` stays on `postEvents`." That function is not in the diff and still calls `CoreMailbox.postEvents`. Public names `seedAskTree`, `seedCommand`, `postOwnedChild`, and `launchBrowserAsk` stay.

## (b) Behaviour the spec did not ask

None. The three-dot diff touches only [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs). `seedCommand` names a local `ops` list so it can pass the same two Ops to the helper; that is the envelope extract, not extra seed behavior. `postOwnedChild` does not call `postBrowserChange`.

## (c) Implemented but wrong

None. Spec: "Envelope: `EventId.zero`, `Authority "Browser"`, empty `commandName`, `postGraphOnly` + `requireOk`." The helper builds that Event (`submissionId = Guid.NewGuid()`, `body = EventBody.Change ops`), posts with `CoreMailbox.postGraphOnly host testCaller event |> Async.StartAsTask`, then `requireOk label posted |> ignore`. Call-site labels stay `"seed"`, `"seed command"`, and `"seed Browser Command"`. `seedBrowserCommand` stays private. The extract answers the spec question: shared envelope only; seed behavior and public harness face unchanged.
