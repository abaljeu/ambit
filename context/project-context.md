# Project context

This file names the step elements.

A future Project's CONTEXT.md names which of these elements apply, in that Project's sequence.

That file is written when that Project is about to be worked.

This file does not create those Project files.

## Elements

1. **map.** [[map-context.md]]. The work file is `plan/<slug>/map.md`. It states the problem, the solution, and the scope.
2. **spec.** [[spec-context.md]]. The work file is `plan/<slug>/spec.md`. It lists the user stories. The Map pointer names `map.md`.
3. **arch.** [[arch-context.md]]. The work file is `plan/<slug>/arch.md`. It holds the story paths, the module map, and the slicing strategy.
4. **ticket.** [[ticket-context.md]]. Each ticket is `plan/<slug>/issues/NN-<ticket-name>.md`.
5. **build.** [[build-context.md]]. The work is the source the ticket names.
6. **review.** [[review-context.md]]. One report per ticket, at `plan/<slug>/review/NN-<ticket-name>.md`.
7. **document.** [[document-context.md]]. Update `doc/current` and `doc/user`. The link file is `plan/<slug>/doc/NN-<ticket-name>.md`.
