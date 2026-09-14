# Code review — 33 auth.Disabled removed

**Pin:** working tree vs `HEAD` (`git diff HEAD`). Place `dev`. Untracked: [[plan/core-creation/reports/remove-auth-disabled-bypass.md|Remove auth.Disabled bypass]].
**Spec:** [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]].
**Related:** [[plan/architecture/browser-and-app-auth.md|Browser and App auth]], [[plan/core-creation/reports/remove-auth-disabled-bypass.md|Remove auth.Disabled bypass]], prior [[plan/core-creation/reports/code-review-33-spec-gap-tip.md|Code review — 33 Spec-gap tip]].
**Ticket Status:** left `coded`. This report is not approval.

## 1. Standards

F# size vs `HEAD`: no over-limit bindings; no added long lines. [[src/Server/RouteRegistration.fs]] stayed under the prior line count (already over 400; no split required). [[src/Server/AuthToken.fs]] is comment-only. No Core vs Adapter violation under [[.agents/rules/core-api.md]].

### 1.1 Documented standards (hard)

**1.1.1 Empty-Auth predicate duplicated**  
Cites [[.agents/rules/core-agent-behavior.md]] Simplicity First: “Don't replicate code — put shared logic in a reusable place and call it.” [[src/Server/RouteRegistration.fs]] still binds `gitAuthOpen` in `createAuthentication`, then after removing `Authentication.Disabled` repeats the same test at two Adapter sites:

```
if auth.ExpectedUser = "" && auth.ExpectedPass = "" then
    Results.Json({| disabled = true; message = "Auth disabled; git gateway is open" |})
```

```
elif auth.ExpectedUser = "" && auth.ExpectedPass = "" then
    auth.SetCookie ctx.Response
    serveAmbitHtml ctx
```

**1.1.2 Paragraph and list not separated**  
Cites [[.agents/rules/markdown-writing.md]]: “Separate blocks (heading, paragraph, list, table, code fence) with exactly one blank line.” [[plan/core-creation/issues/34-outside-core-lifecycle-proof.md|34 — Outside Core lifecycle proof]] section 1: the intro paragraph sits against checklist item 0 with no blank line.

```
Mailbox Actor cases for this proof. Contracts on arch **CoreMsg / CoreMailboxBackend**; Seams **CoreMsg union**, **Credentialed Change posts** (Actor live-table admit).
0. [ ] `StartActorRequest` type — `{ zoomId; focusId; commandId; graphIds }`; one Command / StartActor payload shared by CoreMsg `StartActor`, CoreMailbox `startActor`, and CoreActorPool.startActor
```

**1.1.3 Unrelated ticket edits in the same tree**  
Cites [[.agents/rules/core-agent-behavior.md]] Surgical Changes: “Touch only what you must.” / “Every changed line should trace directly to the user's request.” F# plus [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]] remove `auth.Disabled`. The same pin also edits [[plan/core-creation/issues/34-outside-core-lifecycle-proof.md|34 — Outside Core lifecycle proof]] (`StartActorRequest`, Actual 10m). Those lines do not trace to the cookie-skip removal.

### 1.2 Baseline smells (judgement)

**1.2.1 Possible Duplicated Code / Primitive Obsession**  
Same hunks as 1.1.1 ([[.agents/skills/code-review/SMELLS.md]]). Empty `ExpectedUser` / `ExpectedPass` stand in for the development-credential concept `Disabled` used to name. A record flag that does not skip `IsAuthenticated` would group that clump.

## 2. Spec

Pin: `git diff HEAD` against [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]]. Untracked: [[plan/core-creation/reports/remove-auth-disabled-bypass.md|Remove auth.Disabled bypass]].

### 2.1 Missing / partial

None on the locked auth lines.

- Credential identity 2 / §6.3: `Disabled` is gone; `IsAuthenticated` is cookie == `deriveToken`. Empty Auth without a cookie is 401 on Browser APIs, including capabilities.
- Identity 2 / §6.3: GET `/ambit` auto-issues `deriveToken("", "")` then serves HTML. Auth-enabled `/ambit` redirects to login with no Set-Cookie.
- Restart/seed 8: auto-issue does not `credentials.add`; login POST still does.
- `gitAuthOpen` still opens git. That is GitAuthDisabled, not the rejected skip.

Prior [[plan/core-creation/reports/code-review-33-spec-gap-tip.md|Code review — 33 Spec-gap tip]] 2.2.1 (Disabled app-serve Set-Cookie) and 2.3.1 (Adapter 401 vs CoreMailbox) are resolved by the updated spec plus this cut.

### 2.2 Scope creep

**2.2.1 StartActorRequest on 34 — Outside Core lifecycle proof**  
This pin also edits [[plan/core-creation/issues/34-outside-core-lifecycle-proof.md|34 — Outside Core lifecycle proof]] (`StartActorRequest`, Actual 10m). Spec: “What this increment avoids … Outside Core lifecycle proof”; out of scope 5: “Actor live-table admit, StartActor, TestActor hello, Outside Core lifecycle proof.”

### 2.3 Looks implemented but wrong

None. Login POST dropping `username <> ""` is empty Auth as a real credential (identity 2 / §6.3). Adapter missing-cookie 401 without CoreMailbox matches identity 4 and §3.3.

## 3. Summary

Standards 4 (worst: duplicated empty-Auth predicate after removing `Disabled`); Spec 1 (worst: `StartActorRequest` edits on [[plan/core-creation/issues/34-outside-core-lifecycle-proof.md|34 — Outside Core lifecycle proof]]).
