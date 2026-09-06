# 20 — Client presents credential

**Status:** done
**Blocked by:** [[14-server-tracks-credentials.md|14 Server tracks credentials]], [[15-launch-actor-and-hold-span.md|15 Launch an Actor and hold the span]]
**Actual:** 30m

## Context

A live Browser is an active source, like an Actor. After page open, every message from the Browser must present a credential.

## What to build

The live Browser presents a credential on every message. A missing or inactive credential is the same auth refuse as an inactive Actor sender.

- [x] Every Browser message presents a credential.
- [x] A Browser message without a live credential is the same auth refuse as an inactive Actor sender.

## See also

[[10-define-actor-cancellation-and-output-admission.md]], [[09-define-core-command-launch-contract.md]]

## Comments

- 2026-09-06 — Implementation started on `dev`.
- 2026-09-06 — Delivered Browser presentation of the `gambol_auth` cookie on POST JSON (`credentials: 'same-origin'`). Without a live cookie, `/ambit/state`, poll, changes, and load are HTTP 401, the same auth refuse as an inactive Actor sender. See [[plan/core-creation/reports/implement-issue-20.md]].

## Time

- 2026-09-06 30m — Browser cookie on every message; 401 without a live cookie (from chat)
