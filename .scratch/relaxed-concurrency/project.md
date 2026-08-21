# Relaxed concurrency

Stage: spec
Summary: Slice 1 (drop global revision gate) spec-ready; G resolved — client merge-sync with reject+remote payload and client replan at pending tail (slices 2–3 deferred); 
Updated: 2026-08-19

Related later charting (not a replacement of this spec): [[.scratch/event-sourced-ops/]] is a more general relaxed-concurrency picture — [[.scratch/event-sourced-ops/details/relation-to-relaxed-concurrency.md]]. Slice 1 and G (no server weak-form Replace) stand. Slice 2 Reject+replan is **obsolete** for recoverable kick-back (same file). Do not treat that project as cancelling slice 1.
