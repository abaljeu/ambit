# Issue tracker: Local Markdown

Issues and specs for this repo live as Markdown files under `.scratch/`.

## Language

**Issue tracker**: The system holding tracked work. In this repo, it is the local `.scratch/` Markdown convention.

**Issue**: One tracked unit of work: a bug, task, spec, or implementation slice.

**Decision ticket**: A Wayfinder child issue whose question resolves to a decision rather than an implementation slice.

**Epic**: A standing Roadmap file at `.scratch/roadmap/epics/<slug>.md` whose resolution is a met user end-goal. It has a **Stage** (same words as a feature-set Project, except steering). Person-job Epics have **Chapters** (named beats) plus **Required for done**. Home Epics have Required for done and no Chapters. Each Chapter uses the raised shape of an implementation issue: **What to build** is major features; the checklist is pointers to Projects or issues that belong to that beat. See [[.scratch/roadmap/map.md]]. The wayfinder frontier does not scan `epics/`.

**Chapter**: A named beat of a person-job Epic. Not a Stage. Checklist items belong to that beat and are not repeated on Required for done.

**Triage role**: The next-action state assigned during triage, using the mapping in [[docs/agents/triage-labels.md]].

An issue tracker holds issues. An issue carries one triage role at a time. A decision ticket is an issue, but its Wayfinder lifecycle is recorded separately from implementation triage.

Use **issue tracker**, not “backlog backend” or “backlog manager.” Use **issue** except for Wayfinder decision tickets.

## Conventions

- One Project per directory: `.scratch/<slug>/` (a feature-set Project, or the Roadmap).
- The spec is `.scratch/<feature-slug>/spec.md`.
- Implementation issues are separate files at `.scratch/<feature-slug>/issues/<NN>-<slug>.md`, numbered from `01`.
- A `Status:` line records an implementation issue's triage role.
- Append comments and conversation under `## Comments`.

## Publishing and fetching

When a skill says “publish to the issue tracker,” create a file under `.scratch/<feature-slug>/`, creating the directory if needed.

When a skill says “fetch the relevant issue,” read the referenced file. The user will normally provide its path or number.

## Wayfinding operations

The Wayfinder map is one file with one child file per decision ticket.

- **Map**: `.scratch/<effort>/map.md` holds Notes, Decisions so far, Not yet specified, and Out of scope. The Roadmap also lists open Epics grouped by Stage, each with its current Chapter. Order inside a Stage does not matter.
- **Child decision ticket**: `.scratch/<effort>/issues/NN-<slug>.md`, numbered from `01`, contains the question. `Type:` records `research`, `prototype`, `grilling`, or `task`; `Status:` records `open`, `claimed`, or `resolved`.
- **Blocking**: `Blocked by: NN, NN` near the top. A decision ticket is unblocked when every listed ticket is resolved.
- **Frontier**: Scan the effort's `issues/` directory for open, unblocked, unclaimed tickets; first by number wins. On the Roadmap, do not treat `epics/` as the frontier.
- **Claim**: Set `Status: claimed` and save before doing any work.
- **Resolve**: Append the resolution under `## Answer`, set `Status: resolved`, then append a one-line gist and link to the map's Decisions so far.
