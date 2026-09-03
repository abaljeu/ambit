# Chapter: Ask from what I see

**Part of:** [[plan/roadmap/epics/agent-chat-managed-context.md]]
**Blocked by:** None.

## Context

A person chats with an Agent that provides managed context.

## Goal

Run `?` with a message and included context. The reply is Owned children of the focus Node. The call is a long-running Actor: launch, answers arrive while the person works, cancel stops a slow job.

## Required for done

- [ ] [[plan/llm-connector/project.md]] — pack, LLM call, write-back
- [ ] [[plan/expression-language/issues/33-recognize-ask-run-statement.md]] — recognize `?` as a Run statement
- [ ] [[plan/event-sourced-ops/issues/07-generalized-server-actor-produce-path.md]]
- [ ] [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]]

## Notes
