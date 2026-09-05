# Record the Core container Committed Decision

## What I created

[[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]. The number follows [[doc/Decisions/README.md]], which tells me to scan the directory for the highest existing number; 0001 and 0002 exist, so this record is 0003. The name is the sequential-number plus short-title form the two existing records use.

## Structure match

The existing records have no front matter. They are an H1 short title plus a short body. I kept that shape and added two short sections, because [[doc/Decisions/README.md]] permits status, options, or consequences when they give lasting value. The record states the decision, the reasons, and what would replace it.
    10|
## Provisional status

I expressed provisional status with a `Status: provisional` line directly under the H1, plus a closing section that names the replacement triggers. [[doc/Decisions/README.md]] says to add status only when it gives lasting value, so status is a permitted field of the house format and I did not invent a new one. No existing record uses a status field, so there was no earlier spelling to copy; I used the plainest word for the state the user asked for.

## Files updated

- [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]] — new record.
- [[plan/core-creation/project.md]] — one new Committed Decisions section with one link. I did not change any other part of that file.
    20|
No index or register file exists in [[doc/Decisions/]]. [[doc/Decisions/README.md]] describes the format and the numbering rule, and it tells the writer to scan the directory instead of reading a list, so there was nothing to add to.

## Convention conflicts

- [[doc/agents/domain.md]] still says Projects live under `plan/`, but the repo Projects are under `plan/`, and [[CONTEXT.md]] defines a Project as a `plan/<slug>/` effort. I used `plan/`.
- The prompt asked for front matter to copy. The existing records have none, so I followed the repo files.

## Verification

Every file path in the record is a wikilink and none is in backticks. Backticks appear only on code identifiers such as `core.changeAgent.postChange`. No paragraph has an internal linebreak, and no two blank lines are consecutive. I wrote only under [[doc/]] and [[plan/]], and I ran no git command other than the startup status script.
