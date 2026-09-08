# 06 — Api.fs actors Create to Core launch

**Status:** ready-for-agent
**Blocked by:** [[05-register-cloud-agent-actor.md]]

## Context

Create path is HTTP at the edge, not Core. Grill lock: `Api.fs` POST → typed message → message handler → Core `launch`.

## What to build

- `POST /ambit/actors` (Create) in Api.fs: JSON `{ actor, nodelist, focusnode, rootnode, revision }` + session cookie Credential.
- Convert to a typed Server message; handler maps nodelist/focus/root to Core launch arguments (registered name + revision + span per **Define the Core Command launch contract**) without changing that Core contract’s locked wording.
- Call Core launch; return public number to the caller.
- Lock Focus only for the job (Focus Node in the lock span); pack/extract may include a larger nodelist.

- [ ] Api.fs accepts Create and produces a typed message.
- [ ] Handler maps to Core launch and returns the public number.
- [ ] Cookie Credential is required; no API key in the body.
- [ ] Focus-only lock span for this job.

## See also

[[../reports/grill-run-agent-actor-2026-09-08.md]], [[05-register-cloud-agent-actor.md]], [[03-seam-after-ask-recognition.md]], [[plan/core-creation/issues/09-define-core-command-launch-contract.md]]
