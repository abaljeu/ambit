# Project work

Step context for a Project lives here.

## Folder map

- [[project-work.md]] — the list of step elements
- [[map-work.md]] — map
- [[spec-work.md]] — spec
- [[arch-work.md]] — arch
- [[ticket-work.md]] — ticket
- [[build-work.md]] — build
- [[refactor-work.md]] - refactor
- [[review-work.md]] — review
- [[document-work.md]] — document

## Routing
When working on a ticket, first set the state to your task

| Task   | Instruction |
|------  |-------------|
| code   | build-work.md |
| review | review-work.md|
| document | document-work.md|

- You want to review a ticket: [[review-work.md]]
- You want to understand the sequence of steps for this project:  [[project-work.md]]
- You want one step's work file and template: that step's work file

## Handoff

No folder under context/ has its own CONTEXT.md.

This file is the router.

This file names the step elements.

A future Project's CONTEXT.md names which of these elements apply, in that Project's sequence.

That file is written when that Project is about to be worked.

This file does not create those Project files.

## Elements

1. **map.** [[map-work.md]]. The work file is `plan/<slug>/map.md`. It states the problem, the solution, and the scope.
2. **spec.** [[spec-work.md]]. The work file is `plan/<slug>/spec.md`. It lists the user stories. The Map pointer names `map.md`.
3. **arch.** [[arch-work.md]]. The work file is `plan/<slug>/arch.md`. It holds the story paths, the module map, and the slicing strategy.
4. **ticket.** [[ticket-work.md]]. Each ticket is `plan/<slug>/issues/NN-<ticket-name>.md`.
5. **build.** [[build-work.md]]. The work is the source the ticket names.
6. **review.** [[review-work.md]]. One report per ticket, at `plan/<slug>/review/NN-<ticket-name>.md`.
7. **document.** [[document-work.md]]. Update `doc/current` and `doc/user`. The link file is `plan/<slug>/doc/NN-<ticket-name>.md`.
