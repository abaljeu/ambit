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
- [ ] [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]]
- [ ] [[plan/core-creation/issues/02-core-actor-pool.md]]

## Notes
Might be implemented through [[plan\roadmap\epics\operate-connected-channels.md]], making a channel for sending messages to an Agent API.  Agent may asynchronously send info to Ambit.