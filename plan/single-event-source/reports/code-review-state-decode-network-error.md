# Code review — /state decode / boot error

Independent review. Not approval. No ticket. Bug intent is the Spec. [project.md](plan/single-event-source/project.md) Stage stays `build`.

Range: `origin/staging...HEAD` (three-dot). Tip `ff6eecc3`. Base `b226cb97`. Non-empty. 6 files. One commit: `Fix /state boot decode throwing KeyNotFound and looking like a network failure.`

Mechanical scan (`python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`): FILE [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) 454→548; FILE [ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) 2983→3001. Binding [graphFromDecodedNodes](src/Shared/Serialization.fs) 18 lines. No OVER bindings. No added LONG lines.

Alan locks applied: labeled links `[label](path)`; Change→Ev naming; EventId not peeled to int in-process; no GitHub PR; this report sits on a disposable `cursor/*` only; no staging push.

## Standards

Range `origin/staging...HEAD` is not empty (6 files). Tip `ff6eecc3`. Base `origin/staging` `b226cb97`. Mechanical-scan lines are documented-standard hits. Surgical under-100-line preference is not a fail ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

**Hard documented violations**

File size ([fsharp-source.md](.agents/rules/fsharp-source.md): 400 lines; do not grow a file already over 400): [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) FILE 454→548; [ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) FILE 2983→3001. `graphFromDecodedNodes` is 18 lines. No OVER bindings. No added long lines.

Exceptions (same rule: do not use Exceptions; use Error types). [ApiResponseSerialization.fs](src/Shared/ApiResponseSerialization.fs) wraps `Decode.fromString decodeStateResponseDecoder`:

```
try
    Decode.fromString decodeStateResponseDecoder text
with ex ->
    Error ("state decode exception: " + ex.Message)
```

[Program.fs](src/Client/Program.fs) wraps the `/state` success path:

```
try
    ...
with ex ->
    showBootError ("failed to apply /state: " + ex.Message)
```

The [Serialization.fs](src/Shared/Serialization.fs) `failwithf` → `Decode.fail` change is the Error-typed path. The two new `try/with` handlers put exception control flow back in Shared and Client.

EventId / Change→Ev: no hit. The test uses `EventId.fromJson 1` on a JSON fixture ([core-api.md](.agents/rules/core-api.md) serialize path). No `change` / `changes` bound to Ev. No EventId peeled to int in-process.

Markdown / planning: no plan files in range. No consecutive-blank or bare-id hits ([markdown-writing.md](.agents/rules/markdown-writing.md), [refer-by-name.md](.agents/rules/refer-by-name.md)).

**Judgement smells (not hard)**

Duplicated Code — same catch-to-string shape in Shared and Client (quotes above).

Speculative Generality — the Client `try/with` also covers `finishPaint` / `persistAfterState`, not only decode:

```
finishPaint response []
setTimeout (fun () -> BootCacheStore.persistAfterState ...) 0
```

## Spec

Range `origin/staging...HEAD` is non-empty (one commit `ff6eecc3`, 6 files). Spec is the bug intent (no ticket). I did not run CI.

**(a) Missing or partial**

None. Spec lines 1–5 are present on the `/state` boot path.

**(b) Scope creep**

**Spec:** “Missing canonical ROOT must be a decode `Error`, not `KeyNotFoundException` / throw through `fetchGet`.”

`firstGraphChild` now skips Root children absent from `graph.nodes` ([ViewModelOccurrence.fs](src/Shared/ViewModelOccurrence.fs)). Missing ROOT is already a `Decode.fail` in `graphFromDecodedNodes` *before* `Graph.fromNodes` (the `nodes.[rootId]` lookups in `ensureWorkspacesNode` / System / Trash). This instead changes `StateLoaded` Zoom: it uses `firstGraphChild`, then `buildSiteMapFrom` does `graph.nodes.[rootNodeId]`. Dangling first-child ids no longer KeyNotFound at apply; Zoom just moves to the next present child. Spec did not ask to change Zoom. The new ViewModel test locks that extra behavior.

**(c) Looks implemented, looks wrong**

None found.

- **Spec 1 / 5:** `fetchGet` still maps any throw in `onSuccess` to `onNetworkFail` → `"network failure loading /state"`. `loadFromState` now try/withs the 200 body: decode `Error` → `"failed to decode /state: …"`; later throw (including `finishPaint` / `StateLoaded`) → `"failed to apply /state: …"`.
- **Spec 2:** missing canonical ROOT is `Decode.fail "graph missing canonical root node"`; `decodeStateResponse` also turns leftover decode throws into `Error`.
- **Spec 3:** mixed `kind` is existing `oneOf`; null `name` is `Optional.Field` → `None` via Thoth `decodeMaybeNull`; string `updateTime` is `Decode.int64` (`integral` `TryParse` on strings). No new codec required.
- **Spec 4:** tests cover Alan node + `StateResponse` shapes and Graph missing ROOT as `Error` (not throw).
- Alan locks: `eventId` stays `EventJson.decodeEventId` / `EventId`; no Change→Ev peel in-scope.

## Summary

Standards: 4 hard findings (2 FILE growth, 2 Exception `try/with`), 2 judgement smells (Duplicated Code, Speculative Generality); worst is Exception control flow in [ApiResponseSerialization.fs](src/Shared/ApiResponseSerialization.fs) and [Program.fs](src/Client/Program.fs).

Spec: 1 finding (scope creep); worst is `firstGraphChild` skipping absent Root children and changing `StateLoaded` Zoom.
