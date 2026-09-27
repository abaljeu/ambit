# 15 — Keep the App outside Peer Actor hosting

**Status:** defined
**Blocked by:** [13 — Run git Load and Save through the Server Peer Actor](13-peer-actor-runs-git-load-save.md)

## Context

A person can start Load or Save from a device that maps through Server. The App already handles the Command surface and desk WebDAV path. GitHub transport must stay on Server so devices do not need their own clone protocol, git process, or GitHub credential.

## What to build

### 1. App

Complete the open **App** constraint in the Module map in [[../arch.md]]. This capability supports the **Server Peer Actor does the round-trip**, **One Actor shape through Server**, and **Host git credentials** Story paths.

- [ ] 6.2.2 Do not host the Peer Actor — The App sends the Load or Save request through Server and does not construct or run GithubTransportActor.
- [ ] 6.2.1 Keep git off the App — Verification proves the App does not invoke GitRun for GitHub pull or push.
- [ ] 6.1.1 Keep remote config off the App — Verification proves the App adds no remote map, tracked-branch map, or GitHub credential state.
- [ ] 6.3.1 Keep the Command surface — The App continues to expose Load and Save with the selected pre-pick.
- [ ] 6.3.2 Keep desk WebDAV — A desk choice continues through the existing WebDAV path.
- [ ] 6.2.2 Prove the Server boundary — Tests or repository-boundary checks prove every git choice crosses the Server request door and no App Actor host is added.

## See also

- [github-transport architecture](../arch.md)
- [04 — Credential storage on Server](04-credential-storage-on-server.md)
