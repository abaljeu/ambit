# AI agent protocol

Category: Contract
See Also: [Actors](actors.md), [Gambol.CloudAgents](gambol-cloud-agents.md), [HTTP contract](http-contract.md)

Ambit wakes an external agent with one ack-only POST and takes reply text on `POST /ambit/actors/deliver`.

## Sources

[Grok bot HTTP](../../src/CloudAgents/Internal/GrokBotHttp.fs)
[Grok bot adapter](../../src/CloudAgents/Internal/GrokBotAdapter.fs)
[Grok bot settings](../../src/Server/GrokBotSettings.fs)
[Route registration](../../src/Server/RouteRegistration.fs)
[API deliver](../../src/Server/Api.fs)
[Run Agent Actor](../../src/Server/RunAgentActor.fs)

## Parties

[x] Ambit Server sends the wake and accepts deliver.
[x] The external agent is the Grok bot. The Run Agent Actor selects this wire when the first behavior token is `gbot`.
[x] `sessionId` is the live-pool id, a GUID string. The wake and the deliver use that same string.

## Operations

### Wake

[x] Method: `POST`.
[x] URL: the config value `grokbot:WakeUrl`, posted as given.
[x] Header: `Authorization: Bearer <grokbot:WakeSecret>`.
[x] Header: `Content-Type: application/json; charset=utf-8`.
[x] A blank wake URL or a blank wake secret does not send.

```json
{
  "source": "ambit",
  "kind": "message",
  "sentAt": "2026-09-25T00:00:00.0000000Z",
  "commandId": "550e8400-e29b-41d4-a716-446655440000",
  "focusId": "550e8400-e29b-41d4-a716-446655440001",
  "sessionId": "550e8400-e29b-41d4-a716-446655440002",
  "text": "extract pack",
  "responseUrl": "https://collaborative-systems.org/ambit/actors/deliver",
  "payload": {}
}
```

[x] `source`: string. The value is `ambit`.
[x] `kind`: string. The value is `message`.
[x] `sentAt`: string. UTC round-trip time.
[x] `commandId`: string. The command Node id.
[x] `focusId`: string. The focus Node id.
[x] `sessionId`: string. The live-pool id.
[x] `text`: string. The system prompt, a blank line, then the Zoom-rooted extract XML.
[x] `responseUrl`: string. `PublicAssetBase` with trailing slashes removed, or `https://collaborative-systems.org` when that setting is blank, then `/ambit/actors/deliver`.
[x] `payload`: object. The value is `{}`.
[x] `200`–`299`: success. Ambit ignores the body.
[x] `401`: failure. Ambit records unauthorized.
[x] Any other status: failure. Ambit records the status and the body text.

### Deliver

[x] Method and path: `POST /ambit/actors/deliver`.
[x] Header: `X-Ambit-Inbound-Secret`. The value equals `grokbot:InboundSecret`.
[x] A missing header, a mismatched secret, or a blank configured secret is `401` with an empty body.
[x] Content type: `application/json`.
[x] The body requires `sessionId` and `text`. Extra fields are ignored.

```json
{
  "sessionId": "550e8400-e29b-41d4-a716-446655440002",
  "text": "<>"
}
```

[x] `sessionId`: string. The live-pool id from the wake.
[x] `text`: string. Reply text for that session.
[x] `200`: the session is live and the text is accepted. The body is empty.
[x] `400`: the body is not those two strings.

```json
{ "error": "invalid deliver body" }
```

[x] `404`: the session is not live. The body is empty.
[x] `500`: the runner returns an error. The body is empty.
[x] Empty text. `text` `""` keeps the session live. A later deliver with text still applies.
[x] Whitespace. A `text` value of spaces is reply text.
