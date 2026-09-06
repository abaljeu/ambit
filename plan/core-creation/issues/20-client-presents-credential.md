# 20 — Client presents credential

**Status:** ready-for-agent
**Blocked by:** [[14-server-tracks-credentials.md|14 Server tracks credentials]], [[15-launch-actor-and-hold-span.md|15 Launch an Actor and hold the span]]

## Context

A live Browser is an active source, like an Actor. After page open, every message from the Browser must present a credential.

## What to build

The live Browser presents a credential on every message. A missing or inactive credential is the same auth refuse as an inactive Actor sender.

- [ ] Every Browser message presents a credential.
- [ ] A Browser message without a live credential is the same auth refuse as an inactive Actor sender.

## See also

[[10-define-actor-cancellation-and-output-admission.md]], [[09-define-core-command-launch-contract.md]]
