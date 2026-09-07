# Console log for failed POST /changes (no Network tab)

## Goal

When `POST .../changes` returns non-2xx, log **HTTP status** and **response body** (truncated) to the console with a **filter token** `GAMBOL_HTTP_ERR` so you can diagnose without DevTools Network (which may not retain requests if DevTools was closed).

## Changes

### 1. [src/Client/JsInterop.fs](src/Client/JsInterop.fs)

Replace `postJson` emit so the chain always does `r.text()` first, then:

- if `r.ok` → `onSuccess(t)`
- else → `onHttpError(r.status, t)`

Signature: `(onHttpError: int -> string -> unit)` instead of `onReject: unit -> unit`.

### 2. [src/Client/App.fs](src/Client/App.fs)

In the `SubmitChange` branch, replace the HTTP-error callback with one that:

- `clearTimeout timeoutId`
- builds a snippet (first 400 chars of body, append `...` if longer)
- `consoleLog` a line containing `GAMBOL_HTTP_ERR`, `req=`, `http=` (status), `body=` (snippet)
- `dispatch (SysMsg SubmitRejected)` unchanged

## Filter in console

Search for: `GAMBOL_HTTP_ERR`

Server 400 bodies are JSON like `{"error":"Revision mismatch: ..."}` — the snippet will include that text.

## Tests

No new tests required (browser-only `Emit`; behavior is manual verification). Optional: document in a comment near `postJson` that error bodies are surfaced in App.fs.

## Done when

- Fable build succeeds; failed submit shows one log line with status + body snippet and `GAMBOL_HTTP_ERR`.
