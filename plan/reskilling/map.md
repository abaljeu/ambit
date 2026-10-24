# Reskilling

## Destination

The outcome of this effort is a `plan/context/` folder of `<step>-work.md` files, agent front matter that loads the current step, and skills that kick a step off. `project.md` lists the sequence for an effort. A feature implementation lists the sequence in [[../context/project-work.md]]. Any other step is a defined context named in that list. A correction re-enters the step whose outcome is wrong.

1. **Map.** The problem, the solution, and the scope of the effort. [[../context/map-work.md]]
2. **Spec.** The user stories. [[../context/spec-work.md]]
3. **Arch.** Story paths, the module map, and the slicing strategy in [[../context/arch-work.md]]. The strategy is the direction of the cuts, and it sits at the end of the arch.
4. **Slicing.** The scope of one ticket, then the next, until every slice has a ticket. [[../context/slicing-work.md]]
5. **Ticket.** One ticket file, `plan/<slug>/issues/NN-<ticket-name>.md`. [[../context/ticket-work.md]]
6. **Build.** The source those tickets change. [[../context/build-work.md]]
7. **Review.** One report per ticket to `plan/<slug>/review/NN-<ticket-name>.md`, scored against the stories that ticket named. A miss names what failed, and the ticket returns to build. [[../context/review-work.md]]
8. **Document.** Updates `doc/current` and `doc/user` to match the tickets whose stories were met. `plan/<slug>/doc/NN-<ticket-name>.md` links the pages updated. [[../context/document-work.md]]

A work file is `plan/context/<step>-work.md`. It says where that step's work goes, and it names the template whose headings are copied into the work. Pointer lines stay in the template, and each pointer names its target file. The file is written when that step is about to be worked.

## Scope

This effort has written the map, the spec, and the arch. Its sequence so far is map, spec, arch. The work files live in `plan/context/`. The outcome still to write is the agent front matter and the kickoff skills. The old arch section list is an example of a skeleton. These words stay out of the glossary.
