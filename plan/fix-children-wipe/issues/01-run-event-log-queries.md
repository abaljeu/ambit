# 01 — Run the event-log queries

**Status**: ready-for-human
Blockers: none — can start immediately.

## Scope

Alan runs the queries in [[evidence/event-log-queries.sql]] (Q0–Q18, every statement a `SELECT` inside a `READ ONLY` transaction) against the Azure Postgres `gambol-pg` / `gambol` database, starting with Q3, then Q14 through Q18, then the rest as needed to follow up. Each result set is pasted into a new file under `evidence/results/`. Once the results are in, the candidate cause ranking on [[../project.md]] and in [[evidence/investigation-report.md]] section 9 is confirmed or revised.

## Done

[ ] Q3, Q14, Q15, Q16, Q17, and Q18 have each run, and their result sets are pasted into `evidence/results/`.
[ ] Q0 through Q13 have run as far as those results call for.
[ ] `project.md` "Candidate causes, ranked" table is confirmed or revised against the results, and the losing candidates are marked as ruled out.
[ ] `project.md` "Affected nodes" summary is confirmed or revised against the results.

## Stop

[ ] Stop if a query errors against the live schema; name the query and the error, and do not retry with a changed `WHERE` clause without Alan's say.
[ ] Stop before running anything outside a `SELECT` inside `BEGIN TRANSACTION READ ONLY`.
[ ] Stop if a result set holds a production credential or secret; redact it before it is pasted anywhere.
