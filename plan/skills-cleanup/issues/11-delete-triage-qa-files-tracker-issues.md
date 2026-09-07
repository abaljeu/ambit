# 11 — Delete triage; QA files valid tracker issues

**Status:** ready-for-agent
**Blocked by:** [[03-write-status-and-stage-lists-into-canonical-docs.md|03 Write Status and Stage lists into canonical docs]]

## Context

The triage skill applies GitHub labels, issue numbers, and pull requests. This repo's issue tracker is local Markdown under [[plan/]]. QA is the conversational file-issues skill. Issues it files must be valid tracker files. Ticket skills still own the `/to-tickets` template.

## What to build

The triage skill and its companions are gone. No live skill or index tells an Agent to apply GitHub labels to Markdown files. QA stays. An issue QA files is a valid tracker file: bold `**Status:**` with a locked Status value, and it points at [[doc/agents/issue-tracker.md]] instead of copying a second ticket template.

- [ ] The triage skill and its companions are deleted. Live pointers to `/triage` are gone.
- [ ] QA remains the conversational file-issues skill.
- [ ] An issue QA files carries bold `**Status:**` and a locked Status value. QA does not copy the `/to-tickets` template.

## See also

[[plan/skills-cleanup/reports/lock-delete-triage.md]], [[plan/skills-cleanup/reports/lock-keep-qa.md]]
