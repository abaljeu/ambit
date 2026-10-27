# Slicing work

## 1. Work

Examine the requirements. Decide the scope of one ticket. Kick off one ticket-creator agent. Receive the handback. Take the next ticket. Stop when every slice has a ticket.

Requirements:

- `plan/<slug>/arch.md`
  - slicing strategy
  - story paths
  - module map
- `plan/<slug>/spec.md`
  - stories

The ticket-creator agent follows [[ticket-work.md]].

The handback is the ticket file that agent wrote.

## 2. Cut and order

The arch sets the direction of the cuts. This step cuts the next slice and orders it.

1. **Read the strategy.** Read the slicing strategy at the end of `plan/<slug>/arch.md`. It is one of `tracer-cut`, `module-build` or `expand-contract`. Ask when that strategy is unnamed.
2. **Cut.** Cut the next slice that strategy names, using the rules below. Size that slice to fit one fresh context window.
3. **Order.** Give that ticket its blockers. A ticket's blockers are the tickets that come before it.
   - The first ticket lists its blockers as none.
   - The last ticket is the one later tickets leave off their blockers.
4. **Number and name.** Number and name that ticket per [[.agents/rules/refer-by-name.md]].

Pass that scope to the ticket-creator agent.

### Tracer cut (`tracer-cut`)

One ticket per story path in the arch.

- Each slice cuts a narrow, complete path through every layer it touches.
- Each finished slice is demoable or verifiable on its own.
- An instruction-file slice is verified by repo search and by the instructions' published behavior.

### Module build (`module-build`)

One ticket per module map entry in the arch.

- Each ticket is one capability that hangs together and is proven by tests.
- Each finished ticket is verifiable once its blockers are done.
- Split two capabilities that test independently. Merge a split that would leave an untestable fragment.
- Add tickets for user-visible behavior after the testable capabilities, when the arch's stories still need them.

### Expand-contract (`expand-contract`)

For a wide mechanical change, such as renaming a column or retyping a shared symbol, whose single edit breaks many call sites.

1. **Expand.** Add the new form beside the old, so every caller still builds.
2. **Migrate.** Move call sites in batches sized by blast radius, such as one package or one folder per batch. Each batch is its own ticket, blocked by the expand. The old form keeps each batch green.
3. **Contract.** Delete the old form once no caller remains. This ticket is blocked by every migrate batch.

When a batch can only be green together with others, the batches share an integration branch, and a final integrate-and-verify ticket is blocked by all of them. Green is promised at that ticket.

### Size

- A ticket is one step: one expand, one change, or one contract.
- A ticket with numbered steps is split into one ticket per step.
- A ticket stays within the [Size limit](size-limit.md).

### Ripple

When a ticket adds a union case or changes a public signature, the scope names every site that changes:

- each match on that union, with its file and function
- each caller of that signature, with its file and function
- each test that calls it, with its file and count

The scope also names the helper that does the same job today, such as the append function or the post function.

### Scope

A ticket's scope is source and tests.

These change in their own steps:

- GLOSSARY
- arch
- spec

Each test name in Outputs is a test that exists or that the ticket adds.

## 3. Done

- the scope is one ticket
- the scope names its cut, its blockers, and its size
- the ticket-creator agent has handed back the ticket file
- the next ticket starts, or every slice the strategy names has a ticket
