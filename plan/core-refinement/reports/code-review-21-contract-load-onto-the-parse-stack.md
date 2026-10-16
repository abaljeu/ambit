# Code review — 21 Contract Load onto the Parse stack

- Range: `git diff HEAD~1` at `b375dbba`
- Spec: [21 — Contract Load onto the Parse stack](../issues/21-contract-load-onto-the-parse-stack.md)
- Arch: [core-refinement architecture](../arch.md)

Alan accepted the pull request. Ticket Status is `done`.

## 1. Standards

1. **Hard** — [markdown writing](../../../.agents/rules/markdown-writing.md): "Always read GLOSSARY.md files and use their ubiquitous language." [Parse and persist](../../../doc/current/parse-persist.md) and [21 — Contract Load onto the Parse stack](../issues/21-contract-load-onto-the-parse-stack.md) said "Git Load posts the Workspace onto the Parse stack." [GLOSSARY.md](../../../GLOSSARY.md) **Workspace Node** says always say Workspace Node. The mailbox posts an Event. That claim already says a Load pushes the subject onto the Parse stack.
2. **Hard** — [markdown writing](../../../.agents/rules/markdown-writing.md): "Always read GLOSSARY.md files and use their ubiquitous language." [File mapping](../../../doc/user/guide/file-mapping.md) said "Load of a workspace, a directory, or a file." Those Load subjects are a Workspace Node, a Directory Node, and a File Node.
3. Scan: none. No limit break.
4. Note — Tip `27d493d2` renames those phrases. The two Hard items are closed on the tip.

## 2. Spec

1. No findings.

## 3. Arch

1. **Blocker** — [core-refinement architecture](../arch.md) §3 **Parse stack** Expand. [Desk load push](../../../src/Server/Core/DeskLoadPush.fs) `subjectToLoad`. A web desk Load's subject is a Workspace Node, a Directory Node, or a File Node, and the parse push marks that subject Unparsed and then pushes it. `subjectToLoad` returns no subject when `holdStack` is set, so that Desk Load neither marks Unparsed nor pushes. On a desktop-mapped Load the first command can also carry an inner-node focus.
2. Note — §3 **Parse stack** Contract holds. The follow-up Load has `holdStack` clear and pushes through mailbox Load. Desktop completion no longer calls `parseFileOp` or posts directory reconcile.
3. Note — §3 **Parse stack** Migrate, **Parse push marks Unparsed**, holds. The Load command does not mark Unparsed. A push that runs still marks, then pushes.
4. Note — §5 **Load on File** and **Load on Directory** hold. The follow-up subject is that File Node, Directory Node, or Workspace Node. A node inside a File Node uses that File Node.

## 4. Totals

- Standards: 2 findings. Worst: the Load claim said Workspace instead of Workspace Node.
- Spec: 0 findings.
- Arch: 1 finding. Worst: a Desk Load with `holdStack` set does not push.
