# Cursor repo to Ambit mobile to LLM — Epic options

This report presents **options**. No option is locked. Epic files hold a **recommended draft** in Notes only. Confirm: options were presented (axes below, then a recommended default). HITL pick: [[plan/roadmap/issues/13-grill-cursor-repo-to-ambit-llm-use.md]].

Target use: a person works with Cursor in **another repo** (agent and documents there); maps that repo to an Ambit **Workspace**; wants **automatic Upload** so the Server stays current; then works in **Ambit** (including a mobile **Browser**); then re-engages with an LLM (Grok or another) **from Ambit**.

Glossary: [[CONTEXT.md]]. Ambit is the SaaS. Gambol is the repo name. Do not invent a third Epic kind.

## What changed on disk

- [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]] — Notes name the related use, list options, and a recommended default. **No new Chapter.** Current Chapter stays **Automatic upload and download**. Issue [[plan/roadmap/issues/07-chart-automatic-upload-and-download.md]] stays the auto-upload pointer. Mapping stays Current ([[doc/current/workspace-local-mapping.md]]). Mobile stays Required ([[plan/client-start-time/project.md]]).
- [[plan/roadmap/epics/agent-chat-managed-context.md]] — Notes name the in-Ambit LLM step, list options, and a recommended default. **No new Chapter.** Current Chapter stays **Ask from what I see**. Owning Project stays [[plan/llm-connector/project.md]].
- [[plan/roadmap/map.md]] — **Not yet specified** points at this report and issue 13. No Decision until HITL.
- [[plan/roadmap/issues/13-grill-cursor-repo-to-ambit-llm-use.md]] — grilling ticket to pick an option.
- [[plan/roadmap/project.md]] — `Updated: 2026-09-01`. Stage stays **steering**. [[plan/index.md]] was not regenerated.

No product code. No third Epic kind. No new User Epic file. No duplicate of issue 07.

## How the target maps onto beats (facts, not a pick)

| Step | In product today / on Roadmap | Default home if you keep two User Epics |
| --- | --- | --- |
| Cursor in another repo | Outside Ambit | Starting condition. Not a Chapter. Later: **Act through CLI or MCP** if Ambit must drive that repo’s tools. |
| Map repo to Workspace | Current: [[doc/current/workspace-local-mapping.md]], [[doc/current/desktop-local-files.md]] | Not a Chapter unless you choose that. Wiki already names mapping: [[plan/end-user-wiki/issues/01-describe-documents-from-any-connected-device.md]]. |
| Auto-sync App → Server (currency) | Current Chapter **Automatic upload and download**. Auto-download: [[plan/auto-download-persisted-files/project.md]] (HITL tabled). Auto-upload: no Project; chart on issue 07. | Keep that Chapter. Do not file a second auto-upload ticket. |
| Work in Ambit / take the repo mobile | Browser talks to the same Server. Required: [[plan/client-start-time/project.md]] (primarily mobile). | Required for done, not a new Chapter, unless you choose that. |
| LLM from Ambit | Current Chapter **Ask from what I see**. Project: [[plan/llm-connector/project.md]]. | Keep that Chapter. Vendor name is not glossary; say LLM. |

**Sync** in glossary is Graph Actions, not Load. The currency want is automatic **Upload** (and Download), not Poll.

## Option axis 1 — which User Epic(s)

### A. Expand both existing User Epics (recommended default)

Documents / Workspace / currency / mobile stay on [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]. In-Ambit LLM stays on [[plan/roadmap/epics/agent-chat-managed-context.md]].

Gives: matches existing Epic shape (*A person …*); one loop, two marketable goals; no third kind.

Avoids: a mega-Epic; folding chat into files; a Developer Epic for an end-user job.

Tradeoff: the loop is not one Roadmap row. Sessions must name both Epics.

### B. Expand only documents-from-anywhere

Put every step, including in-Ambit LLM, on that User Epic.

Gives: one file for this loop.

Avoids: a new Epic file.

Tradeoff: collides with agent-chat already charted; Required lists would overlap [[plan/llm-connector/project.md]]; marketable “chat with managed context” becomes a Chapter of “documents from anywhere.”

### C. Expand only agent-chat

Put mapping, auto-upload, and mobile on agent-chat.

Gives: one file if you treat the loop as “keep talking to an LLM.”

Avoids: a new Epic file.

Tradeoff: agent-chat is *inside Ambit* ([[plan/roadmap/issues/04-chart-agent-chat-managed-context.md]]). Mapping and Upload are the disk channel ([[plan/roadmap/reports/hub-epic-framing.md]]). First Chapters of agent-chat would wait on files work they do not own.

### D. New User Epic for the whole loop

Opening line example: *A person brings a repo from another tool into Ambit, keeps files current, works on any device, then asks an LLM in Ambit.*

Gives: one marketable name for this pattern.

Avoids: stretching two Epics’ Notes.

Tradeoff: splits “documents from anywhere” and “agent chat” or duplicates their Chapters. Map forbids a third Epic **kind**, not a new User Epic file — but the standing list already covers the jobs. Risk: Epic Project folder pressure (out of scope).

### E. Developer Epic to home the loop

Gives: a Required-for-done bucket with no Chapters.

Avoids: a false Chapter story.

Tradeoff: this is an end-user pattern. Developer Epics serve developers ([[plan/roadmap/epics/organize-huge-outlines.md]], [[plan/roadmap/epics/robust-outliner.md]]). Wrong kind.

## Option axis 2 — how Chapters split the loop

### F. No new Chapters (recommended default)

Keep current Chapters. Put the loop in Notes. Point at existing checklists.

Gives: no overlap with Required for done; mapping stays Current; mobile stays Required.

Avoids: Chapters that are already met (map) or not one beat (Cursor-outside).

Tradeoff: the loop is not a Chapter title on the map’s current-Chapter line.

### G. New Chapters on documents-from-anywhere

Examples: **Map a Workspace**; **Work on a phone**; keep auto-sync as now.

Gives: named beats for map and mobile.

Avoids: hiding map/mobile in Notes.

Tradeoff: map is Current — a Chapter would be documentation or polish, not a feature. Mobile is already Required via client-start-time. Chapter vs Required overlap if you also keep that Project on Required.

### H. One Chapter per step of the loop (five beats)

Cursor-outside; map; auto-sync; Ambit-direct/mobile; LLM.

Gives: a storyboard.

Avoids: ambiguity about order.

Tradeoff: first beat is outside Ambit. LLM beat duplicates agent-chat. Five beats across two Epics or one new Epic (axis 1).

### I. New Chapter only for “work in Ambit after currency”

Gives: a beat after auto-sync (Browser as the place of work).

Avoids: treating auto-sync as the whole job.

Tradeoff: “work in Ambit” is already the Epic opening line. Thin Chapter unless it names a missing feature.

## Option axis 3 — auto-sync Chapter vs new Chapter

### J. Keep **Automatic upload and download** as current Chapter (recommended default)

Wire to [[plan/roadmap/issues/07-chart-automatic-upload-and-download.md]] and [[plan/auto-download-persisted-files/project.md]].

Gives: currency is already the current Chapter. No duplicate ticket.

Avoids: a second auto-upload chart.

Tradeoff: Chapter also covers Download. The stated want is App → Server (Upload). Download remains in the same Chapter (HITL tabled).

### K. New Chapter **Automatic upload** (Download stays or splits)

Gives: Upload-only currency for this use.

Avoids: mixing tabled auto-download HITL with auto-upload charting.

Tradeoff: splits a Chapter that grilling already named ([[plan/roadmap/issues/06-chart-work-with-text-files-from-anywhere.md]]). Issue 07 would need a new Question or a sibling ticket.

### L. Auto-sync is Required, not a Chapter

Gives: Chapter list stays document classes (parse, styling, image, tables).

Avoids: mixing transport with document UX Chapters.

Tradeoff: reverses issue 06 (current Chapter is auto-sync). The target’s currency step would lose the map’s current-Chapter highlight.

## Option axis 4 — in-Ambit LLM vs llm-connector vs Developer Epic

### M. User Epic Chapter **Ask from what I see** + llm-connector (recommended default)

Gives: already charted. Run `?`, Included context, Owned-child reply, Actor. Vendor-neutral.

Avoids: a Grok-named Epic; a Developer Epic for an end-user chat job.

Tradeoff: first `?` is not “Grok with Cursor-style tools.” **Change the Graph**, **Query the Graph or the files**, and **Act through CLI or MCP** stay later.

### N. Later Chapter as the “re-engage” beat (**Talk again** or **Change the Graph**)

Gives: matches “continue working” after Cursor, not a one-shot `?`.

Avoids: shipping `?` as if it were the full Cursor replacement.

Tradeoff: blocks on **Ask from what I see**. This use’s last step would wait.

### O. **Act through CLI or MCP** is the Cursor-shaped beat

Gives: Ambit drives tools like the other-repo agent.

Avoids: pretending `?` is Cursor.

Tradeoff: Chapter is blocked by **Change the Graph**. Wrong first increment for “ask from Ambit.” Cursor-in-other-repo stays a starting condition, not this Chapter.

### P. llm-connector only (no Epic Chapter pointer)

Gives: work proceeds on the feature-set Project.

Avoids: Epic prose churn.

Tradeoff: against map rules: classified Projects sit on an Epic. llm-connector already enables **Ask from what I see**.

### Q. Developer Epic for LLM vendors / connectors

Gives: a home if many vendor Projects appear.

Avoids: User Epic Chapters per vendor.

Tradeoff: one LLM in Ambit is already a User Epic. Transport-layer may home extra connectors ([[plan/roadmap/reports/hub-epic-framing.md]]). Too early.

## Recommended default (after the options)

Use **A + F + J + M**.

Keep two User Epics. Do not add a User Epic or a Developer Epic for this loop. Do not add Chapters. Currency stays issue 07. In-Ambit LLM stays llm-connector on **Ask from what I see**. Mapping is Current. Mobile is Required. Cursor-in-other-repo stays outside Ambit until **Act through CLI or MCP** if you want that later.

Reject this default via issue 13.

## Assumptions

- “Auto-sync client to server” means automatic **Upload** of mapped Workspace files, not Graph **Sync**.
- Mapping a folder to a Workspace Node in the App is enough; no extra “import Cursor repo” feature is assumed.
- Mobile means Browser on a phone to the same Server, not a separate App (issue 06 Q1).
- “Grok (et al)” means an LLM from Ambit, not a committed vendor. llm-connector has not specified which LLM ([[plan/llm-connector/map.md]] Not yet specified).
- Wiki issues for documents-from-anywhere already cover mapping, Load, Upload, Download. Agent-chat wiki portions are still unfiled on that Epic’s Required list.

## Open questions

- After HITL: which axis-1..4 combination.
- Whether wiki issues should mention Cursor-outside then Ambit-inside (scope of those wiki tickets, not a product exclusion).
- Whether auto-upload should sibling [[plan/auto-download-persisted-files/project.md]] (issue 07 recommended) or a new slug.

## WORK.md mutations (for parent)

Re-read [[WORK.md]] immediately before this list. Do not duplicate issue 07, llm-connector, or the wiki issues.

- **add** Pending: [[plan/roadmap/issues/13-grill-cursor-repo-to-ambit-llm-use.md]] — HITL pick Epic/Chapter shape for Cursor-repo → Workspace → automatic Upload → mobile Ambit → in-Ambit LLM (options: [[plan/roadmap/reports/cursor-repo-to-ambit-mobile-grok.md]])
- **move**: none
- **block**: none
- **remove**: none
