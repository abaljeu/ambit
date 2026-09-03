# Direct Browser API vs cPanel proxy

Recommendation, not a Committed Decision. Related: [[doc/reference/cpanel-transparent-proxy.md]], [[proxy.php]], [[src/Client/JsInterop.fs]], [[src/Server/RouteRegistration.fs]], [[src/Desktop/LocalProxy.fs]], [[src/Desktop/Desktop.fs]], [[browser-workspace-load-timeout.md]], [[graph-only-reconcile-chunk.md]]. App WebView2 Navigate slice: [[plan/webview2-azure-origin/issues/01-webview2-navigate-azure-ambit.md]].

## Recommendation

**Do not** send Browser `fetch` / XHR to `*.azurewebsites.net` while the page stays on `https://collaborative-systems.org/ambit`.

For the **pretty URL** in a web browser, keep [[proxy.php]] (or an equivalent same-origin reverse proxy) for the user URL, login, `Set-Cookie`, and **all cookie-auth Browser HTTP** (`GET` and `POST`). Keep the current split: JS/CSS from Azure (`PublicAssetBase`); App git and file bodies already off PHP (`6ce817f` class).

For the **App** (WebView2), do **not** keep the document on `collaborative-systems.org/ambit` and `fetch` Azure. Discover the Azure host, then `Navigate` the WebView to Azure `/ambit` so the **page origin** is Azure. Relative API calls are then same-site. PHP is out for that document. The user never sees the address.

A later way for the pretty URL to leave PHP is a **same-site hostname** on App Service (for example `ambit.collaborative-systems.org`), not POSTs to the Azure default host from the HostGator page.

## Why the naive split is blocked

Login `POST /ambit/login` goes through PHP. Azure sets `gambol_auth` with no `Domain` ([[src/Server/RouteRegistration.fs]]: HttpOnly, Secure, SameSite=Lax, ~10 year expiry). The Browser stores that cookie on **`collaborative-systems.org`**. Cookies do not move to `azurewebsites.net`.

WebView2 is still a Browser for cookies, CORS, and `fetch`. No URL bar does not skip SameSite.

SameSite=Lax also withholds the cookie on **cross-site** XHR even if the host matched. `collaborative-systems.org` and `*.azurewebsites.net` are different sites (eTLD+1).

CORS today is only for `/ambit/*.js` (and maps/svg). Default `Access-Control-Allow-Origin: *` when `PublicAssetBase` is on ([[src/Server/Server.fs]]). Credentialed API needs a **named** origin, `Access-Control-Allow-Credentials: true`, and `OPTIONS` preflight for `Content-Type: application/json`. `*` plus credentials is invalid. API routes have **no** CORS and **no** OPTIONS.

Client URLs are **relative** (`/{pathname}/state`, `…/poll`, `…/changes`, `…/load`) from `window.location.pathname` ([[src/Client/UpdateHelpers.fs]], [[doc/api.md]]). `fetchGet` / `postEmpty` use `credentials: 'same-origin'`. `postJson` omits `credentials` (Fetch default is same-origin). Cross-origin Azure URLs would send **no** cookie unless the client used `credentials: 'include'` **and** Azure echoed the custom-domain origin.

**Blocker (auth):** cookie host + SameSite. **Blocker (CORS):** API CORS/preflight absent; `*` cannot carry cookies. Mixed content is not a factor (both HTTPS). No Content-Security-Policy on API. No EventSource / SSE in the Client.

A token in JS (`Authorization`) could reach Azure without the cookie. That drops HttpOnly and needs a CSRF design. Do not do that only to skip PHP.

## App WebView2 origin

Today the App sets WebView `Source` to loopback LocalProxy ([[src/Desktop/Desktop.fs]], [[src/Desktop/LocalProxy.fs]]). That document is already same-origin to the proxy. The process attaches `gambol_auth` from AuthStore and HTTP-forwards to Azure. Relative `/state` / `/changes` / `/poll` do not go to PHP.

Agreed later App slice: after host discovery, `Navigate` to Azure `/ambit`. That is a **new origin**. A `gambol_auth` cookie from the pretty domain or from loopback does not follow `Navigate` by itself. Login must happen **after** that navigate, **or** the host copies `gambol_auth` onto the Azure host with the WebView2 cookie API.

`6ce817f` bypassed PHP for **App file/git** (WebDAV, `workspace-push`, capability/`direct-upload`), not for custom-domain Browser JSON.

This report does not implement WebView or host code.

## What “all API posts” includes

PHP today forwards **every** `/ambit/…` method except static JS/CSS that the HTML already points at Azure.

| Kind | Paths (prefix `/ambit`) | Client |
| --- | --- | --- |
| GET JSON | `/state`, `/poll`, `/capabilities`, `/file`, `/git-token`, reconcile `…/latest` | `fetch` same-origin |
| POST JSON | `/changes`, `/load`, `/save`, `/file-status`, `/file/parse`, `/workspace/reconciliation/directory`, `…/added` | `postJson` / sync XHR |
| Form | `POST /login` | full navigation; sets cookie |
| HTML | `GET /ambit`, `/login` | must stay on the user URL |
| App-only | `/_desktop/*` | not on cPanel |
| Git smart HTTP | `/git/{repo}/info/refs`, `git-upload-pack`, `git-receive-pack` | git client; PHP already 600 s |
| WebDAV | `/dav/{label}/…`, `_prepare-push`, `_finish-commit`, `/direct-upload` | App; already Azure-side for bodies |

Poll is **GET**, not EventSource. Boot `GET /state` is the large read; `/load` is POST Fetch+Poll. A hybrid “only POST to Azure” still leaves `/state` and `/poll` on PHP (or splits them and 401s).

## Time and size

[[proxy.php]] buffers the full body (`php://input`) and the full curl response (`CURLOPT_RETURNTRANSFER`). Default curl timeout **60 s** → HTTP **502** `Proxy error`. Git, `/workspace/reconciliation/`, and `/load` use **600 s** after cPanel has the new file ([[graph-only-reconcile-chunk.md]]). Other JSON stays 60 s (`POST /changes` included).

Kestrel max body is **100 MiB** ([[src/Server/Server.fs]]). That is not the usual 502. Azure App Service still cuts idle HTTP near **230 s** after PHP is gone. Chunking and faster reconcile remain the product fix; skipping PHP does not remove App Service or DbAgent 8 s apply bounds.

## Hybrid cost (pretty URL)

Some routes on PHP, some on Azure: 401 (cookie miss), CORS fail (blank network error), vs 502 (curl timeout). Login `Location` rewrite keeps the user on the custom domain; an Azure-only 302 would show `*.azurewebsites.net`. Operator must ship CORS, cookie `Domain`, Client absolute URLs, and `credentials: 'include'` together. Partial deploy fails closed.

## Tradeoffs if the pretty URL leaves PHP

1. **Keep path `/ambit` on HostGator** — keep PHP for HTML+API. Raise curl timeout or chunk work. This matches the user URL constraint (other sites on the same host).
2. **Same-site CNAME to App Service** — e.g. `ambit.collaborative-systems.org` → Azure, TLS on Azure, cookie `Domain=.collaborative-systems.org`, CORS named origin, Client `credentials: 'include'` if HTML stays on the apex path. Then API (and optionally the shell) can skip PHP.
3. **Move the shell to Azure custom domain** — ditch PHP for that host; keep HostGator as marketing plus a link or 302. Path `/ambit` on a shared site is the reason PHP exists ([[doc/reference/cpanel-transparent-proxy.md]]).

The App WebView `Navigate` to Azure `/ambit` is not option (2) or (3) for the public pretty URL. It does not replace [[proxy.php]] for web-browser users.

## Decision

Pretty URL: accept **(1)** unless ops chart **(2)** or **(3)**. Do not implement Browser POSTs to the Azure default hostname while the page stays on HostGator.

App: chart `Navigate` to Azure `/ambit` after host discovery; then login, or copy `gambol_auth` with the WebView2 cookie API. Do not `fetch` Azure from a pretty-URL or loopback document.

## Board mutations (parent applies)

- Keep Pending [[plan/roadmap/reports/graph-only-reconcile-chunk.md]] — HITL: upload [[proxy.php]] to cPanel; large DataDir Workspace Load should be 200, not 400/502 (may still be slow).
- Update Pending [[plan/roadmap/reports/direct-api-vs-proxy.md]] — pretty URL: keep PHP for cookie-auth Browser API, or chart a same-site Azure hostname (not POSTs to `*.azurewebsites.net` while the page stays on the custom domain).
- `add` [[plan/roadmap/reports/direct-api-vs-proxy.md]] — later App slice: WebView2 `Navigate` to Azure `/ambit` after host discovery; login-after-navigate or copy `gambol_auth` via the WebView2 cookie API (not fetch Azure from the pretty URL).
