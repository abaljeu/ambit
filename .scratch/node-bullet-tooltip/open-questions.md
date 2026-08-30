# Node-marker tip — open questions (round 2 frontier)

User has not answered Round 1. Nothing locked. Round 2 **replaces** the Q1–Q15 dump for the parent interview: fewer questions, forced forks, Round-1 contradictions surfaced.

Full Round-1 set kept for traceability: historical appendix below. Parent should ask **R2-Q1…R2-Q6** only.

Evidence notes: [[grill-notes-round-2.md]]. Language candidates: [[provisional-language.md]].

---

❓ **R2-Q1** - **One job (forced fork)**: Pick exactly one primary job for hover on the Node marker:

**A. Glance identity** — see Guid + Update Time; no expectation of copy-paste.

**B. Copy identity** — Guid must be reliably copyable into logs (click-to-copy and/or selectable overlay).

**C. Sync diagnostic** — this control is where current/old/edited (or raw source stamp) finally becomes visible; identity is secondary.

**D. Always-on user chrome** — normal outlining aid (explain hollow/chevron, residency), not a debug strip.

Round 1 recommended A-ish while also recommending native `title`, which cannot satisfy B. C collides with `.amb-file-indicator` and/or the orphaned `desktopFileIndicatorText`. D pulls Unloaded-vs-Unparsed into scope (hollow is ambiguous today).

➡️ **A** for v1: glance Guid + Update Time. If you actually need B, say B and accept custom UI. Do not pick C on this control — finish or kill the orphaned file-status text on its own home first.

---

❓ **R2-Q2** - **Which clocks, under which labels?**: Confirm the display contract:

1. Always: **Update Time** = `Node.updateTime` only (never rename it TimeStamp in UI).
2. Optional second line: **Source modified** = `sourceModifiedUtc` when present on the *already-fetched* status payload.
3. Derived **current / old / edited**: (i) never on this tip, (ii) instead of raw Source modified, or (iii) both.

Remember: after persist, Update Time often *is* Server DataDir mtime; server file-status `sourceModifiedUtc` is also DataDir. Dual lines can be redundant or a drift detector — pick intentional.

➡️ (1)+(2) with raw Source modified only; **(i) never** put current/old/edited on this tip in v1. Missing Update Time → explicit `—`, not epoch.

---

❓ **R2-Q3** - **Ban list — accept or cut**: Hard-ban for v1 tip content (yes = banned):

| Candidate | Ban? |
|-----------|------|
| Outline text / Filename | yes |
| Kind / Owned-Ref / Loaded-Unloaded / Parsed-Unparsed as text | yes (hollow stays ambiguous unless R2-Q1 = D) |
| SiteId / occurrence / owner Guid / CSS classes | yes |
| Absolute local paths / resolved `%LocalAppData%` paths | yes |
| WorkspacePathSync `shortLabel` (duplicate of `.amb-file-indicator` title) | yes |
| Capability / Session / Revision noise | yes |
| DesktopFileStatus word (file/folder/create/…) | **?** — only if you want the tip to adopt the orphaned indicator |
| Request path string | yes |

➡️ Accept the whole "yes" column. For DesktopFileStatus word: **ban** on this tip unless R2-Q1 = C.

---

❓ **R2-Q4** - **Availability edges (one policy)**: Status lives on `VM.desktopFileIndicator` for the **active** file ref only; may be filled by **App** `/_desktop/file-status` *or* **Server** `/{file}/file-status`. Choose:

**Omit-unless-match**: show Source-modified / status lines only when indicator `nodeId` equals hovered Node; else omit. Checking / Blank / non-file → omit. No refetch-on-hover.

**Active-row only tip**: tip itself only attaches when the row is active (weaker identity for other rows).

**Fetch-on-hover**: new policy (out of "cheap hover" land).

➡️ **Omit-unless-match**. Never show another Node's stamp. Browser-only Sessions still get Guid + Update Time; they simply never grow Source-modified lines.

---

❓ **R2-Q5** - **Vehicle (depends on R2-Q1)**:

| If job is… | Vehicle |
|------------|---------|
| A (glance) | Native `title` with `\n` lines; Shared formats the string |
| B (copy) | Custom overlay and/or click-to-copy; native `title` insufficient |
| C / D | Decide chrome separately; do not default to `title` without a layout sketch |

➡️ Match the table. Do not ship B with only `title`.

---

❓ **R2-Q6** - **Samness + naming (one bite)**:

1. Same tip payload on FoldChevron / SolidCircle / HollowCircle? (Passive tip; clicks unchanged; no Load/fold instructions in tip.)
2. Spoken name: **Node marker** or **children indicator** — never "bullet" in docs/glossary.
3. Code: rename binding `leafBullet` → `nodeBullet` without glossary promotion of "bullet"?
4. CSS `amb-leaf-*` and class lie `.amb-node-guid` (Filename): same slice, later ticket, or ignore?

➡️ (1) same payload all three. (2) Node marker. (3) yes, binding only. (4) later tickets; tip spec must warn implementers not to "fix" by stuffing Guid into the name span.

---

## Historical Round-1 frontier (superseded for interview order)

See prior revision / git history of this file for Q1–Q15 wording. Mapping:

| Round 1 | Absorbed into |
|---------|----------------|
| Q1 | R2-Q1 |
| Q2, Q15 | R2-Q2 |
| Q3 | R2-Q1 (B implies full Guid) + R2-Q5 |
| Q4–Q5 | R2-Q3 |
| Q6–Q8 | R2-Q2 + R2-Q3 + R2-Q4 |
| Q9–Q10 | R2-Q5 |
| Q11–Q13 | R2-Q6 |
| Q14 | R2-Q4 (Guid+Update Time always; desktop lines gated) |
