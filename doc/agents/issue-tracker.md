# Issue tracker: Local Markdown

Issues and specs for this repo live as Markdown files under `plan/`.

## Language

**Issue tracker**: The system holding tracked work. In this repo, it is the local `plan/` Markdown convention.

**Issue**: One tracked unit of work: a bug, task, spec, or implementation slice.

**Decision ticket**: A Wayfinder child issue whose question resolves to a decision rather than an implementation slice.

**Epic**: A standing Roadmap file at `plan/roadmap/epics/<slug>.md` whose resolution is a met user end-goal. It has a **Stage** (same words as a feature-set Project, except steering). **User Epics** and **Developer Epics** may have **Chapters** (named beats) plus **Required for done**. Developer Epic Chapters are optional until charted. Each Chapter uses the raised shape of an implementation issue: **What to build** is major features; the checklist is pointers to Projects or issues that belong to that beat. See [[plan/roadmap/map.md]]. The wayfinder frontier does not scan `epics/`.

**Chapter**: A named beat of an Epic. Not a Stage. Checklist items belong to that beat and are not repeated on Required for done.

**Triage role**: The next-action state assigned during triage, using the mapping in [[doc/agents/triage-labels.md]].

An issue tracker holds issues. An issue carries one triage role at a time. A decision ticket is an issue, but its Wayfinder lifecycle is recorded separately from implementation triage.

Use **issue tracker**, not “backlog backend” or “backlog manager.” Use **issue** except for Wayfinder decision tickets.

## Conventions

- One Project per directory: `plan/<slug>/` (a feature-set Project, or the Roadmap).
- The spec is `plan/<feature-slug>/spec.md`.
- Implementation issues are separate files at `plan/<feature-slug>/issues/<NN>-<slug>.md`, numbered from `01`.
- A `Status:` line records an implementation issue's triage role. `Status: grilling` or `Stage: grilling` on an issue is not a triage role: it is a directive. The next agent that starts or advances that issue must follow [[.agents/skills/grilling/SKILL.md]] on it before any implement work.
- Append comments and conversation under `## Comments`.

## Time tracking

Goal: weeks later, answer **when we started**, **when we finished**, and **how many hours** for a ticket or a whole project, using the files plus conversation and commit history.

### On an issue

Optional fields after `Status:`:

- `Estimate:` — optional forecast (`45m`, `2h`). Omit when unknown. Do not rewrite to match Actual unless Alan asks.
- `Actual:` — sum of `## Time` once any work is logged.

```
## Time

- 2026-09-04 1.5h — sketched apply seam
- 2026-09-05 45m — tests for amend path
```

Prefer logging when a session ends. If a session was never logged, agents may append an inferred line and tag the source: `(from chat)` or `(from commits)`.

### On the project (`project.md`)

After `Updated:`, keep the arc:

```
Started: 2026-09-01
Finished: 2026-09-12
Actual: 14h
```

- `Started:` — set once (earliest wins). Do not clear.
- `Finished:` — set when Stage becomes `done` (or Alan says done). For `dead`, use the retire date; leave blank while live.
- `Actual:` — sum of every issue's `Actual:` / `## Time` under `plan/<slug>/issues/`. Refresh when finishing or when Alan asks.

Optional project-level `## Time` only for reconstruction notes that do not belong on one ticket (e.g. a discuss/review session spanning many tickets).

### How to fill gaps (conversations + commits)

When Alan asks start / finish / hours, or when setting `Finished:`, fill missing fields from evidence — do not invent:

1. **Started** (earliest of what exists):
   - Conversation: Alan says build / go / start implementing for this project (or the discuss→build handoff).
   - Else first commit that touches `plan/<slug>/` or the project's implementation paths after tickets exist.
   - Else first issue `## Time` date or first claim / active work on an issue.
2. **Finished**:
   - Conversation date of done / Stage set to `done`.
   - Else the commit that set `Stage: done`.
3. **Hours**:
   - Sum issue `## Time` lines (authoritative when present).
   - Backfill gaps: cluster commits for that project into sessions (same day, gaps under ~2h count as one session; duration ≈ first→last commit in the cluster, minimum 15m). Tag `(from commits)`.
   - Chat-only work (discuss, review) with no commits: one line from the conversation date and a fair duration, tagged `(from chat)`.
4. Write the filled dates and sums back into `project.md` / issue files so the next ask is file-local.

No separate time database. The Markdown files are the record; git and chat are evidence used to complete them.

## Publishing and fetching

When a skill says “publish to the issue tracker,” create a file under `plan/<feature-slug>/`, creating the directory if needed.

When a skill says “fetch the relevant issue,” read the referenced file. The user will normally provide its path or number.

## Wayfinding operations

The Wayfinder map is one file with one child file per decision ticket.

- **Map**: `plan/<effort>/map.md` holds Notes, Decisions so far, Not yet specified, and Out of scope. The Roadmap also lists open Epics grouped by Stage, each with its current Chapter. Order inside a Stage does not matter.
- **Child decision ticket**: `plan/<effort>/issues/NN-<slug>.md`, numbered from `01`, contains the question. `Type:` records `research`, `prototype`, `grilling`, or `task`; `Status:` records `open`, `claimed`, or `resolved`.
- **Blocking**: `Blocked by: NN, NN` near the top. A decision ticket is unblocked when every listed ticket is resolved.
- **Frontier**: Scan the effort's `issues/` directory for open, unblocked, unclaimed tickets; first by number wins. On the Roadmap, do not treat `epics/` as the frontier.
- **Claim**: Set `Status: claimed` and save before doing any work.
- **Resolve**: Append the resolution under `## Answer`, set `Status: resolved`, then append a one-line gist and link to the map's Decisions so far.
