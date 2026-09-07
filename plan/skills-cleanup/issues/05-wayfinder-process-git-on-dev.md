# 05 — Wayfinder is process; all git stays on `dev`

**Status:** ready-for-agent
**Blocked by:** [[03-write-status-and-stage-lists-into-canonical-docs.md|03 Write Status and Stage lists into canonical docs]], [[04-remove-vendor-merge-and-setup-matt.md|04 Remove the vendor merge and setup-matt bootstrap]]

## Context

Wayfinder charts a huge effort as a map of decision tickets. Its body still copies GitHub-shaped ops (assignee claim, `wayfinder:` labels, close the issue) and teaches a `research/<name>` place. Prototype still teaches a `prototype/<name>` place. This repo's issue tracker is local Markdown. All git work stays on `dev`, then `ready` / `master`.

## What to build

An Agent that runs Wayfinder charts a destination and decides when to research, grill, or prototype. For claim, Type, Status, frontier, and file layout it follows [[doc/agents/issue-tracker.md]] and does not copy those GitHub-shaped ops. It does not teach `research/<name>` or `prototype/<name>` places. Prototype capture stays on `dev`. Ticket Type (`research` | `prototype` | `grilling` | `task`) stays a Type axis, not Status.

- [ ] Wayfinder points at [[doc/agents/issue-tracker.md]] for claim, Type, Status, frontier, and file layout. It does not repeat those ops.
- [ ] Wayfinder does not teach `research/<name>` places or GitHub assignee / label / close verbs.
- [ ] Prototype does not teach `prototype/<name>` places. Capture stays on `dev`.

## See also

[[plan/skills-cleanup/reports/lock-tickets-home-wayfinder.md]], [[plan/skills-cleanup/reports/lock-git-all-on-dev.md]]
