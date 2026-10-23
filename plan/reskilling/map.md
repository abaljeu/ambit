# Reskilling

## Destination

The outcome of this effort is a `context/` folder of `<step>-context.md` files, agent front matter that loads the current step, and skills that kick a step off. `project.md` lists the sequence for an effort. A feature implementation lists map, spec, arch, ticket, build, review, document. Any other step is a defined context named in that list. A correction re-enters the step whose outcome is wrong.

1. **Map.** The problem, the solution, and the scope of the effort.
2. **Spec.** The user stories.
3. **Arch.** Story paths, the module map, and the slicing strategy. The strategy is tracer-cut, module-build, or expand-contract. It is the direction of the cuts, and it sits at the end of the arch.
4. **Ticket.** One context. Each ticket is `plan/<slug>/issues/NN-<ticket-name>.md`. It has type `coding` or `bug-fixing`, blockers, the files, types, and functions to look at, the changes to make, and what done looks like. Blockers are the order. The implementer needs the ticket and the code.
5. **Build.** The source those tickets change.
6. **Review.** One report per ticket to `plan/<slug>/review/NN-<ticket-name>.md`, scored against the stories that ticket named. A miss names what failed, and the ticket returns to build.
7. **Document.** Updates `doc/current` and `doc/user` to match the tickets whose stories were met. `plan/<slug>/doc/NN-<ticket-name>.md` links the pages updated.

A context file is `context/<step>-context.md`. It says where that step's work goes, and it names the template whose headings are copied into the work. Pointer lines stay in the template, and each pointer names its target file. The file is written when that step is about to be worked.

## Scope

This effort has written the map, the spec, and the arch. Its sequence so far is map, spec, arch. The outcome still to write is the `context/` folder, the agent front matter, and the kickoff skills. The old arch section list is an example of a skeleton. These words stay out of the glossary.
