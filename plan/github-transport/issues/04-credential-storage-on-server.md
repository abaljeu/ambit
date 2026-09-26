# 04 — Credential storage on Server

**Type:** grilling
**Status:** needs-info
Blocked by: None

## 1. Question

Where does the Server keep git credentials so the Actor can talk to GitHub?

The Destination says the operator sets credentials so git works. It does not say the store: git credential helper on the Linux setup, Server `appsettings` / User Secrets, an environment value, or another path.

Grill the store and who loads it. Do not put secrets in the Graph or in DataDir files that Upload/Download would copy. Do not implement.
