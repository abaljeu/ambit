# Status / Stage surface inventory

Fact-finding only for [[plan/skills-cleanup/project.md]]. No skills edited. No grill. No Stage change. No commit.

Scope searched: [[.agents/skills/]], [[.cursor/skills/]], [[.cursor/rules/]], [[doc/agents/]], [[CONTEXT.md]], [[doc/agents/project-status.md]], [[doc/agents/issue-tracker.md]], [[doc/agents/triage-labels.md]], [[.agents/skills/setup-matt-pocock-skills/]], [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/triage/SKILL.md]].

Noise excluded from vocab counts: ordinary English ("stage" in prose), git `status` as a read-only command, git index "stage" as a verb, TDD "review stage", Azure resource lifecycle prose.

## Summary

Nine distinct instruction-surface vocabularies use `Status`, `Stage`, or an equivalent next-action / lifecycle word. Worst collisions: (1) one field name `Status:` carries three enums on issue files (triage vs Wayfinder vs grilling directive); (2) the word `grilling` is a Project/Epic Stage, an issue directive, and a Wayfinder `Type:`; (3) `ready` is a git place and part of triage `ready-for-agent` / `ready-for-human`.

Confirmed known clash: Wayfinder `Status: open|claimed|resolved` vs triage `ready-for-agent` etc. vs Project `Stage:` vs issue `Status: grilling` — all real, and not the whole list.

---

## 1. Project / Epic Stage

**Canonical:** [[doc/agents/project-status.md]]

**Duplicates / pointers:** [[.cursor/rules/project-stage.mdc]] (skill-to-stage transitions); [[.cursor/skills/project-work/SKILL.md]]; [[.cursor/skills/projects-overview/SKILL.md]] (sort order repeats the enum); [[.cursor/skills/to-archive/SKILL.md]]; [[.cursor/rules/gambol.mdc]] (index); [[CONTEXT.md]] (Roadmap / Epic / Chapter / steering glossary); [[doc/agents/issue-tracker.md]] (Epic Stage note); [[.agents/skills/to-feature-tickets/SKILL.md]] (points at project-work for Stage)

**Field:** `Stage:` on `plan/<slug>/project.md`; Epic Stage recorded on the Epic file under [[plan/roadmap/epics/]]

**Allowed values:** `grilling` | `charting` | `steering` | `spec` | `tickets` | `active` | `blocked` | `done` | `dead`

**Entity:** Feature-set Project; Roadmap Project (uses `steering`); Epic (same words except `steering` is forbidden as an Epic Stage — [[CONTEXT.md]])

**Notes:** `grilling` is directive (invoke [[.agents/skills/grilling/SKILL.md]]), then set `charting`. Other values are status-only. Chapter is explicitly not a Stage.

---

## 2. Implementation-issue triage Status

**Canonical (local mapping):** [[doc/agents/triage-labels.md]]

**Role machine (source skill):** [[.agents/skills/triage/SKILL.md]] (five state roles + two category roles)

**Duplicates / copies:** [[.agents/skills/setup-matt-pocock-skills/triage-labels.md]] (vendor table; same five strings); [[.agents/skills/setup-matt-pocock-skills/SKILL.md]] (defaults list); [[.agents/skills/setup-matt-pocock-skills/issue-tracker-local.md]] (`Status:` = triage); [[doc/agents/issue-tracker.md]] (defines triage role → `Status:`); [[.agents/skills/to-tickets/SKILL.md]] / [[.agents/skills/to-feature-tickets/SKILL.md]] (template `**Status:** ready-for-agent`); [[.agents/skills/to-spec/SKILL.md]] (apply `ready-for-agent`); [[.agents/skills/triage/AGENT-BRIEF.md]] / [[.agents/skills/triage/OUT-OF-SCOPE.md]] (consume roles)

**Field:** `Status:` near top of implementation issue under `plan/<feature>/issues/`

**Allowed values (state / next-action):** `needs-triage` | `needs-info` | `ready-for-agent` | `ready-for-human` | `wontfix`

**Parallel category roles (not stored as Status in local docs, but part of the triage machine):** `bug` | `enhancement`

**Entity:** Implementation issue (and external PR when tracker config says yes)

**Notes:** Vendor and live triage-labels tables match on the five strings. Local store is a `Status:` line, not GitHub labels.

---

## 3. Issue grilling directive

**Canonical:** [[doc/agents/project-status.md]] and [[doc/agents/issue-tracker.md]] (both state the exception); enforced by [[.cursor/rules/project-stage.mdc]] and [[.cursor/skills/project-work/SKILL.md]]

**Field:** `Status: grilling` **or** `Stage: grilling` on an **issue** file

**Allowed values:** `grilling` only (as this directive)

**Entity:** Issue (implementation or otherwise under a Project's `issues/`) — not the Project's `project.md` Stage, though the same word and field names appear there

**Notes:** Explicitly not a triage role. Does not change Project Stage. Same word as Project Stage `grilling` and Wayfinder Type `grilling`.

---

## 4. Wayfinder decision-ticket Status (local Markdown)

**Canonical (live):** [[doc/agents/issue-tracker.md]] § Wayfinding operations

**Skill (tracker-agnostic):** [[.agents/skills/wayfinder/SKILL.md]] — speaks open / claimed / closed / unassigned; defers physical encoding to tracker doc

**Vendor copy (incomplete vs live):** [[.agents/skills/setup-matt-pocock-skills/issue-tracker-local.md]] — says `Status:` records `claimed`/`resolved` only (omits `open`), while frontier text still says "open, unblocked, and unclaimed"

**Field:** `Status:` on decision ticket `plan/<effort>/issues/NN-*.md`

**Allowed values (live):** `open` | `claimed` | `resolved`

**Entity:** Wayfinder decision ticket (child of a map)

**Notes:** Same filename tree and same field name `Status:` as triage (#2). Live issue-tracker states decision lifecycle is "recorded separately from implementation triage" but both use `Status:`.

---

## 5. Wayfinder decision lifecycle (GitHub / GitLab encoding)

**Canonical (vendor templates):** [[.agents/skills/setup-matt-pocock-skills/issue-tracker-github.md]], [[.agents/skills/setup-matt-pocock-skills/issue-tracker-gitlab.md]]

**Conceptual source:** [[.agents/skills/wayfinder/SKILL.md]]

**Encoding (not a `Status:` line):**
- Open vs closed: tracker `--state open` / close on resolve
- Claim: assignee (`--add-assignee` / `--assignee`); unclaimed = open + no assignee
- Resolve: comment/note + close

**Entity:** Wayfinder child issue on GitHub or GitLab

**Notes:** Same lifecycle idea as #4, different fields. Collides with ordinary tracker issue open/closed used by triage listing (`gh issue list --state open`).

---

## 6. Wayfinder ticket Type

**Canonical:** [[.agents/skills/wayfinder/SKILL.md]] § Ticket Types; local line in [[doc/agents/issue-tracker.md]]; vendor local/github/gitlab issue-tracker files

**Field:** `Type:` (local) or label `wayfinder:<type>` (GitHub/GitLab)

**Allowed values:** `research` | `prototype` | `grilling` | `task`

**Entity:** Wayfinder decision ticket

**Notes:** `grilling` here means ticket kind (HITL conversation), not Project Stage and not issue directive Status.

---

## 7. ADR Status

**Canonical:** [[.agents/skills/domain-modeling/ADR-FORMAT.md]]

**Pointers:** [[doc/agents/domain.md]] (ADRs under [[doc/Decisions/]]; no enum restated); vendor [[.agents/skills/setup-matt-pocock-skills/domain.md]] (lazy create, no enum)

**Field:** optional Status frontmatter / `Status:` on ADR

**Allowed values:** `proposed` | `accepted` | `deprecated` | `superseded by ADR-NNNN`

**Entity:** Architecture Decision Record

---

## 8. Learning Record Status

**Canonical:** [[.agents/skills/teach/LEARNING-RECORD-FORMAT.md]]

**Field:** optional Status frontmatter / `Status:`

**Allowed values:** `active` | `superseded by LR-NNNN`

**Entity:** Learning record under `learning-records/`

**Notes:** `active` also appears as Project Stage `active` with unrelated meaning.

---

## 9. Git places (commit lifecycle)

**Canonical:** [[.cursor/skills/git-protocol/SKILL.md]]; glossary in [[CONTEXT.md]] (`dev`, `ready`, `master`, agent-done)

**Duplicates / pointers:** [[.cursor/skills/git-share/SKILL.md]], [[.cursor/skills/git-master/SKILL.md]], [[.cursor/rules/environment.mdc]], [[.cursor/rules/gambol.mdc]]

**Field:** branch / place names (not `Status:` / `Stage:`)

**Allowed values:** `dev` | `ready` | `master` (plus workflow token **agent-done**)

**Entity:** Git long-lived branches / promotion places

**Notes:** Word `ready` collides with triage `ready-for-agent` / `ready-for-human`. Not a plan/`Status` field, but a named lifecycle vocabulary agents must obey.

---

## Adjacent surfaces (same words, not plan Status enums)

### A. Product Load stages ([[CONTEXT.md]])

Load sequence stages: Upload, Parse, Fetch (plus Poll). Domain product language. Uses the word "stage"; not agent `Stage:`.

### B. Wizard stages ([[.agents/skills/wizard/SKILL.md]])

Procedural `stage` helpers in a bash wizard (`TOTAL_STAGES`, one focused task per stage). Not Project Stage.

### C. Blocked by (dependency field)

`Blocked by:` on implementation tickets ([[.agents/skills/to-tickets/SKILL.md]], [[.agents/skills/to-feature-tickets/SKILL.md]], [[.agents/skills/qa/SKILL.md]]), Wayfinder tickets ([[doc/agents/issue-tracker.md]]), and Chapters ([[CONTEXT.md]]). Not an enum Status, but collides with Project Stage `blocked`.

### D. Triage category roles

`bug` | `enhancement` in [[.agents/skills/triage/SKILL.md]] — category axis of the same state machine as #2; not written to local `Status:` by [[doc/agents/triage-labels.md]].

---

## Collisions matrix

| Clash | What overlaps | Why it hurts |
| --- | --- | --- |
| `Status:` triple use | Triage (#2), Wayfinder local (#4), grilling directive (#3) | Same field name, three enums; frontier query on `Status: open` misses `ready-for-agent` build work |
| `grilling` triple use | Project Stage (#1), issue directive (#3), Wayfinder Type (#6) | Same word: directive vs lifecycle status vs ticket kind |
| `Stage:` on issues | Project Stage field reused for issue grilling directive (#3) | Issue may carry `Stage: grilling` while Project uses `Stage:` for #1 |
| `open` / `claimed` / `resolved` vs triage | Wayfinder (#4) vs triage (#2) | Both next-action; incompatible value sets on the same `Status:` line |
| Vendor vs live Wayfinder Status | [[.agents/skills/setup-matt-pocock-skills/issue-tracker-local.md]] omits `open`; [[doc/agents/issue-tracker.md]] includes it | Duplicate surface, incomplete enum |
| Local vs remote Wayfinder | `Status:` line (#4) vs open/closed + assignee (#5) | Same skill, different encodings |
| `blocked` / Blocked by | Stage `blocked` (#1) vs `Blocked by:` field (C) | Waiting state vs dependency list |
| `active` | Project Stage (#1) vs LR Status (#8) | Unrelated meanings |
| `ready` | Git place (#9) vs triage ready-for-* (#2) | Unrelated readiness |
| `steering` | Roadmap Project Stage only; forbidden as Epic Stage | Same Stage vocab, entity restriction |
| Chapter vs Stage | [[CONTEXT.md]] / [[doc/agents/issue-tracker.md]] | Chapter is a beat, not a Stage — agents must not treat Chapter as Stage |

---

## File index (instruction surfaces that define or copy these)

| Path | Vocabularies touched |
| --- | --- |
| [[doc/agents/project-status.md]] | #1, #3 |
| [[doc/agents/issue-tracker.md]] | #1 (Epic), #2, #3, #4, #6, Blocked by |
| [[doc/agents/triage-labels.md]] | #2 |
| [[.cursor/rules/project-stage.mdc]] | #1, #3 |
| [[.cursor/skills/project-work/SKILL.md]] | #1, #3 |
| [[.cursor/skills/projects-overview/SKILL.md]] | #1 |
| [[.cursor/skills/to-archive/SKILL.md]] | #1 |
| [[.cursor/skills/git-protocol/SKILL.md]] | #9 |
| [[CONTEXT.md]] | #1, #9, Load stages, Chapter ≠ Stage |
| [[.agents/skills/wayfinder/SKILL.md]] | #4/#5 concepts, #6 |
| [[.agents/skills/triage/SKILL.md]] | #2, categories |
| [[.agents/skills/setup-matt-pocock-skills/triage-labels.md]] | #2 (vendor) |
| [[.agents/skills/setup-matt-pocock-skills/issue-tracker-local.md]] | #2, #4 (incomplete), #6 |
| [[.agents/skills/setup-matt-pocock-skills/issue-tracker-github.md]] | #5, #6 |
| [[.agents/skills/setup-matt-pocock-skills/issue-tracker-gitlab.md]] | #5, #6 |
| [[.agents/skills/to-tickets/SKILL.md]] | #2 |
| [[.agents/skills/to-feature-tickets/SKILL.md]] | #1 pointer, #2 |
| [[.agents/skills/to-spec/SKILL.md]] | #2 |
| [[.agents/skills/domain-modeling/ADR-FORMAT.md]] | #7 |
| [[.agents/skills/teach/LEARNING-RECORD-FORMAT.md]] | #8 |
| [[.agents/skills/wizard/SKILL.md]] | Wizard stages (adjacent) |
| [[.agents/skills/qa/SKILL.md]] | Blocked by (no Status template) |

---

## Count for the grill

**Distinct vocabularies: 9** (#1–#9 above).

**Worst collisions for cleanup:** `Status:` field overload (triage vs Wayfinder vs grilling); `grilling` word overload (Stage vs directive vs Type); vendor local Wayfinder enum missing `open` while live includes it; `ready` git place vs triage ready-for-*.
