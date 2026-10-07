# Chart Automatic upload and download onto Projects

**Type:** task
**Status:** done
Blocked by: 06

## Question

Chart pointers for Chapter **Automatic upload and download** on [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]. [[plan/auto-download-persisted-files/project.md]] already owns auto-download (HITL tabled). Auto-upload has no Project. Point the Chapter at owning Projects or issues; do not own that work on the Epic file. Do not implement. Do not create an Epic Project folder.

Recommended: extend or sibling the auto-download Project. HITL resume of auto-download is not this ticket.

## Answer

2026-10-07 (Alan): Auto-upload as a sibling Project is withdrawn. The seamlessness gap for Upload is not a missing sibling Project. Keep-files-current outbound is UPDATE on [transport-layer](plan/transport-layer/project.md): graph edit → Unpersisted → Core Persist stack → push. Persist is Core async, not an Actor. Upload and Download are the byte move. Outbound content is Persist (Graph → text) then push. Chapter [Automatic upload and download](plan/roadmap/epics/chapters/automatic-upload-and-download.md) points at that chart and at [auto-download-persisted-files](plan/auto-download-persisted-files/project.md) (HITL tabled). This ticket does not ask for a new auto-upload Project. File-channel implementation Projects are the ones the transport-layer chart names.

## Comments

- 2026-10-07 — Superseded. Sibling auto-upload Project withdrawn. Remaining work is the transport-layer UPDATE chart (Persist → push) and the file-channel implementation Projects that chart names. Chapter correction: [Automatic upload and download](plan/roadmap/epics/chapters/automatic-upload-and-download.md).
- 2026-09-19 — Actor-shaped Workspace sync redesign chart is owned by [[plan/transport-layer/project.md]]; this ticket still charts the auto-upload implementation Project pointer for the Chapter.
- 2026-09-02: Parked from WORK.md. Chart auto-upload (and remaining pointers) for [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]] current Chapter.
