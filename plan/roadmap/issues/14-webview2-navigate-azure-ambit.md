# 14 — App WebView2 Navigate to Azure `/ambit`

**What to build:** After Azure host discovery, the App WebView2 document origin is Azure `/ambit`. Cookie-auth Browser HTTP is then same-site on Azure. Do not `fetch` Azure while the page stays on the pretty URL or on loopback LocalProxy. Login after that Navigate, or copy `gambol_auth` onto the Azure host with the WebView2 cookie API.

**Blocked by:** None as a ticket. Product seams are incomplete: there is no Azure host discovery, and relative `/_desktop/*` assumes the document origin is LocalProxy.

**Status:** ready-for-agent

- [ ] Discover the Azure origin. Do not hard-code the cPanel `$BACKEND` host in the App as the long-term source. Do not treat `https://collaborative-systems.org/ambit` as the document origin after discovery.
- [ ] After discovery, `Navigate` WebView2 to Azure `/ambit` so `window.location` is Azure. Relative `/state`, `/poll`, `/changes`, `/load` stay same-origin. PHP is out for that document.
- [ ] Auth on the new origin: if [[src/Desktop/AuthStore.fs]] has credentials, copy `gambol_auth` onto the Azure host with WebView2 `CookieManager` (HttpOnly, Secure, SameSite=Lax, path `/`, value from [[src/Server/AuthToken.fs]] `deriveToken`). If there are no stored credentials, login after Navigate so Azure `Set-Cookie` lands on the Azure host.
- [ ] Keep LocalProxy for `/_desktop/*` (capabilities, mappings, push, download, file-status). Relative `/_desktop` from an Azure document will miss the App. Solve that before Navigate (loopback absolute URLs, WebView2 host mapping, or another same-origin App channel). Do not drop App filesystem features.
- [ ] Do not send credentialed `fetch` / XHR to `*.azurewebsites.net` while the document stays on the pretty URL or on loopback. Do not use a JS `Authorization` header only to skip PHP.

## Context

Decision source: [[plan/roadmap/reports/direct-api-vs-proxy.md]]. Worker notes: [[plan/roadmap/reports/webview2-azure-navigate-issue.md]].

Today [[src/Desktop/Desktop.fs]] `resolveTargetUrl` defaults to the pretty URL. [[src/Desktop/LocalProxy.fs]] starts on loopback; WebView2 `Source` is that loopback `/ambit`. The process attaches `gambol_auth` on the **forwarded** HTTP to the proxy target. The document origin is loopback, not Azure.

`AuthStore` stores username and password, not the cookie bytes. LocalProxy rebuilds the Cookie header with `AuthToken.cookieHeaderValue`. A pretty-domain or loopback cookie does not follow `Navigate` to `*.azurewebsites.net`.

There is no App Project folder. This issue lives on the Roadmap because the slice came from a Roadmap report. Roadmap [[map.md]] still treats [[issues/]] as wayfinder tickets; this file is an implementation issue (`Status:` triage), not a wayfinder `Type:` ticket.

## Remaining after 2026-09-02 claim

Claimed this sitting. No product code: host discovery is absent; Navigate to Azure without a `/_desktop` plan would break App file and Workspace host routes. First complete increment is still the discovery plus `/_desktop` origin plan, then Navigate and cookie copy.

Do not fetch Azure from the pretty URL. Pretty-URL Browser PHP stays a separate Pending line on [[direct-api-vs-proxy.md]].

## Comments

- 2026-09-02: Filed from retired WORK.md App slice. Status set `ready-for-agent`, claimed, then returned to `ready-for-agent` after the seam check.
- 2026-09-02: Parked from WORK.md again. Outcome stays on this issue: after host discovery, WebView2 `Navigate` to Azure `/ambit`; copy `gambol_auth` via CookieManager or login after Navigate; keep `/_desktop` on LocalProxy; do not fetch Azure from the pretty URL.
