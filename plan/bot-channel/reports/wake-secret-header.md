# Wake secret header

Date: 2026-09-25
Follows: [live-grokbot-corrections.md](live-grokbot-corrections.md)

## 1. Contract

1. Outbound wake POST sends `X-Ambit-Wake-Secret: <WakeSecret>`.
2. Empty / whitespace `WakeSecret` fails closed (`AuthenticationFailed` “missing wake secret”) and does not send an unauthenticated POST. Same class as empty `WakeUrl`.
3. `applyWakeAuth` attaches the named header and value when the secret is present; it adds no header when the secret is empty.

## 2. Checks

1. `dotnet build` CloudAgents, CloudAgents.Tests, Server, Server.Tests — passed.
2. `dotnet test tests/CloudAgents.Tests` filter `GrokBotOneshotTests` — 11 passed.
3. `dotnet test tests/Server.Tests` filter `AgentGrokBotStreamTests` — 7 passed.

## 3. Server secrets

1. Documented `grokbot:WakeUrl` + `grokbot:WakeSecret` + `grokbot:InboundSecret` are now sufficient for the Server path: wake is authenticated, inbound deliver is authenticated, empty-text Done finishes the oneshot. Console was not changed.
