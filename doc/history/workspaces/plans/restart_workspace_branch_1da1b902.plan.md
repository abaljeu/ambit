---
name: Restart workspace branch
overview: Create a clean restart branch at `35f2976`, preserving the planning commit, all confirmed good work through `b1a55c0`, and the independent desktop POST fix while leaving the current `db` history intact for reference.
todos:
  - id: create-restart-branch
    content: Create `workspace-restart` at `35f2976` while preserving `db`
    status: pending
  - id: verify-boundary
    content: Verify retained planning/good history and excluded workspace commits
    status: pending
  - id: baseline-tests
    content: Run tests and record the clean restart baseline
    status: pending
isProject: false
---

# Restart workspace work cleanly

1. Confirm the working tree is clean and retain `db` unchanged as the archive containing the abandoned implementation.
2. Create `workspace-restart` directly at `35f2976` (no reset or cherry-pick). This includes `648f267`, all confirmed-good commits through `b1a55c0`, and the isolated [`src/Desktop/LocalProxy.fs`](src/Desktop/LocalProxy.fs) POST fix.
3. Verify the new branch ends at `35f2976`, contains both planning documents from `648f267`, and excludes `5a24a88..22e28ca`.
4. Run the existing test suite to establish a clean baseline before restarting workspace implementation.
5. Treat `db` only as reference: selectively reintroduce ideas later in small, independently tested commits rather than replaying any of the mixed workspace commits.