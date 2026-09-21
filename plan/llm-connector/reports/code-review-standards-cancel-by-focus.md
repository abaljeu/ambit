# Standards review — 10 — Cancel by Focus

Range: `git diff origin/staging...HEAD` (6230600a). Standards only. Ticket: [10 — Cancel by Focus](plan/llm-connector/issues/10-cancel-by-focus.md).

## Hard violations

### File size — [fsharp-source.md](.agents/rules/fsharp-source.md) (800 lines; split when already over 400 and the change increases the file)

- [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) FILE 394→423: already over 400 or new file over 400; change increased it.
- [History.fs](src/Shared/History.fs) FILE 742→743: already over 400 or new file over 400; change increased it.
- Scan also printed [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) 592→594 and [EventTests.fs](tests/Shared.Tests/EventTests.fs) 480→490. File-size does not apply to tests.

### Function size — [fsharp-source.md](.agents/rules/fsharp-source.md) (40 lines per function; tests included)

- [CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs) member `Change before Cancel keeps the accepted children`: lines 270–351 (82 lines).
- Same file member `cancel by Focus accepts Cancelled and drops the live row`: lines 190–230 (41 lines).
- Product `cancelByFocus` (8), `dispatchCancelActor` (24), `pollUntilDone` (26), `waitForCancel` (3): under 40, no mutable.

### Mutable — [fsharp-source.md](.agents/rules/fsharp-source.md) (do not use mutable)

[AgentRunner.fs](src/CloudAgents/AgentRunner.fs) Fake adds `cancelled` and `cancelCount` refs plus `ManualResetEvent` `cancelPulse`, on the existing Fake `ref`/`lock` seam.

### Exceptions — [fsharp-source.md](.agents/rules/fsharp-source.md) (do not use Exceptions)

```
posted.TrySetException(
    Exception(err))
```

in the probe Actor of [CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs).

### Plan text — [markdown-writing.md](.agents/rules/markdown-writing.md) (`[label](path)`); [refer-by-name.md](.agents/rules/refer-by-name.md) (number and name)

- [spec.md](plan/llm-connector/spec.md): `[[issues/10-cancel-by-focus.md|10]]` is an Obsidian labeled wikilink and uses the id only.
- [map.md](plan/llm-connector/map.md): rewritten list uses `[[path|label]]`; `[[issues/09-agent-failure-preserves-children.md|09]]` uses the id only.
- [project.md](plan/llm-connector/project.md) ticket line `[[issues/10-cancel-by-focus.md|10 — Cancel by Focus]]` is a labeled wikilink. Notes uses markdown.

## Checked, no hit

Core does not reference CloudAgents. No Browser chrome edits. Drop proof uses `liveFocusIds`. `trySecretForFocus` is the cancel lookup, not the observation surface.

## Smells (judgement, not hard)

- **Duplicated Code** — `clearFake` copied in [AgentRunnerFakeTests.fs](tests/CloudAgents.Tests/AgentRunnerFakeTests.fs), [AgentAskTests.fs](tests/Server.Tests/AgentAskTests.fs), [CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs). Three hang-then-cancel Facts share one shape.
- **Speculative Generality** — public test helpers on the CloudAgents DLL:

```
let waitForCancel (timeoutMs: int) : bool =
    Fake.waitForCancel timeoutMs
let fakeCancelCount () = Fake.requestedCancels ()
```
