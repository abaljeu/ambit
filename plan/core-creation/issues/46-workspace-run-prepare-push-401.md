# 46 — Workspace Load prepare-push 401

**Status:** coded
Actual: 1h

## 1. What happened

On a mapped Workspace Node, **Load** failed. The App showed `{"error":"prepare-push HTTP 401: "}`. The reason text after the status was empty. The Workspace files did not upload.

## 2. What I expected

The same Desktop session that already shows the Graph would complete the Workspace upload. If the session is not admitted, the App would show a clear login or auth failure, not an empty 401 on prepare-push.

## 3. Steps to reproduce

1. Open the Desktop App against a live Ambit host.
2. Focus a mapped Workspace Node.
3. Invoke **Load** on that Workspace Node.
4. The App reports `prepare-push HTTP 401` with an empty reason. Upload stops.

## 4. Additional context

Alan confirmed the command was Load, not Run or Ctrl+Enter. Browser Graph traffic on Desktop already carries a request cookie. Workspace upload talks to the host on a separate Desktop client and sends a cookie only when stored login credentials exist. A local Development session with no login, or a session that holds only a server-issued cookie, can open the Graph and still fail this upload. Reported during QA. Related prior Desktop 401 work is [33 — Credentialed Browser Change posts](33-credentialed-browser-change-posts.md).

## 5. Comments

1. 2026-09-18 — Alan: command was Load. Validate every Desktop host call, not only workspace-push.

## Time

- 2026-09-18 1h — Load wording; Shared request cookie; Desktop push/pull/download; host-call inventory (from chat)
