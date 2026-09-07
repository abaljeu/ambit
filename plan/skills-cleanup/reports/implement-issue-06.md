# Implement issue 06 — to-spec publishes spec.md only

`/to-spec` now writes spec.md on the Project. It does not file a spec as a ticket and does not apply `ready-for-agent` or any ticket Status. The spec template still has no `**Status:**`. Product F# was not touched. Sibling-owned files were not edited.

## What changed

- [[.agents/skills/to-spec/SKILL.md]] description and step 3 name spec.md on the Project as the publish target. The spec path stays in [[doc/agents/issue-tracker.md]].
- The skill no longer says publish to the issue tracker. It no longer applies `ready-for-agent` or any other ticket Status.
- Step 3 states that a spec is not a ticket and has no `**Status:**`.
- Ticket [[plan/skills-cleanup/issues/06-to-spec-publishes-spec-md-only.md]] is `**Status:** done`.

## How verified

- [[.agents/skills/to-spec/SKILL.md]] names spec.md on the Project in the description and in step 3.
- The skill file has no `ready-for-agent` and no “publish to the project issue tracker.”
- The in-skill template has no `**Status:**` field.

## Leftover risks

- Step 1 still says respect ADRs. [[CONTEXT.md]] says Committed Decision.
- [[doc/agents/issue-tracker.md]] Language still lists spec as an Issue type. Publishing-and-fetching still describes a generic “publish to the issue tracker.”
- [[.cursor/rules/gambol.mdc]] still does not list to-spec. Ask-Matt still names `/to-spec` without saying spec.md. Ticket 10 owns those files.
- The skill does not tell the agent to set Stage `spec`. Who-writes-Stage stays in [[.cursor/rules/project-stage.mdc]].
- Live specs such as [[plan/owner-edge-db-repair/spec.md]] still carry `Status: ready-for-agent`. Later migrate.
- [[plan/skills-cleanup/project.md]] `Actual:` was not refreshed. This ticket does not own that file.
- `./status.sh` is absent (deleted on this tree). First git step was [[scripts/gitstatus.sh]].
