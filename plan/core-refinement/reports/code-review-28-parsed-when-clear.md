# Code review — 28 Parsed when clear

- Range: `git diff HEAD`
- Spec: [28 — Parsed when clear](../issues/28-parsed-when-clear.md)
- Arch: [core-refinement architecture](../arch.md)

## 1. Standards

1. No findings.
2. Mechanical scan stdout was empty. No line-length, file-length, or banned-phrase hit.

## 2. Spec

1. **Extra** — [Graph mutate](../../../src/Shared/GraphMutate.fs) `setParseState` no longer returns the graph unchanged when the requested parse state already matches and `noServerFile` is set, unless the request is Parsed. A matching non-Parsed write now touches `updateTime` and rewrites the node. [28 — Parsed when clear](../issues/28-parsed-when-clear.md) only gates the Parsed write from `ParseFinished`.

## 3. Arch

1. No findings.

## 4. Totals

- Standards: 0 findings.
- Spec: 1 finding. Worst: a matching non-Parsed write on a `noServerFile` node rewrites the node.
- Arch: 0 findings.
