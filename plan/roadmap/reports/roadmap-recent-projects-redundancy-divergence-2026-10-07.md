# Roadmap recent Projects: redundancy and divergence

Date: 2026-10-07. Facts from the Alan locks landed the same day. This note records where two homes said different things, and which home won.

## 1. Find

Server Search Want-fulfillment of Find lives on Chapter [Find what I wrote](plan/roadmap/epics/chapters/find-what-i-wrote.md) as a Required item: [online-search](plan/online-search/project.md). Ambot keeps implement. [Incremental operations](plan/roadmap/epics/chapters/incremental-operations.md) keeps search hydration only (Fetch before navigate when a Find hit is not Resident; [browser-residency](plan/browser-residency/project.md)). Roadmap sessions do not edit `plan/online-search/`.

## 2. Workspace git

[Workspace git](plan/workspace-git/project.md) Stage is `dead` (retired without delivery). The live home is [github-transport](plan/github-transport/project.md) on Chapter [Send to and from GitHub](plan/roadmap/epics/chapters/send-to-and-from-github.md). That Project rejects the retired spec’s non-fast-forward accept of non-overlapping edits. The old Summary stays on the dead file as history.

## 3. Solid core v1 and v2

Solid core v1 is [core-creation](plan/core-creation/project.md) (Stage `done`; Chapter [Initial Core](plan/roadmap/epics/chapters/initial-core.md) Required met). Solid core v2 is [core-refinement](plan/core-refinement/project.md) (Stage `build`; sole Core-seam authority [core-refinement architecture](plan/core-refinement/arch.md)). Epic current Chapter is [Actors supported](plan/roadmap/epics/chapters/actors-supported.md). Leftover core-creation issues continue as pointer tickets [08 — Pointer: Core Actor pool](plan/core-refinement/issues/08-pointer-core-actor-pool.md) through [17 — Pointer: Prove TestActor hello](plan/core-refinement/issues/17-pointer-prove-testactor-hello.md). Source issue files stay history and carry a forward note.

## 4. Parse ownership split

[parse-thread](plan/parse-thread/project.md) owns continuous Parse and Persist product threads. [event-sourced-ops](plan/event-sourced-ops/project.md) owns merge semantics, the Parse File tracer, and advisory soft-lock. The older map line that gave all Parse to event-sourced-ops is replaced. [Actors supported](plan/roadmap/epics/chapters/actors-supported.md) keeps that split: Parse thread on parse-thread; Parse File tracer and advisory soft-lock on event-sourced-ops.
