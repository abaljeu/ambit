# Server state compression — implementation report

Date: 2026-08-27  
Branch: `w/relaxed-concurrency`  
Parent: [[.scratch/client-start-time/research.md]], [[.scratch/client-start-time/reports/client-state-compression.md]]

## Verdict

ASP.NET Core response compression is enabled for JSON API responses (including `/ambit/state`) via gzip and Brotli. No extra NuGet package was required — `Microsoft.AspNetCore.ResponseCompression` ships with `Microsoft.NET.Sdk.Web` on net10.0.

## What we coded

1. **Service registration** — `AddResponseCompression` with Brotli + gzip providers, `EnableForHttps = true`, `CompressionLevel.Fastest` ([[src/Server/Server.fs]]).
2. **Middleware** — `UseResponseCompression` at the start of `configureApplication`, before `HttpResponseLog` and static files, so the log captures plaintext while the client receives compressed bytes.
3. **Tests** — three integration tests in [[tests/Server.Tests/ResponseCompressionTests.fs]] (gzip, brotli, no Accept-Encoding).

## Middleware order

```
UseResponseCompression
HttpResponseLog (capture plaintext)
UseDefaultFiles / UseStaticFiles / routes
```

Default MIME types include `application/json`. Minimum response size for compression is the ASP.NET default (~860 bytes); bootstrap state JSON exceeds this.

## Files changed

| File | Change |
| --- | --- |
| `src/Server/Server.fs` | `configureResponseCompression`, `useResponseCompression` |
| `tests/Server.Tests/ResponseCompressionTests.fs` | new — gzip/br/uncompressed integration tests |
| `tests/Server.Tests/Gambol.Server.Tests.fsproj` | register test file |

## Verification (automated)

```bash
dotnet build src/Server -c Debug
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~ResponseCompressionTests"
```

Result: 3/3 passed.

## Verify in browser (HITL timing retest)

1. Deploy or run the server locally with this branch.
2. Open DevTools → Network, hard-reload `/ambit`.
3. Select the `/ambit/state` request.
4. **Response Headers** should include `Content-Encoding: br` or `Content-Encoding: gzip` (browser sends `Accept-Encoding` automatically).
5. **Size** column should show transferred size much smaller than resource/decoded size (e.g. ~3.7M decoded → hundreds of KB transferred).
6. App should boot normally — client `fetch` + `r.text()` decompresses transparently; rollout guard in [[src/Client/Program.fs]] catches misconfigured proxies.

cPanel proxy forwards `Accept-Encoding` and passes `Content-Encoding` back ([[doc/reference/cpanel-transparent-proxy.md]]).

## Suggested commit message (with scope-before-encode work)

```
Enable gzip/brotli response compression for API JSON.

Scope-before-encode returns StateResponse once; middleware compresses
/ambit/state and related JSON when Accept-Encoding is present.
```

## Out of scope / follow-up

- HITL production timing validation remains on [[WORK.md]] (TTFB and payload size after deploy).
