# Chapter: Ambit as bot DM channel

**Part of:** [[plan/roadmap/epics/operate-connected-channels.md]]
**Blocked by:** None.

## Context

A person wants Slack-like direct messaging with Grok Bots without Slack in the middle. Ambit already folds and organizes text; bots already wake on webhooks (Admiral hub). Cognition should stay in Ambit; files stay on Origin.

## Goal

Ambit is the channel: messages wake a bot, durable replies land in the same Ambit thread, and Git/Origin remains the file backend.

## Required for done

- [ ] [[plan/bot-channel/project.md]] — wake path, reply write API, threading, Origin boundary, first vertical slice
- [ ] First vertical slice proven end-to-end (wake POST + one durable reply in-thread)

## Notes

- Distinct from [[plan/roadmap/epics/chapters/ask-from-what-i-see.md]] / [[plan/llm-connector/project.md]] (`?ai` CloudAgents).
- Credentials: Alan copies webhook URL/key from Admiral’s `ambit-inbound-hub` panel into Ambit config; do not block charting on Nectar.
