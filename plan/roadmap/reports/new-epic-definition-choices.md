# New Epic — definition choices

Recommendation for a multiple-choice pick. Not a Committed Decision. Not an Epic file. Primary clue: [[commit-gated-agent-epic-union.md]]. Map hole also notes a future connect Epic for [[plan/transport-layer/project.md]] ([[plan/roadmap/map.md]] Not yet specified). [[plan/webview2-azure-origin/project.md]] is parked on [[plan/roadmap/epics/organize-huge-outlines.md]] because nothing else homed it.

**Ambiguity:** Options 1–3 answer the commit-gated / Agent / mail union. Options 4–5 answer a different hole: a better home for App/Azure origin than Organize Huge Outlines. Those are separate candidates; picking one does not close the other.

## Problem one sentence

No standing Epic owns the review-then-commit pattern (Agent proposes outside actions in Ambit; the person commits), and App same-site cloud origin has no fitting Epic home (it sits on outline-scale by default).

## Options

### 1. Review and commit Agent mail

**Definition:** A person reviews an Agent's proposed mail actions in Ambit and commits them.

**Kind:** User Epic — marketable end-user pattern; report rejected a Developer "information hub" frame.

**Overlap / steal:** Does not merge [[plan/roadmap/epics/agent-chat-managed-context.md]], [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]], or [[plan/roadmap/epics/operate-a-pkm.md]] if Notes keep chat, device files, and Find there. Ingest may reuse files/Graph from documents-from-anywhere. Does not touch WebView2 / Organize Huge Outlines.

**Too-narrow failure:** Collapses into one mail-connector or [[plan/transport-layer/project.md]] slice.

### 2. Review and commit outside actions

**Definition:** A person reviews Agent proposals that affect outside systems and commits them from Ambit (mail first; later channels under the same seam).

**Kind:** User Epic — same person-job as (1), broader than one server.

**Overlap / steal:** Same neighbors as (1). Risk of treating [[plan/transport-layer/project.md]] examine-before-commit as this Epic instead of the Project template. Still does not home WebView2.

**Too-narrow failure:** Becomes "the mail Project" or a rename of transport-layer.

### 3. Connect tools with a review seam

**Definition:** A person connects outside tools to Ambit so material and actions cross a review seam before they enter or leave.

**Kind:** User Epic if forced — but reads as program thesis, not one marketable end-goal.

**Overlap / steal:** Would pull PKM import, documents Google/inbound, agent Actor, publish outbound, and the connect Epic named on the map. Same reject as [[hub-epic-framing.md]] and issue 13 mega-Epic unions.

**Too-narrow failure:** Either a mega-Epic or a hollow label over transport-layer alone.

### 4. App cloud same-site origin

**Definition:** The App document origin is the cloud host so Browser HTTP is same-site (Navigate, cookies; `/_desktop` stays LocalProxy).

**Kind:** Developer Epic — App/hosting, not a person usage pattern.

**Overlap / steal:** Takes [[plan/webview2-azure-origin/project.md]] off [[plan/roadmap/epics/organize-huge-outlines.md]]. Adjacent to pretty-URL / [[direct-api-vs-proxy.md]] work. Does not own commit-gated Agent mail.

**Too-narrow failure:** Collapses into that one WebView2 Project.

### 5. Host the Desktop App

**Definition:** Developers home App hosting, origin, proxy, and auth-cookie work that is not outline scale.

**Kind:** Developer Epic — broader parking lot than (4); still not a User Epic.

**Overlap / steal:** Same WebView2 steal from Organize Huge Outlines; may absorb more Desktop/LocalProxy items later. Orthogonal to options 1–3.

**Too-narrow failure:** Stays a single-Project Epic until a second hosting need appears.

## Too close — do not choose

- **Option 3 (Connect tools with a review seam)** — too close to the rejected hub / "connect my tools" frame; would steal from PKM, documents-from-anywhere, agent-chat, and publish.
- Do not invent widenings of agent-chat or documents-from-anywhere as the new Epic; [[commit-gated-agent-epic-union.md]] already rejected those.

Options 1 and 2 are near each other (mail-only vs multi-channel). Prefer one; do not create both.

## Multiple-choice labels

1. Review and commit Agent mail
2. Review and commit outside actions
3. Connect tools with a review seam
4. App cloud same-site origin
5. Host the Desktop App
