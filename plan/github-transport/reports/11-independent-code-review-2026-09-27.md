# 11 — PathPick independent code review — 2026-09-27

**Classification:** Good

## 1. Review range

[Ambit PR 138](https://github.com/abaljeu/ambit/pull/138), commit `c89c12c3`, reviewed as `origin/staging...HEAD`.

## 2. Must-fix

None.

## 3. Nice-to-have

None.

## 4. Standards

No findings. `PathPick` is a pure Shared module, follows the F# source limits and local style, and is in a valid F# project compile position. The test file follows the matching Shared test placement and is included in its project.

## 5. Specification

No findings. `PathPick.choose` returns `Git` for `true` and `Desk` for `false`, stores no durable state, and adds no allowlist or other gate. Both branches have dependency-free tests. The change does not invoke a git process and does not wire Load or Save, add `WorkspaceGit.remoteExists`, add the Peer Actor, or change App hosting.

The [11 — Pick git or desk for plain Load and Save](../issues/11-pick-git-or-desk-for-plain-load-save.md) ticket correctly has Status `coded`, and its completed checks match the implementation and tests. The architecture checks mark the PathPick module and pure seam as delivered while the later WorkspaceGit, Peer Actor, App hosting, and Load/Save wiring checks remain open.

## 6. Verification

1. **Shared build:** `dotnet build tests/Shared.Tests/Gambol.Shared.Tests.fsproj -c Debug` passed with 0 warnings and 0 errors.
2. **PathPick facts:** `dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj -c Debug --no-build --filter "FullyQualifiedName~PathPickTests"` passed 2 of 2 tests.
3. **Full solution gate:** `dotnet test gambol.sln -c Debug --no-build` passed 1,722 tests, skipped 1, and failed the untouched `AmbDocumentTests.read ambiguous owner-link candidates keeps map order` test because its generated `NodeId` differed. No PathPick test failed, and the PR does not change the failing code path.

## 7. Summary

Standards: 0 findings. Specification: 0 findings.
