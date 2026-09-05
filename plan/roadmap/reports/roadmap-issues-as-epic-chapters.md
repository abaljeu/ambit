# Briefing: roadmap issues as epic chapters (or tickets)

Fact-gathering only. No recommendation to adopt or reject. Proposal under review: make roadmap issues so they are epic chapters, or else tickets toward developing the roadmap.

## 1. Current model

**Issue** — One tracked unit of work: bug, task, spec, or implementation slice ([[doc/agents/issue-tracker.md]]). Files live under `plan/<slug>/issues/` (CONTEXT; agent docs still say `plan/` in places).

**Project** — A `plan/<slug>/` effort. Two kinds only: the **Roadmap** (`steering`), and a **feature-set Project** ([[CONTEXT.md]], [[plan/roadmap/map.md]]). Avoid “epic project” as a third kind.

**Epic** — Standing file under [[plan/roadmap/epics/]] until a marketable end-goal is met. Has a Stage (same words as feature-set Projects, except not `steering`). **User Epic**: Chapters + Required for done. **Developer Epic**: Required for done only, no Chapters. Wayfinder frontier does not scan `epics/` ([[doc/agents/issue-tracker.md]], [[CONTEXT.md]]).

**Chapter** — Named beat of a User Epic (not a Project Stage). **What to build** names major features; checklist points at Projects or issues and does not own them. Advancing a Chapter means charting pointers on owning Projects, not coding on the Roadmap ([[plan/roadmap/map.md]], [[doc/agents/project-status.md]]). `/to-tickets` subdivides a feature-set Project, not the Epic or Chapter list.

**Roadmap** — Steering Project at [[plan/roadmap/]]. Groups Epics by Stage; answers what to work on next. `[[issues/]]` is wayfinder tickets only (map decisions and tasks) per map Notes.

**Wayfinder** — Map + child **decision tickets** (`Type:` research/prototype/grilling/task; `Status:` open/claimed/resolved). Separate from implementation triage roles ([[doc/agents/issue-tracker.md]], [[doc/agents/triage-labels.md]]).

**Stage `tickets`** — Project broken into implementation issues; ready to build ([[doc/agents/project-status.md]]). Distinct from “decision ticket.”

**doc/roadmap/** — Leftover planned-direction text until a Project cites it or it moves to history ([[doc/agents/domain.md]], [[doc/agents/scope-vs-commitment.md]]). Non-authoritative for product commitments.

**Committed Decisions** — [[doc/Decisions/]] has no record about issues vs epics vs roadmap structure (only git hygiene / protocol plus README scope note).

## 2. Vocabulary gaps

| Phrase | Status today |
| --- | --- |
| **Chapter** | Defined: User Epic beat. “Epic chapter” is not a separate glossary term; Chapter already is that. |
| **ticket** | Prefer **issue** except Wayfinder **decision ticket**. Informal speech and Stage name `tickets` still say “ticket.” CONTEXT `_Avoid_`: tickets board. |
| **roadmap issue** | Informal only (e.g. map Decisions: “no Roadmap issue file”). Not a defined kind. |
| **Epic** | Not an issue; not a Project folder. |

## 3. How roadmap work is filed now

1. **Developing the Roadmap** — Wayfinder children under [[plan/roadmap/issues/]] (name Epics, chart Chapters onto Projects, Required-for-done rules, grill options). Example: [[plan/roadmap/issues/07-chart-automatic-upload-and-download.md]] (`Type: task`).
2. **Epics / Chapters** — Standing prose in `epics/<slug>.md`, not issue files. Example: [[plan/roadmap/epics/operate-a-pkm.md]].
3. **Implementation** — Feature-set Project issues (raised shape Chapters copy). Example: [[plan/selective-client-loading/issues/17-represent-unloaded-child-lists-end-to-end.md]].
4. **doc/roadmap leftovers** — Doc-only until cited; not the live tracker.
5. **Exception already on disk** — former Roadmap `issues/14-webview2-navigate-azure-ambit.md` was an implementation issue (`Status:` triage) under Roadmap `issues/` because no App Project folder. Rehomed 2026-09-02 to [[plan/webview2-azure-origin/issues/01-webview2-navigate-azure-ambit.md]].

## 4. Tensions / duplication

- The live model **already splits** “chart/develop the Roadmap” (Wayfinder tickets) from “User Epic Chapters” (beats with pointers) from “implementation issues” (feature-set Projects). The proposal reads largely like a restatement unless it means something stricter.
- **Chapter vs issue shape**: Chapters deliberately reuse the raised shape of an implementation issue but are not issues and are not on the frontier.
- **Path drift**: [[doc/agents/issue-tracker.md]] / [[doc/agents/project-status.md]] still say `plan/`; [[CONTEXT.md]] and live tree use `plan/`.
- **Issue 14** showed Roadmap `issues/` mixed with product slices. That file is now [[plan/webview2-azure-origin/issues/01-webview2-navigate-azure-ambit.md]].
- Out of scope on the map: third Project kind (Epic Projects); implementing feature slices on the Roadmap map.

## 5. Implications if adopted (costs / gains; no silent pick)

**If the intent is taxonomy clarification only** (encode “Roadmap issues are either Chapter-charting / map tickets, or … wait, Chapters aren’t issues”):

- Likely touch: [[doc/agents/issue-tracker.md]], [[CONTEXT.md]], maybe map Notes; rehome or relabel issue 14-class files.
- Gain: less informal “roadmap issue” confusion; clearer frontier.
- Cost: wording pass; decide fate of non-wayfinder files under `plan/roadmap/issues/`.

**If Chapters become issues** (issue files = Chapters):

- Conflicts with: Epics as standing files; frontier must not scan `epics/`; Chapters are not Stages and do not own work; `/to-tickets` stays on feature-set Projects.
- Cost: large model change (tracker, Wayfinder, grilling/charting, Stage vocabulary).
- Gain: only if the human wants Chapters claimable/resolvable like tickets.

**If “roadmap issues” means product roadmap epics filed as plan issues**:

- Conflicts with standing Epic files and “no Epic Project folders.”
- Would duplicate or replace `epics/*.md`.

Ambiguity of the proposal means these are different change sets; do not pick one silently.

## 6. Ambiguities for the parent to ask

1. Does **roadmap issues** mean (a) items on the product Roadmap (Epics/Chapters), (b) files under `plan/roadmap/issues/`, or (c) any `plan/` issue that changes roadmap docs?
2. Does **epic chapters** mean (a) User Epic Chapters as today, (b) turning those Chapters into issue files, or (c) chapter-sized plan-project issues?
3. Are **tickets toward developing the roadmap** (a) Wayfinder chart/grill/task tickets (already the pattern), (b) doc/`doc/roadmap` cleanup tickets, or (c) implementation tickets under an Epic?
4. Should implementation slices like issue 14 stay on Roadmap `issues/`, move to a feature-set Project, or be forbidden there?
5. Is the proposal a **glossary enforcement** of the existing map, or a **structural change**?

## Grilling skill (process only)

[[.agents/skills/grilling/SKILL.md]]: stress-test via design-tree interview rounds; settle prerequisites before acting; do not implement until shared understanding is confirmed. Relevant if this proposal is advanced as a Project/issue in `grilling`.

## Sources (primary)

[[CONTEXT.md]], [[doc/agents/issue-tracker.md]], [[doc/agents/project-status.md]], [[doc/agents/scope-vs-commitment.md]], [[doc/agents/domain.md]], [[plan/roadmap/map.md]], [[plan/roadmap/project.md]], [[plan/index.md]], sample issues and epics under [[plan/roadmap/]], [[doc/Decisions/README.md]].
