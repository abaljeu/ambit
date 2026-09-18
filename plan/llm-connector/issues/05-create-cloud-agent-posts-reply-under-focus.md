# 05 — Create cloud-agent posts a reply under Focus

**Status:** cancelled
**Blocked by:** Not applicable — cancelled.

## Context

A person (or a Server test) already has a session cookie and a Focus Node in a Graph. They want Run Agent without the Browser Client this session: they POST Create for ActorName `cloud-agent` with nodelist, focus, root, and revision. The Server must launch work outside Core’s HTTP layer, run the registered Actor, and leave the agent’s reply as Owned children under Focus when the job finishes.

## What to build

Create on `/ambit/actors` starts `cloud-agent`. The Actor builds an Ambit system prompt plus the Focus message plus an Md pack of the launch subgraph, calls CloudAgents with no repo (key from CloudAgents config), and `postChange`s the result as Owned children of Focus. Focus alone is locked for the job. Api.fs turns the POST into a typed message; the handler maps nodelist/focus/root to Core launch without changing **Define the Core Command launch contract**. Composition registers `cloud-agent` outside Core. When the Actor returns, Core finish-drop clears the job.

- [ ] A Create request with cookie and `{ actor: cloud-agent, nodelist, focusnode, rootnode, revision }` returns a public number and starts the job.
- [ ] When the Actor finishes, Focus has new Owned children from the agent result text.
- [ ] Core never sees `CURSOR_API_KEY`; the Actor uses CloudAgents config; no repo is attached.
- [ ] A Server test (or equivalent) proves Create → launch → reply under Focus without the Browser Client.

## See also

[[../reports/grill-run-agent-actor-2026-09-08.md]], [[../reports/first-agent-cursor-cloud-agents.md]], [[03-seam-after-ask-recognition.md]], [[plan/core-creation/issues/09-define-core-command-launch-contract.md]]

## Comments

- 2026-09-11 — Set 3 review closed. Six confirmed misses in [[../../core-creation/reports/actor-pool-rewind-review.md]]: identify work by name; tests must prove Create → reply under Focus; Focus is the replace-parent and lock is Focus (extract/pack is a larger set); paste-replace Focus children, do not append; reply is a nested Md tree that needs ops; do not convert pack errors into agent calls. CloudAgents product was rewound. Do not implement this ticket as a wrap of discarded PR 4.
- 2026-09-11 — **Cancelled.** Draft PR #4 (https://github.com/abaljeu/ambit/pull/4) closed unmerged. This Create slice and the fat vertical-slice issue approach are superseded by the agent redesign. See [[../reports/agent-redesign-locked-2026-09.md]] for locked restart points.
- 2026-09-11 — Reconciled with the controlling implementation record. Do not revive the Create payload, Focus-only lock, Md pack/paste-replace path, or its vertical proof. Chart the replacement through [[06-define-command-run-agent-redesign.md]].
