# Client state compression — implementation report

Date: 2026-08-27  
Branch: `w/relaxed-concurrency`  
Parent: [[.scratch/client-start-time/research.md]]

## Verdict

**Browser auto-decompression is sufficient.** No manual gzip/brotli/deflate decode was added. The client already used `fetch()` + `r.text()` for bootstrap state; that path transparently decompresses when the server (or proxy) sends `Content-Encoding`.

## How `/ambit/state` is fetched

[[src/Client/Program.fs]] builds `stateUrl` (`/{file}/state` or with `?zoom=`) and calls `fetchGet` from [[src/Client/JsInterop.fs]] — not `fetchTextNoCacheWithFail`. Capability fetches still use `fetchTextNoCacheWithFail`; all use the same underlying `fetch` + `r.text()` pattern.

## Browser behavior

| Concern | Finding |
| --- | --- |
| `Accept-Encoding` | Forbidden header in `fetch`; the user agent sends gzip/br/deflate automatically. |
| Decompression | `Content-Encoding` bodies are decompressed before `Response.text()` returns. |
| Uncompressed rollout | Plain JSON (`{`…) passes through unchanged — no branch needed. |
| Misconfiguration | If compressed bytes arrive without `Content-Encoding`, `r.text()` returns raw binary; JSON decode would fail cryptically. |

## What we coded

1. **Documented** transparent decompression on `fetchGet` ([[src/Client/JsInterop.fs]]).
2. **Rollout guard** — `LogText.looksCompressed` detects gzip/zlib magic; bootstrap shows a clear boot error instead of a decode failure ([[src/Shared/LogText.fs]], [[src/Client/Program.fs]]).
3. **Tests** — [[tests/Shared.Tests/LogTextTests.fs]] for `looksCompressed`.

No Shared fetch helper was required beyond the existing `fetchGet` emit; logic stays in Client interop.

## Files changed

| File | Change |
| --- | --- |
| `src/Shared/LogText.fs` | `looksCompressed` |
| `src/Client/JsInterop.fs` | `fetchGet` compression comment |
| `src/Client/Program.fs` | guard before `decodeStateResponse` |
| `tests/Shared.Tests/LogTextTests.fs` | four tests |

## Verification

- `dotnet build tests/Shared.Tests -c Debug`
- `dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~LogTextTests"`
- `dotnet fable src/Client` (client compile)

## Server follow-up (out of scope here)

Enable ASP.NET response compression middleware on `/ambit/state` after scope-before-encode. cPanel proxy forwards `Accept-Encoding` and passes `Content-Encoding` back ([[doc/reference/cpanel-transparent-proxy.md]]).
