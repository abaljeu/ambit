# Regenerate index after summaries

Regenerated [[plan/index.md]] from every live `plan/*/` `project.md` per [[.cursor/skills/projects-overview/SKILL.md]]. Did not commit.

## Counts

Live directories (`plan/*/`, skip [[plan/done/]]): **33**.

Overview table rows: **33**. Row count equals directory count.

[[plan/debug-reload/]] had no `project.md` at list time. The live file is now Stage `tickets` with Summary `Tell a person on watch how to load debug modules and how to pick up an esbuild rebuild with a hard-reload of the Browser.` The overview uses that line. Did not change any other `project.md`.

## Nine active Summary lines

Copied verbatim from each `project.md`:

- **Client start time:** On App refresh after a prior Session, the Browser shows the Graph from a local IndexedDB snapshot plus stored Changes, then does a Poll, so the user does not wait for `/state` while a blank screen or Loading... is visible.
- **Daily git save:** The Server saves Graph documents in App DataDir. Commit that directory each day so the operator can recover those files from git without a manual commit.
- **Delete Ref:** A person uses a Ref in Children to link to a Node Owned elsewhere in the Graph; this Project makes Delete unlink that appearance from Children and leave the Node in place, and makes Delete of an Owned Node with a self-Ref finish: the command must not hang and must not promote the self-Ref.
- **Event-sourced ops:** Give one semantic standard for how an Actor's Change enters a Graph so every Actor uses the same path and concurrent work merges instead of being refused.
- **Expression Language:** Specify and implement a Prolog-like Expression language in Amble. The language extends path references with a left-to-right word pipeline over the Graph and yields Node, text, and number Answers for Find.
- **Git protocol:** Give this repo one git procedure: ordinary commits on **dev**, merge to **ready** after **Agent-done**, squash to **master**; other instructions point at the skill and do not copy it.
- **Large-node cursor perf:** When one Node has a large Children list in the SiteMap, make Selection, Focus, and delete among the Children stay fast in the Browser.
- **Owner-edge database repair:** Persisted Owned Children can fail to be a tree. After Server restart, every surviving non-ROOT Node has exactly one Owned parent that reaches ROOT, unreachable Nodes are deleted, and a reachable Node with no Owned parent has a Ref promoted to Owned, durable with no History Change.
- **Selective client loading:** Give the Browser a Graph that starts with only the Workspace Nodes needed for ROOT and restored navigation, grow residency only through explicit Load, and keep the Server Graph fully Resident and authoritative.
