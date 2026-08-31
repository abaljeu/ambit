# llm-connector

Labels: wayfinder:map

## Destination

Run `?` with a message and included context. The reply is Owned children of the focus Node. The call is a long-running Actor: launch, answers arrive while the person works, cancel stops a slow job.

## Notes

- Enables [[plan/roadmap/epics/agent-chat-managed-context.md]] Chapter **Ask from what I see**.
- This Project owns pack, LLM call, and write-back. [[plan/expression-language/issues/33-recognize-ask-run-statement.md]] only recognizes `?` as a Run statement.
- Long-running Actor: [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/issues/07-generalized-server-actor-produce-path.md]], [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]].
- Glossary: [[CONTEXT.md]] Included context. Do not say Agent for the LLM.

## Decisions so far

- Same Run command, third statement `?` plus a message. Reply is Owned children of the focus Node.
- Included context is SiteMap under Zoom, honoring Fold. Visible is speech, not glossary.
- Project name is llm-connector (renamed from run-ask).

## Not yet specified

- How the pack is encoded for the LLM.
- Which LLM and where credentials live.
- The seam after expression-language recognizes `?`.
- How much of eso 07/09 must land before the first `?` is usable.

## Out of scope

- Follow-up turns, LLM-authored Changes, Graph/file queries, CLI/MCP — later Chapters on the Epic.
- An Epic Project folder.
