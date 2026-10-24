# Reskilling

## User stories

1. Default sequence — As a person directing a feature, I want the sequence to be map, spec, arch, ticket, build, review, document, so that the feature is defined, built, checked, and documented in that order.
2. Project sequence — As a person directing an effort, I want `project.md` to list the sequence, so that I can include a context we have already defined.
3. Same feature — As a person directing an effort, I want a correction to re-enter the step whose outcome is wrong, so that the repair continues through the later steps.
4. Map — As a person directing an effort, I want the map to state the problem, the solution, and the scope, so that later steps build that outcome.
5. Stories — As a person directing an effort, I want the spec to list named user stories, so that each later step can point at a story.
6. Architecture — As a person directing an effort, I want story paths, a module map, and a slicing strategy, so that tickets cut the feature in a known direction.
7. Self-contained ticket — As an implementer, I want a ticket to name its blockers, the files, types, and functions to look at, the changes, and what done looks like, so that I can build without opening the map, the spec, or the arch.
8. Ticket type — As an implementer, I want a ticket to be `coding` or `bug-fixing`, so that I know whether the change adds behavior or corrects it.
9. Order — As an implementer, I want blockers to say which tickets come first, so that I start on a ticket whose blockers are met.
10. Build — As an implementer, I want the build to change the source the ticket names, so that the ticket's changes exist in the code.
11. Review — As a person directing an effort, I want one review per ticket scored against the stories that ticket named, so that a miss sends that ticket back to build.
12. Docs — As a later reader, I want `doc/current` and `doc/user` updated for the tickets whose stories were met, so that the docs match what was achieved.
13. Step work — As a person directing an effort, I want each step's work file to decide what gets built, so that the agent loads that step's files and not the whole effort.
