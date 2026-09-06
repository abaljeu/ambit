# Read 25 (Bind Changes at the Core seam)

Status: `needs-triage`. Blocked by: none. Estimate: 2h.

Leftover after [[23-close-core-object-seam.md|23 (Close Core object seam)]] (`done`): `/ambit/changes` still unpacks Core into `changes()`, credentials, and `browserCredential`, and `postChange` still runs `CoreAuth.post`. Bind Changes inside Core. The Adapter posts through one nested handle (Browser credential), same pattern as `parseBound`. Do not add a Core-level `postChange` facade. Tests: live Browser admitted; inactive sender auth-refused.

Out of scope: [[07-define-core-files-contract.md|07 (Define the Core Files contract)]], [[08-define-core-query-contract.md|08 (Define the Core Query contract)]], Graph-only chunking, [[17-cancel-a-job.md|17 (Cancel a job)]], [[21-client-shows-lock-present.md|21 (Client shows lock-present)]], Browser dual Change POST.

Looks implementation-ready after 23, but Status is not takeable until triage sets `ready-for-agent`.
