# Reskilling architecture

Spec: [[spec.md]]

## 1. Story paths

1. Default sequence — Sequence writes map, spec, arch, ticket, build, review, document. Each step has one work file.
2. Project sequence — Sequence can also name any context that already exists.
3. Same feature — Sequence names the step to re-enter. Later steps read the work artifact that was edited.
4. Map — `context/map-work.md` writes the problem, the solution, and the scope to `plan/<slug>/map.md`.
5. Stories — `context/spec-work.md` writes the named user stories to `plan/<slug>/spec.md`.
6. Architecture — `context/arch-work.md` writes the story paths, the module map, and the slicing strategy to `plan/<slug>/arch.md`.
7. Self-contained ticket — `context/ticket-work.md` writes each ticket's blockers, the files, types, and functions to look at, the changes, and what done looks like to `plan/<slug>/issues/NN-<ticket-name>.md`.
8. Ticket type — A ticket's type is `coding` or `bug-fixing`.
9. Order — Blockers on the tickets are the order. The tickets are the work of `context/ticket-work.md`.
10. Build — `context/build-work.md` changes the source the ticket names. The implementer reads the ticket and that code.
11. Review — `context/review-work.md` writes one report per ticket to `plan/<slug>/review/NN-<ticket-name>.md`, scored against the stories that ticket named. A miss returns the ticket to build.
12. Docs — `context/document-work.md` updates `doc/current` and `doc/user` for the tickets whose stories were met, and writes `plan/<slug>/doc/NN-<ticket-name>.md` to link the pages updated.
13. Step work — Front matter loads the current step's work file. Kickoff starts that step. The agent does not load the other work files.

## 2. Module map

1. **Context.** One file per step: `context/map-work.md`, `context/spec-work.md`, `context/arch-work.md`, `context/ticket-work.md`, `context/build-work.md`, `context/review-work.md`, and `context/document-work.md`. The file says where that step's work goes. It also names the template whose headings are copied into the work.
2. **Sequence.** `plan/<slug>/project.md` lists this effort's steps.
3. **Work.** Map, spec, and arch go to `plan/<slug>/map.md`, `plan/<slug>/spec.md`, and `plan/<slug>/arch.md`. Tickets go to `plan/<slug>/issues/NN-<ticket-name>.md`. Reviews go to `plan/<slug>/review/NN-<ticket-name>.md`. Build's work is the source the ticket names. Document's work is `doc/current` and `doc/user`, linked from `plan/<slug>/doc/NN-<ticket-name>.md`.
4. **Front matter.** `AGENTS.md` and `.agents/` point an agent at the current step's work file.
5. **Kickoff.** Skills that start a step by reading its work file.

## 3. Slicing strategy

module-build. Each work file is one module. Build the seven work files first. Front matter and kickoff come after those files exist.
