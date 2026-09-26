# 01 — Which Workspace labels and remotes

**Type:** grilling
**Status:** needs-info
Blocked by: None

## 1. Question

Which Workspace labels (DataDir work trees) connect to which GitHub remotes, and where does that config live?

The Destination says GitHub is the outside channel for key repos, and the Workspace label’s DataDir work tree **is** the git home. It does not say which labels, how many remotes per label, or whether the operator’s `git remote` on that work tree is the only config.

Grill: one label vs many, one remote vs many, config in git itself vs Server settings vs a Graph Node. Do not implement.
