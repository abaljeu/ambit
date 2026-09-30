# Code review — 06 Explicit parse command on a File (Load)

Range: uncommitted working tree vs `HEAD`. Spec: [06 — Explicit parse command on a File (Load)](../issues/06-explicit-parse-command-load-file.md), [[../arch.md]] §3 step 2 Expand, [[plan/architecture/server-core.md]] §2 Actors.

## Standards

1. **Hard — grouped parameters** — [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md): group related parameters / “When adding a parameter… extend that type instead of lengthening the argument list.” `makeMailBox` is now six positionals (`credentials`, `persist`, `pool`, `parsePush`, `onError`, `formatError`); `hostWithParsePush` / `startHost` thread the same new free `parsePush` instead of a cohesive seed/deps record.

2. **Judgement — Primitive Obsession / mutable surface** — Also tensions “Don’t use mutable.” Public `ParseStack` exposes implementation cells:
   ```fsharp
   type ParseStack =
       { gate: obj
         items: NodeId list ref
         closed: bool ref }
   ```
   Callers can mutate `items`/`closed` outside `lock`. (Server already uses concurrent mutation elsewhere; standing behind the *public ref record* shape.)

3. **Judgement — Speculative Generality** — `ParseActorDeps.cancel` is always `CancellationToken.None` (production `startParseActor` and tests); stop is only via the handle’s private CTS + `CreateLinkedTokenSource`.

Mechanical scan: no function >40, no new line >100, no file >800 — no scan findings.

## Spec

No findings. Diff matches ticket 06 / arch §3 expand (mailbox Load of a File node → Parse stack push → Actor-thread loop runs `planParseFile`); old parse paths and Persist stay out of scope.

## Summary

Standards: 3 findings (worst: hard grouped-parameters on `makeMailBox` / `parsePush`). Spec: 0 findings.
