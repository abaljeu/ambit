# 35b slice 1 — Unfolded Included context id list

Slice 1 of [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md). Status of the whole ticket stays `ready-for-agent`. Do not start slice 2.

## 1. What landed

[IncludedDescendantIds.expand](src/Shared/IncludedDescendantIds.fs) now takes `Graph`, `SiteMap`, and the Zoom root `NodeId`, and returns a flat `NodeId` list. The walk is Fold (SiteMap `expanded`), not residency (`childrenStatus`). Client will call this later to fill Command `graphIds`. Server does not Zoom-expand.

Compile order: [IncludedDescendantIds.fs](src/Shared/IncludedDescendantIds.fs) sits after [ViewModel.fs](src/Shared/ViewModel.fs) so the function can read `SiteMap`.

## 2. Ticket §1 items

1. **Include Zoom root** — the list starts with the requested Zoom root.
2. **Walk unfolded children** — when a SiteEntry is expanded, every Graph child id is included and the walk continues.
3. **Stop at folded children** — a folded SiteEntry is included; its descendants are not.
4. **Ignore ownership** — Owner and Ref children are both included; the walk does not branch on `ChildNode.ref`.
5. **Return ids only** — the result is `NodeId list`.

## 3. Tests

Seam: [IncludedDescendantIdsTests](tests/Shared.Tests/IncludedDescendantIdsTests.fs) against `IncludedDescendantIds.expand`. Fixtures Zoom at a user Node (`buildSiteMapFrom`), not `Graph.root`, so system folders are outside the list.

```
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~IncludedDescendantIdsTests"
```

Passed: 5. Client does not reference this function; Client compile gate was not run.

## 4. Out of scope

[§2 Browser Run](plan/core-creation/issues/35b-browser-run-hello.md), [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md), [§4 CoreMailbox / CoreMsg / CoreActorPool](plan/core-creation/issues/35b-browser-run-hello.md), [§5 TestActor / History / CoreRuntime](plan/core-creation/issues/35b-browser-run-hello.md), [§6 History durability](plan/core-creation/issues/35b-browser-run-hello.md), [§7 Browser proof](plan/core-creation/issues/35b-browser-run-hello.md). No headed Browser proof. No staging publish.
