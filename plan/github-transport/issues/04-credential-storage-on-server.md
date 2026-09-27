# 04 — Credential storage on Server

**Type:** grilling
**Status:** done
Blocked by: None
Actual: 5m

## 1. Question

- [x] Where does the Server keep git credentials so the Actor can talk to GitHub?

The Destination says the operator sets credentials so git works. It does not say the store: git credential helper on the Linux setup, Server `appsettings` / User Secrets, an environment value, or another path.

Grill the store and who loads it. Do not put secrets in the Graph or in DataDir files that Upload/Download would copy. Do not implement.

## 2. Answer

Locked 2026-09-26 (Alan, chat).

Normal git way: Ambit does **not** store GitHub credentials in appsettings, user-secrets, Graph, or DataDir.

The Actor invokes `git`; **git** loads credentials (credential helper / host setup). On Server that is the host’s git, not a separate Ambit secret plumbing.

Map gist: [[../map.md]] Decisions so far item 10.

## Comments

- 2026-09-26: Alan locked in chat. Status `done`. No Ambit credential store. Host git + credential helper.

## Time

- 2026-09-26 5m — recorded lock from chat
