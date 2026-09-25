# 07 — Proxy forward inbound secret

**Status:** defined
**Blocked by:** None
**Type:** coding (ops + small PHP)

Optional / not frontier. The hub already delivers via the raw Azure `*.azurewebsites.net` host. Alan said that is fine for now.

## Context

Deliver door `POST /ambit/actors/deliver` requires header `X-Ambit-Inbound-Secret` equal to `grokbot:InboundSecret` ([02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](02-inbound-actors-deliver-door.md)).

Production custom domain `https://collaborative-systems.org/ambit` goes through cPanel `proxy.php` ([cPanel transparent proxy](../../../doc/reference/cpanel-transparent-proxy.md), repo-root [proxy.php](../../../proxy.php)).

`proxy.php` `$forwardHeaders` currently allows only: Accept, Accept-Language, Accept-Encoding, Content-Type, Content-Length, Cookie, Authorization, X-Requested-With, User-Agent, Git-Protocol.

It does not forward `X-Ambit-Inbound-Secret`. POSTs via `collaborative-systems.org` arrive at Ambit with an empty header and get HTTP 401 even when App Settings and the hub secret match.

Proven live 2026-09-25: hub POST to `https://amble-f6bfcfdygjd9b6b7.canadacentral-01.azurewebsites.net/ambit/actors/deliver` returns 200 for ack and empty Done. Alan said that is fine for now.

[06 — Wake response URL](06-wake-response-url.md) currently prefers `PublicAssetBase` / `https://collaborative-systems.org`. Until this ticket lands and cPanel is re-uploaded, hubs that honor wake `responseUrl` still hit the stripping proxy unless they override to `azurewebsites.net`.

## Definition of done

1. Repo-root [proxy.php](../../../proxy.php) includes `X-Ambit-Inbound-Secret` in `$forwardHeaders`.
2. [cPanel transparent proxy](../../../doc/reference/cpanel-transparent-proxy.md) documents that header in the forwarded-request-headers list.
3. Operator note: after merge, re-upload `proxy.php` to the cPanel document root (same ops path as existing proxy updates).
4. Optional proof: deliver via `https://collaborative-systems.org/ambit/actors/deliver` with the header returns 200/404 (not 401) for a known-good secret.

## What to build

### 1. Forward the inbound secret header

1. [ ] Add `X-Ambit-Inbound-Secret` to `$forwardHeaders` in repo-root [proxy.php](../../../proxy.php). Existing header copy maps `X-Ambit-Inbound-Secret` to `HTTP_X_AMBIT_INBOUND_SECRET`.
2. [ ] Document that header in the forwarded-request-headers list in [cPanel transparent proxy](../../../doc/reference/cpanel-transparent-proxy.md).
3. [ ] After merge, re-upload [proxy.php](../../../proxy.php) to the cPanel document root (same ops path as existing proxy updates).

### 2. Proof (optional)

1. [ ] `POST https://collaborative-systems.org/ambit/actors/deliver` with `X-Ambit-Inbound-Secret` equal to a known-good secret returns 200 or 404, not 401.

## Non-goals

1. Changing Ambit Server deliver auth or wake `responseUrl` defaults. Until cPanel is updated, hubs may need to POST to the raw `*.azurewebsites.net` host.
2. Admiral hub implementation
3. Putting `InboundSecret` on the wake body
4. Expanding [01 — CoreActorPool sessionId + deliver + commandId exclusivity](01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md)

## See also

[map.md](../map.md), [06 — Wake response URL](06-wake-response-url.md), [02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](02-inbound-actors-deliver-door.md), [cPanel transparent proxy](../../../doc/reference/cpanel-transparent-proxy.md), [proxy.php](../../../proxy.php)

## Comments

- 2026-09-25 — Filed optional ticket. Live proof: hub POST via raw Azure host succeeds; custom-domain proxy strips `X-Ambit-Inbound-Secret` → 401. Status `defined`. Not frontier.

## Time

- 2026-09-25 15m — File optional proxy allowlist ticket + plan pointers (from chat)
