# Stream code quality

Date: 2026-09-23

## What was wrong

The stream path in [CursorHttp.fs](../../../src/CloudAgents/Internal/CursorHttp.fs) had two SSE parsers. `parseSseDocument` folded lines, and `streamRun` walked the same `event:` / `data:` / blank rules again. `streamRun` was 82 lines. `dispatchStreamMessage` was 49 lines and built `GitResult` a second time, beside `mapGitResult` in [CursorAdapter.fs](../../../src/CloudAgents/Internal/CursorAdapter.fs). `StreamDispatch` had a `Cancelled` case that no event produced.

End of stream was also wrong. A non-terminal SSE block returned `StreamIncomplete`, and the reader called itself again. At EOF, `ReadLine` returns null, so a stream with no terminal event spun instead of returning `stream ended without terminal event`.

[AgentRunner.fs](../../../src/CloudAgents/AgentRunner.fs) `waitFakeStream` was 43 lines and copied the `waitLive` deadline check. `streamUntilComplete` matched the fake result only to return that same result. [Program.fs](../../../src/CloudAgents.Console/Program.fs) `streamForResult` was 41 lines.

HTTP 409 `stream_unavailable` is not a poll case. `responseError` still returns `HTTP 409: …` for every non-success status except 401. [CursorAdapter.streamRun](../../../src/CloudAgents/Internal/CursorAdapter.fs) maps that string to `NetworkError` and stops.

## What changed

One line stepper (`stepSseLine`) feeds `parseSseDocument`, the pure `interpretSseDocument`, and the live `StreamReader` loop. EOF calls `finishSse` once and stops. Git mapping stays in `mapGitResult`. `streamRun` returns `StreamBody`. The adapter turns that into `AgentResult` and `RunFinished`.

Fake wait and live poll share `pastDeadline`. Cancelled runs share `cancelledStream`. Each new or edited stream function is under 40 lines. No source line in these files is over 100 characters.

Tests cover a result document, a document with no terminal event, an error event that stops before a later result, and a result with no trailing blank line.

## Test result

`dotnet test tests/CloudAgents.Tests/Gambol.CloudAgents.Tests.fsproj` passed. 37 passed, 0 failed, 0 skipped.

## Left in place

`getRunStatus` (71 lines) and `createAgent` (54 lines) are still over 40 lines. They are outside the stream path. The fake runner still uses its process-local `ref` cell. Live `streamUntilComplete` still does not apply `pollIntervalMs` or `maxWaitMs`. Those arguments control the fake wait. The live call blocks on SSE. A `done` event is still an empty terminal result. This pass did not change READMEs and did not commit.
