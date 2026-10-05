# 04 — Duplicate hit identity

**Type:** grilling
**Status:** done
**Blocked by:** [01 — Incremental search fill and resume](01-incremental-search-fill-and-resume.md)

## 1. Question

When the server phase completes the picture, what makes a server hit the same hit the client already showed, so the second phase removes that hit?

The destination locks that the second phase removes duplicates. The identity of a duplicate is the open question. Use the findings on [01 — Incremental search fill and resume](01-incremental-search-fill-and-resume.md) for how a residence hit is identified today. Record the identity the Search spec will lock.

## Answer

Alan, 2026-10-05. The identity of a result is the NodeId. A client hit and a server hit are the same hit when they share a NodeId.
