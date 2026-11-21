# Code review — 61 Session

- Range: `git diff staging`
- Spec: [61 — Move Session off the core mailbox](../issues/61-move-session-off-the-core-mailbox.md)
- Arch: [core-refinement architecture](../arch.md)

## 1. Standards

1. No findings.
2. **Scan** — Printed function sizes stay under the 40-line limit in [F# source](../../../.agents/rules/fsharp-source.md). Changed lines stay within 100 characters. No Banned phrasings hit.

## 2. Spec

1. No findings.
2. **Wait** — A login while admission is closed keeps the previous generation on the same authorities slot, so `finishAdmittedRoute` still completes `waitForAdmittedRoutes` for that generation.

## 3. Arch

1. No findings.

## 4. Totals

- Standards: 0 findings. Worst: none.
- Spec: 0 findings. Worst: none.
- Arch: 0 findings. Worst: none.
