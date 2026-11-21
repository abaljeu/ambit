---
name: teach
description: Teach the user a new skill or concept, within this workspace.
disable-model-invocation: true
argument-hint: "What would you like to learn about?"
---

The user has asked you to teach them something. Learning spans multiple sessions. Method: [[WHY.md]].

## Teaching workspace

Treat the current directory as the teaching workspace:

- `MISSION.md` — reason for learning; format [[MISSION-FORMAT.md]]
- `RESOURCES.md` — trusted sources; format [[RESOURCES-FORMAT.md]]
- `NOTES.md` — user preferences and working notes
- `./learning-records/*.md` — non-obvious lessons that steer future sessions; format [[LEARNING-RECORD-FORMAT.md]]; titles `0001-<dash-case-name>.md`
- `./lessons/*.html` — one self-contained HTML **lesson** per tightly-scoped unit; titles `0001-<dash-case-name>.html`
- `./reference/*.html` — compressed reference (cheat sheets, syntax, glossaries); glossary format [[GLOSSARY-FORMAT.md]]
- `./assets/*` — reusable **components** shared across lessons

## Process

### 1. Ground the mission

If `MISSION.md` is missing or the user is unclear, interview them and write it per [[MISSION-FORMAT.md]]. Confirm with the user before changing an existing mission; when it changes, update `MISSION.md` and add a learning record.

Done: `MISSION.md` exists and matches [[MISSION-FORMAT.md]] Rules; the user has confirmed it.

### 2. Stock resources

Before `RESOURCES.md` is well-populated, find high-trust resources and record them per [[RESOURCES-FORMAT.md]]. Prefer primary sources over parametric knowledge.

Done: `RESOURCES.md` lists the sources needed for the next lesson, or a Gaps section names what is still missing.

### 3. Pick the next lesson

Choose one tightly-scoped unit in the user's zone of proximal development: read `./learning-records/`, the mission, and any stated target. Prefer the most mission-relevant unit that fits that zone.

Done: one lesson topic is named, tied to the mission, and justified from learning records or the user's stated target.

### 4. Author the lesson

Before writing, read `./assets/` and reuse components there. New reusable pieces go in `./assets/` (shared stylesheet first). Save one short HTML lesson to `./lessons/` with the next sequential title. Tie it to the mission; stay inside working memory; give one tangible win. Link to other lessons and reference docs via HTML anchors. Cite a primary source from `RESOURCES.md`. Include a reminder to ask follow-up questions. Open the file for the user when a CLI can do so. Design for storage strength (retrieval, spacing, interleaving for skills) per [[WHY.md]].

Done: the lesson file exists, reuses or adds assets correctly, cites a primary source, and is open or linked for the user.

### 5. Update workspace memory

Write learning records when [[LEARNING-RECORD-FORMAT.md]] says to. Add or refresh reference docs (including glossary terms per [[GLOSSARY-FORMAT.md]]) for knowledge that will be revisited. Record teaching preferences in `NOTES.md`.

Done: every new non-obvious insight has a learning record or glossary entry as those formats require; preferences the user stated sit in `NOTES.md`.
