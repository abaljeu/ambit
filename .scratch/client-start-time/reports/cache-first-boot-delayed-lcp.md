# Cache-first boot delayed LCP

Date: 2026-08-27
Parent: [[../issues/09-cache-first-boot-delayed-lcp.md]]

## Measurement

LCP of `div.amb-text` was **10.81 s** on `/ambit` with `Program.bundle.js` (not `?debug=1`). Before cache-first boot, the same path was ~**1 s**.

## Cause

Boot waited for IndexedDB, then decoded the snapshot on the main thread, then fetched `/state`. A large snapshot made `StateLoaded` late. Equal-Revision Poll also loaded the live Graph to fingerprint it, which blocked the agent.

## Fix

`GET /state` starts immediately again. Snapshot persist after paint stays. Poll omits `bootstrapHash`.

## HITL

Hard-reload `/ambit`. Network: one `Program.bundle.js`, then `/state` without a 2.5 s idle gap. LCP should return near ~1 s.
