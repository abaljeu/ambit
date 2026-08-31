# Restore active Workspace on cold init

Type: grilling
Status: resolved
Blocked by: 04

## Question

Auth now survives iOS memory-unload (still-open tab cold-reloads). Client init still does not Load the previously active Workspace. Normal Refresh does, via `sessionStorage` in [[src/Client/SessionState.fs]] (`b` bootstrap widen → `/state?zoom=`; `z` Zoom restore).

After memory-unload the tab is a new Session, so `sessionStorage` is empty. What should cold init restore?

Candidates: previously active Workspace only (`b`); Refresh-parity Workspace + Zoom (`b` + `z`); also folds (`e`). Medium likely `localStorage` (survives new Session; ITP 7-day script-storage purge).

## Comments

- Q1 **B** — Refresh-parity: previously active Workspace + Zoom (`b` + `z`). Medium: `localStorage` fallback (same snapshot). Folds (`e`) ride along on the existing blob; do not split.

## Answer

Cold init after memory-unload should match Refresh for Workspace Load and Zoom. Keep the same `gambol-session-v1` snapshot. Write `sessionStorage` and `localStorage`. Read `sessionStorage` first (tab Session / Refresh); if empty, read `localStorage` (new Session after iOS unload). `tryReadSavedZoomId` and `restoreSessionState` both use that read order. No new client credential store. ITP may still purge `localStorage` after 7 days without site interaction.

## Verification

Passed on 2026-08-15: after iOS unloaded a still-open Safari tab from memory, its cold reload Loaded the owning Workspace and restored the prior Zoom. Device model and iOS version were unspecified. See the authoritative [[../pending-audit-cold-reload.md]].
