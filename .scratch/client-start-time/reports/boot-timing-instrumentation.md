# Boot timing instrumentation

Dev/prod console logs for Bucket 3 boot phases: state decode, session restore, and first view render.

## What was added

### `src/Client/Program.fs`

In the `/state` fetch success callback, wraps `decodeStateResponse` with `perfNowMs()` before/after.

### `src/Client/App.fs`

On `SysMsg (StateLoaded _)` only:

- Times `restoreSessionState` (includes `applyFoldSession`)
- Times `View.render` on first full render after load
- Logs optional `StateLoaded dispatch total` from dispatch entry through render

Uses `perfNowMs()` (`performance.now()`) and `consoleLog` from [[src/Client/JsInterop.fs]]. No debug flag — always on, prefixed `[Gambol boot]`.

## Example console output

```
[Gambol boot] decodeStateResponse: 142ms, 2847193 chars, 1842 nodes
[Gambol boot] restoreSessionState: 8ms
[Gambol boot] View.render: 95ms, 47 rows
[Gambol boot] StateLoaded dispatch total: 118ms
```

Order: decode runs in the fetch callback; restore/render/total run when `StateLoaded` is dispatched.

After [[.scratch/client-start-time/reports/decode-list-append-hotspot.md]] (`Decode.resizeArray` in [[src/Shared/Serialization.fs]]), production decode should drop from ~900ms toward ~200–350ms on the same ~6k-node graph; restore/render/total stay in the low-ms range above.

## Notes

### `perfNowMs` vs `nowMs`

Boot elapsed timing uses **`perfNowMs()`** (`performance.now()` → `float`, monotonic from navigation start). **`nowMs()`** stays **`Date.now()`** for idle/polling/cache-bust — wall-clock epoch ms that overflows 32-bit `int` when used as a duration baseline (~1.78e12 ms).

A prior bug logged epoch values (~1786706395147 ms) for restore/render/total because `Date.now()` deltas do not fit in F# `int` and Fable closure capture of `restoreStart` was unreliable. Decode sometimes looked correct when both endpoints truncated similarly within ~900 ms.

### Fields

- `chars` = raw JSON text length from `/state`
- `nodes` = `Map.count response.graph.nodes`
- `rows` = `List.length (ViewModel.getVisibleInstanceIds newModel.siteMap)`
- Timing only — no behavior change
