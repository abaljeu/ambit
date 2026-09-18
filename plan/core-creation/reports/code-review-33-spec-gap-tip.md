# Code review — 33 Spec-gap tip

**Pin:** clean tree → tip `06b312e` only (`git show HEAD`).
**Spec:** [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]].

## 1. Standards

### 1.1 Documented standards (hard)

**1.1.1 `tests/Server.Tests/TestBackend.fs:208` — line length**  
Measure: `LONG (103)`. Added doc comment exceeds 100 chars.  
Cites `.agents/rules/fsharp-source.md`: “100 characters or less per line on source code.”

```
/// GET `/ambit/state` returns the scoped ROOT bootstrap graph; use `?scope=full` for total-load tests.
```

**1.1.2 `tests/Server.Tests/TestBackend.fs` — orphan binding**  
`authDisabledCookieHeader` is defined and never referenced (`withAuthDisabledCookie` inlines `AuthToken.cookieHeaderValue` instead).  
Cites `.agents/rules/core-agent-behavior.md` Surgical Changes: remove bindings your change made unused.

**1.1.3 F# size (functions)**  
All measured bindings under 40 lines — no further hard findings from the measure output.

Plan markdown under `plan/core-creation/` looks fine vs `.agents/rules/markdown-writing.md` / `.agents/rules/refer-by-name.md`. Core boundary: cookie parse stays in Server Adapter (`BrowserRequestCreds.fs`), not Core — OK vs `.agents/rules/core-api.md`.

### 1.2 Baseline smells (judgement)

**1.2.1 Mysterious Name** — `UpdateHelpers.fs`:
```fsharp
let page = readBuildEpochSec ()
```
`page` hides that it is the page deploy/build epoch (`pageDeployEpochSec` elsewhere).

**1.2.2 Speculative Generality** — same helper returns `bool`, but every call site does `|> ignore` (`App.fs`, `Program.fs`). Unused result suggests `unit` until a caller needs the flag.

**1.2.3 Middle Man** (mild) — `BrowserRequestCreds.tryCookieSecret` mostly unwraps the cookie then delegates to `trySecretFromCookieValue`; thin, but the HttpRequest/test seam is a plausible reason to keep it.

## 2. Spec

### 2.1 Missing / partial

- **2.1.1 Partial — client “reseed / re-establish”.** Spec: *“that tells the client to reseed / re-establish. On initial Browser load, the client can make the same call.”* Diff reacts to `DeployEpochSec` by writing `window.__BUILD_TS__` (+ log). No cookie re-issue, re-login, or other credential re-establish. Matches the Spec-gap gloss (“client reacts to DeployEpochSec”) more than the stronger “re-establish” wording.

### 2.2 Scope creep

- **2.2.1 Auth-disabled cookie on ambit app serve.** Spec §6.2: *“Login.html / auth routes issue the same derived token as `gambol_auth`.”* Tip adds `auth.SetCookie` when serving the app with `auth.Disabled` — not named in §6.2 / “boot+login `credentials.add` only.”

### 2.3 Looks implemented but wrong / misaligned

- **2.3.1 Missing-cookie refuse stays in the Adapter.** Spec §3.3: *“Adapter stays decode and status — admission is not re-implemented in the Adapter”*; Credential identity: *“they arrive at CoreMailbox; mailbox validates before action”*; §5.5: *“a missing or inactive credential is the same auth refuse.”* `withBrowserChanges` returns `Unauthorized()` when the cookie is absent and never calls `browserChanges` / CoreMailbox. Spec-gap wants a missing-cookie refuse (and tests cover HTTP 401 + CoreMsg refuse for `Credential ""`), but missing cookie never reaches the Core door as the same CoreAuth refuse path.

### 2.4 Covered cleanly by this tip

Request-carried `gambol_auth` on state/poll/load/changes; drop closed-over `browserCredential` fallback; drop every-request `credentials.add` (boot + login remain); DeployEpochSec restart/initial-load client reaction; no new Browser chrome.

## 3. Summary

Standards 5 (worst: unused `authDisabledCookieHeader`); Spec 4 (worst: missing-cookie refuse stays in Adapter vs CoreMailbox admit).
