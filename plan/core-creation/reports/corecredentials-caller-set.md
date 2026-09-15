# CoreCredentials Caller set

Date: 2026-09-14

Correction after [[../issues/34b-outside-core-lifecycle-proof.md|34b — Outside Core lifecycle proof]]: remake [[src/Server/Core/CoreCredentials.fs]] as a Caller collection. No commit.

## 1. CoreCredentials API

`CoreCredentials` is a private-field record over `Set<Caller>`. Not a mailbox.

- `empty`
- `ofCallers`
- `add`
- `remove`
- `contains`

One set of Caller. No Credential twin. `CoreAuth` / `CoreAdmissionError` stay in the same file.

## 2. Who holds the instance

Only the mailbox. `MailboxContext.credentials` is a `CoreCredentials ref`. `CoreMailbox.host` / `createFile` / `createDb` take that value at start and hand it to `CoreMailboxBackend.start`. File and Db stay persist-only.

[[src/Server/Core/CoreRuntime.fs]] builds a boot `CoreCredentials` at the `host` call and does not keep a field. No public `credentials` on `CoreRuntime`.

## 3. Caller mapping

[[src/Server/Core/CoreChanges.fs]] `Caller` now has `authority`, `name`, and `secret`.

- `authority` is still the source kind (`Browser`, `Actor`, `Test`, …).
- `name` is this browser/session instance. It is not the global user name. The derived cookie token is the same for that user, so the instance name is what distinguishes Callers in the set.
- `CoreMailbox.login` is still one door: `(name, secret)` maps to `{ authority = Browser; name; secret }` on the mailbox thread, then `Login` adds it.
- `isAdmitted` / `AdmitCaller` take the whole `Caller`.
- Non-Actor admit is `CoreCredentials.contains`. Actor admit stays `pool.isLive`. `remove` is on the collection; there is no public remove door. Actor revoke stays pool drop.

HTTP login still passes `""` for the instance name (login form has no instance field). Boot and `browserChanges` use the same empty name so the development cookie still matches. User name is not stored or used as a key.

## 4. Tests

Focused Server tests green: `CoreCredentialsTests`, `CoreRuntimeTests`, `CoreMsgActorCasesTests`, `CoreMailboxDoorTests`, `CredentialedChangePostsTests`, `CoreActorPoolTests`, `TestActorHelloTests`, `BrowserCredentialTests` — 58 passed.

New fact: `CoreMailbox.login name distinguishes Callers that share a secret`.
