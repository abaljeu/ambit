# 02 — Actor command surface

**Type:** grilling
**Status:** needs-info
Blocked by: None

## 1. Question

What Commands does the Server Actor receive for remote, pull, and push?

The Destination locks a Server-side Peer Actor that does pull and push (round-trip v1). It does not lock the command names or whether a person in the Browser starts them.

Prior spec [[plan/workspace-git/project.md]] proposes Git Remote, Git Push, and Git Pull on WorkspaceGit. Grill that surface as a candidate. Do not inherit that spec’s non-FF accept. Do not implement.
