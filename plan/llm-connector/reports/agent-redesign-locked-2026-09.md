# Agent Redesign Locked Points (September 2026)

Date: 2026-09-08 through 2026-09-11  
Project: [[../project.md]]  
Related: [[grill-run-agent-actor-2026-09-08.md]], [[first-agent-cursor-cloud-agents.md]]

## Status

**Draft PR #4 closed unmerged** on 2026-09-11. The cloud-agent Create slice (POST `/ambit/actors` / Md reply under Focus) and the fat issue 05 approach are **obsolete** as the build frontier. Core agent project is **restarting** from these locked design points.

PR: https://github.com/abaljeu/ambit/pull/4

## Product Framing: Info Hub

**Ambit is an info hub**: pull in, transform, send out / process / bring back.

- Leverages persist algorithms and adaptive update.
- **Data formats and protocols vary by Actor; the update process is common.**
- Not LLM-only. Examples of the same loop:
  - Agentic interop
  - File upload
  - Email organizer

## Use Cases (Direction)

Three primary patterns:

1. **Chat** — Question is a Focus or Command Node; history and context are packed; answer delivered as Owned children under that Focus/Command.

2. **Expand** — Goblin-like insert details. Instruction may be separate from the outline target (e.g. a link or Ref under the Command pointing to the target Node).

3. **Edit** — Change something in a specified fashion. Same dual-locus pattern as Expand: instruction at one place, target at another.

## Command Node + Run

- Mark a **Command Node**. Before Run, grow children: notes, Refs into the doc, links, etc.
- **Pack / send** = Command Node and its descendants (authoritative), not "everything visible" unless the client explicitly puts that in the set.
- **Run logic**: If Focus is not a Command, walk **ancestors** to find a Command; fail if none. That Command is what runs.
  - **Focus** = where Run was pressed.
  - **Command** = self or nearest command ancestor.

## Soft Lock / Working Set

- Client specifies the set under the Command ("visible" / working items) to the server: **lock this set**.
- Soft lock is **not** a hard fence: anyone can still edit elsewhere; expect merge conflicts.
- Launched Actor works on a **local Graph copy**, submits from that copy. Others may work concurrently; master merge uses **existing Core conflict resolution**.
- Soft lock marks the job's working/merge domain and edit-authority intent — not exclusive UI blocking.
- Avoid making "is locked?" a live transitive owner+Ref graph query. Membership in the client-supplied job set is the cheap check.

## Agent Actor Wire (Locked 2026-09-10)

**Text ↔ Text + Text-Based Update**

- Agent Actor sends **simple formatted text**; agent returns **formatted text**.
- Apply via **text-based update**, not node/id-based replace-add-delete.
- **No explicit edit-with-references** on this path for now.
- Merge recognizing node reuse may exist later — **out of scope**.

**Format Selection**

- Pack/output should **match the owning file's format**.
- Amble has no serialization today.
- Ambit file format is complete but not ideal as an LLM hand-edit dialect.
- Agent path uses **text in the owning format** instead.

**XML ID Protocol Superseded**

Earlier XML id protocol brainstorm (from prior sessions) is **superseded** for the agent Actor by text↔text + text apply. May still inform other Actors later.

## Naming (Product vs Source)

- **Ambit** = product name (SaaS, public-facing).
- **Gambol** = source directory name only. Emphatically **not** a public-facing product name or term. Internal use only.

## Explicitly NOT Locked

The following remain **open brainstorm** only:

- Split-pane UI: left doc / right meta.
- Whether HTML/text/graph surfaces are pluggable pane types.
- Hard lock semantics.
- Full Expand/Edit Ref surgery protocol: how instruction references target; detailed ops for inserts/replacements across the outline.

## Implementation Note

Do **not** generate new implementation tickets from this report. These are locked architectural and product direction points for the restart. Tickets will emerge from the revised design work.
