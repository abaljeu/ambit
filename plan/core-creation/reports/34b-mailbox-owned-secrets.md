# 34b mailbox-owned secrets

Place: commit 2 after [[34b-inject-actors-mailbox-history.md]]. One major edit: the mailbox owns Browser secrets. There is no public add-credential door.

## 1. Ownership

1. Loop.secrets is a `Set<Credential>` on the CoreMailboxBackend loop. `addSecret` and `hasBrowserSecret` are private helpers. There is no second CoreCredentials MailboxProcessor.
2. CoreActorPool.create takes no credentials. The live row is Actor liveness. Browser and Parse secrets stay on the mailbox thread.
3. CoreRuntime does not export a credentials field. Boot puts `AuthToken.deriveToken` into the host `initialSecrets` set. Login and cookie admit go through CoreMailbox.login and CoreMailbox.isAdmitted.

## 2. No public add

1. Deleted: CoreCredentials mailbox type, `create` / `add` / `contains` / `remove`, CoreMailbox.addCredential, seedCredentials, CoreRuntime.credentials, parseCredential.
2. The mailbox adds a Browser secret only on CoreMsg Login, after it decides login is valid.
3. Tests and HTTP do not call add. Tests start the host with admittedSecrets (same shape as boot). HTTP login calls Core.login. Cookie admit calls isAdmitted.

## 3. Admit

1. Browser (and other non-Actor Authority): `hasBrowserSecret`.
2. Actor: `pool.isLive`.
3. postGraphOnlyChange is not admit-gated. Parse HTTP uses Core.changes() / parseBound with browserCredential; it does not take a sender.

## 4. Tests

1. Server.Tests filter TestActorHelloTests, CoreMailboxDoorTests, CoreMsgActorCasesTests, CoreRuntimeTests, CoreActorPoolTests, CredentialedChangePostsTests, CoreCredentialsTests, AuthTokenTests, BrowserCredentialTests: 70 passed.
2. New fact: CoreMailbox.login privately admits a Browser secret (refuse, then login, then post).
