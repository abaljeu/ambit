# Code review — 51 Build the persist stack

- Range: `git diff origin/staging`
- Spec: [51 — Build the persist stack](../issues/51-stand-the-persist-stack.md)
- Arch: [core-refinement architecture](../arch.md)

## 1. Standards

1. **Note** — [measure-fs-size](../../../.agents/skills/code-review-fsharp/scripts/measure-fs-size.py) printed no function over 40 lines and no line over 100 characters.
2. No findings.

## 2. Spec

1. **Unnamed** — `PersistNodeDeps`. [Persist thread](../../../src/Server/Core/PersistThread.fs) groups `requests`, `write`, and `finish` for the node thread. The ticket names `PersistRequest`, `DocumentPersistChange.persistNode`, and `Notice.NodePersisted`. The record is internal.
2. **Note** — `DocumentPersistChange.persistNode` returns an error when the node is missing or Unparsed. That is a failed write, so the thread does not post `Notice.NodePersisted`.

## 3. Arch

1. No findings.

## 4. Totals

- Standards: 0 findings. The size scan is clean.
- Spec: 1 finding. Worst: `PersistNodeDeps` is an internal record the ticket does not name.
- Arch: 0 findings. The thread writes the request graph and does not edit the graph.
