# 06 — to-spec publishes spec.md only

**Status:** ready-for-agent
**Blocked by:** [[03-write-status-and-stage-lists-into-canonical-docs.md|03 Write Status and Stage lists into canonical docs]], [[04-remove-vendor-merge-and-setup-matt.md|04 Remove the vendor merge and setup-matt bootstrap]]

## Context

`/to-spec` turns a thread into a spec. The skill still says publish to the issue tracker and apply `ready-for-agent`. A spec is not a ticket. Tickets carry Status. A spec does not.

## What to build

An Agent that runs `/to-spec` writes spec.md on the Project. It does not file a spec as a ticket and does not apply `ready-for-agent` to a spec. The spec has no `**Status:**`.

- [ ] to-spec names spec.md on the Project as the publish target.
- [ ] to-spec does not apply `ready-for-agent` or any ticket Status to a spec.

## See also

[[plan/skills-cleanup/reports/lock-bold-properties-spec-md.md]], [[doc/agents/issue-tracker.md]]
