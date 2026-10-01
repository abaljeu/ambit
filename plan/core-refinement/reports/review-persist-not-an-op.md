# Code review — persist is not an operation

Range: uncommitted working tree vs `HEAD`. Spec: [04 — Parsed/Unparsed and Persisted/Unpersisted](plan/core-refinement/issues/04-parsed-unparsed-and-persisted-unpersisted.md) and [arch.md](plan/core-refinement/arch.md) §6.

## Standards

No findings.

## Spec

**(a)** An op request can set the parsed value. Acceptance: "it should not be possible to set the core's own parsed or persisted values through like an op request." Issue: "Parsed is the other pole of Unparsed (`Current` is today’s name)." `Op.SetDocumentState` applies through `Node.withDocumentState`, which writes `parseState` from `DocumentState`. The persisted op is gone. This requirement is partial.

**(d)** The diff removes the shared DU case `Op.SetPersistState`. The spec does not mention that type or interface. The issue names the axes and `DocumentState`, not `Op`.

## Summary

Standards: 0 findings. Spec: 2 findings (worst: an op request can set the parsed value through `Op.SetDocumentState`).
