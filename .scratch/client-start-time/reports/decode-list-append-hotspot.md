# decodeGraph List.append hotspot

Date: 2026-08-27  
Branch: `w/relaxed-concurrency`  
Parent: [[.scratch/client-start-time/reports/bucket-3-post-state-work.md]], [[.scratch/client-start-time/reports/state-further-optimization.md]]

## Flame chart finding

Production boot profiler (microtask block during `decodeStateResponse`):

| Frame | Time | Share |
| --- | --- | --- |
| `Serialization.fs:191` (`decodeGraph`) | 1015.7 ms | 94.1% of microtask |
| `List.js` append | 811.5 ms | 75.2% |
| Thoth `Decode.fs` chain | (nested) | — |

The stack lands on `decodeGraph` because that is where `ApiResponseSerialization.decodeStateResponse` enters graph decode ([[src/Shared/ApiResponseSerialization.fs]]:19). Line 191 is the `Decode.object` body start in [[src/Shared/Serialization.fs]].

## Root cause

**Primary:** Thoth.Json.Core `Decode.list` builds results with `result <- result @ [ value ]` per array element ([Thoth.Json.Core 0.7.1 `Decode.fs`:656). On Fable this compiles to repeated `List.append` — **O(n²)** for n nodes.

Our code at decodeGraph used:

```fsharp
get.Required.Field "nodes" (Decode.list decodeNode)
```

With ~**1800 nodes** and ~**3.7M characters** of scoped bootstrap JSON, that is ~1.6M list-cell copies (n(n+1)/2 appends). The 811 ms `List.append` frame matches this pattern.

**Secondary (not hot):** After decode, `List.map` + `Map.ofList` is **O(n log n)** — not the flame-chart culprit.

**Not hot per node:** `decodeNode` uses `Decode.list decodeChildNode` for each node's `children` field. Child lists are short; quadratic cost there is negligible compared to the top-level `nodes` array.

## Fix

Replace `Decode.list decodeNode` with **`Decode.resizeArray decodeNode`** for the graph `nodes` field.

Thoth's `Decode.resizeArray` pre-allocates `ResizeArray tokens.Length` and uses `.Add` per element — **O(n)** ([Thoth.Json.Core `Decode.fs`:678–695).

Build the map directly from the array:

```fsharp
let nodeArray = get.Required.Field "nodes" (Decode.resizeArray decodeNode)
let nodes =
    nodeArray
    |> Seq.map (fun n -> n.id, n)
    |> Map.ofSeq
```

- Works on **Fable** (client) and **.NET** (Shared.Tests via Thoth.Json.Newtonsoft → Core).
- Mutable `ResizeArray` is confined to Thoth; our code stays immutable after decode.
- **~15 lines changed** in [[src/Shared/Serialization.fs]].

## Expected impact (~1800 nodes / 3.7M chars)

| Phase | Before (est.) | After (est.) |
| --- | --- | --- |
| `nodes` array accumulation | ~800–1000 ms (`List.append`) | ~5–20 ms (`ResizeArray.Add`) |
| Per-node `decodeNode` (1800×) | ~150–250 ms | unchanged |
| `Map.ofSeq` + `Graph.fromNodes` | ~50–100 ms | unchanged |
| **Total decodeGraph / decodeStateResponse** | **~1000 ms** | **~200–350 ms** |

Expect **~600–800 ms** shaved off the Bucket 3 microtask block on production-sized bootstrap — roughly **60–80%** of current decode time, not the full 1015 ms (remainder is per-node object decode and `fromNodes` index rebuild).

Combined with server-side scope-before-encode and gzip, client boot should move measurably toward the ~1.19 s screenshot baseline and below.

## Verification

- Existing [[tests/Shared.Tests/SerializationTests.fs]] `Graph round-trip` covers encode/decode parity.
- `dotnet test tests/Shared.Tests --filter FullyQualifiedName~SerializationTests` — green after change.

## Not done (out of scope)

- Patching Thoth upstream or wrapping all `Decode.list` call sites.
- Avoiding intermediate list in `encodeGraph` (`Map.toList` + `List.map`) — server encode path, not this flame chart.
- Streaming / incremental graph decode — architectural; separate from this O(n²) bug.
