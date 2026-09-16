# Move Ev for Change visibility

Date: 2026-09-16. First step only: relocate Ev types so History and other Change call sites can name `Gambol.Shared.Ev`. Change stays. No Change-list migration. No `fromOps`. No commit.

## 1. Goal

Put Ev where everyone that uses Change can see it. [[src/Shared/History.fs]] currently defined `Op`, then `Change`, then modules. Ev lived in [[src/Shared/Event.fs]], which compiled after History, so History could not name Ev.

## 2. What moved

1. **Ev type cluster** — `EventId`, `EventId` module (`zero`, `next`, `max`, `value`, `ofRevision`, `toRevision`), `Authority`, `ActorResult`, `ActorStart`, `EventBody`, and `Ev` now sit in [[src/Shared/History.fs]] immediately after `Op`. They do not need the Change record.
2. **Ev module** — `id`, `authority`, `ops`, `isAction`, `target`, `inverseOps`, `asChange`, `ofChange`, `apply` now sit in the same file after `module Change` (before `ChangeValidation`). Helpers that call `Change.apply` / `Change.inverse` stay after that module exists.
3. **Event.fs removed** — Types left that file. F# FS0250 forbids a type and a same-named module in two files of one assembly, so the helpers could not stay in Event.fs. [[src/Shared/Event.fs]] is deleted. [[src/Shared/Gambol.Shared.fsproj]] no longer compiles it. Downstream files still name `Gambol.Shared.Ev`; no open/alias rewrite was required.
4. **Namespace** — Types remain `Gambol.Shared.Ev` (not `Gambol.Shared.Events`). Same names as before the move.

## 3. Why the module is in History.fs

A first try left `module Ev` in Event.fs after moving the types. `dotnet build` of Shared failed with FS0250: a module and a type named `Ev` in two parts of the assembly. F# allows a companion module only in the same file as the type. Agreed placement already allowed helpers later in History.fs. That is the layout that compiles.

## 4. What did not change

1. **Change** — Record, `module Change`, `ChangeValidation`, and `PersistStamp` still exist. PersistStamp still appends to Change lists. No call site switched Change lists to Ev lists.
2. **Ev helpers** — Same functions, same Change bridge (`asChange` / `ofChange`). No `fromOps`.
3. **EventLog / EventJson / ClientHistory** — Stay in their files. They still compile after History and still see Ev.
4. **[[src/Shared/EventId.fs]]** — Uncompiled leftover stub (not in the fsproj). It is a second `EventId` definition. Do not add it to the fsproj; History already owns EventId. Not deleted in this step.

## 5. Arch

[[plan/core-creation/arch.md]] now says Ev types live with Op/Change in [[src/Shared/History.fs]], and the Ev module is in that file after `module Change`. Historical reports under `plan/core-creation/reports/` were not edited.

## 6. File size

[[src/Shared/History.fs]] was already over the 400-line F# file guide. This step added the Ev types and the Ev module (~100 lines). Split is out of scope for this first step.

## 7. Build

`dotnet build src/Shared/Gambol.Shared.fsproj` succeeded: 0 warnings, 0 errors. Shared.Tests and the Client compile gate were not run (WIP-friendly focused Shared build). Full test suite was not run.

## 8. Next (not this step)

Change call sites in History (including PersistStamp) can now name `Ev`. Replacing Change with Ev (`id` → `Ev.id`, `changeId` → `Ev.submissionId`, ops via `Ev.ops`, later a `fromOps` constructor) is a later migration.
