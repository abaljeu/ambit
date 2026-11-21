# Reskilling architecture

Spec: [[spec.md]]

## 1. Story paths

1. Default sequence — [[../context/project-work.md]] names the sequence. Each step has one work file.
2. Project sequence — Sequence can also name any context that already exists.
3. Same feature — Sequence names the step to re-enter. Later steps read the work artifact that was edited.
4. Map — [[../context/map-work.md]] writes `plan/<slug>/map.md`.
5. Stories — [[../context/spec-work.md]] writes `plan/<slug>/spec.md`.
6. Architecture — [[../context/arch-work.md]] writes `plan/<slug>/arch.md`.
7. Slicing — [[../context/slicing-work.md]] decides one ticket's scope and kicks off [[../context/ticket-work.md]]. The handback starts the next ticket.
8. Self-contained ticket — [[../context/ticket-work.md]] writes `plan/<slug>/issues/NN-<ticket-name>.md` from its template and hands that file back to slicing.
9. Ticket type — A ticket's type is `coding` or `bug-fixing`.
10. Order — Blockers are the order. [[../context/slicing-work.md]] decides them. [[../context/ticket-work.md]] writes them on the ticket.
11. Build — [[../context/build-work.md]] changes the source the ticket names. The implementer reads the ticket and that code.
12. Review — [[../context/review-work.md]] writes one report per ticket to `plan/<slug>/review/NN-<ticket-name>.md`, scored against the stories that ticket named. A miss returns the ticket to build.
13. Docs — [[../context/document-work.md]] updates `doc/current` and `doc/user` for the tickets whose stories were met, and writes `plan/<slug>/doc/NN-<ticket-name>.md` to link the pages updated.
14. Step work — Front matter loads the current step's work file. Kickoff starts that step. The agent loads that step's work file.

## 2. Module map

1. **Context.** One file per step, under `plan/context/`:

   - [[../context/map-work.md]]
   - [[../context/spec-work.md]]
   - [[../context/arch-work.md]]
   - [[../context/slicing-work.md]]
   - [[../context/ticket-work.md]]
   - [[../context/build-work.md]]
   - [[../context/review-work.md]]
   - [[../context/document-work.md]]

   The file says where that step's work goes. It also names the template whose headings are copied into the work.
2. **Sequence.** `plan/<slug>/project.md` lists this effort's steps.
3. **Work.**

   - map: `plan/<slug>/map.md`
   - spec: `plan/<slug>/spec.md`
   - arch: `plan/<slug>/arch.md`
   - slicing: one ticket's scope, handed to ticket
   - ticket: `plan/<slug>/issues/NN-<ticket-name>.md`
   - review: `plan/<slug>/review/NN-<ticket-name>.md`
   - build: the source the ticket names
   - document: `doc/current` and `doc/user`, linked from `plan/<slug>/doc/NN-<ticket-name>.md`
4. **Front matter.** `AGENTS.md` and `.agents/` point an agent at the current step's work file.
5. **Kickoff.** Skills that start a step by reading its work file.

## 3. Slicing strategy

module-build. Each work file is one module. Build the work files in [[../context/project-work.md]] first. Front matter and kickoff come after those files exist.
