# Ticket rules

Rules every ticket work file follows.

- Tickets are numbered in order of creation, never to show sequence. No ticket is ever numbered 0. The next ticket is always N+1, where N is the highest existing ticket number in that Project. Run order comes from each ticket's dependency or ordering note, not from its number.
- A ticket's stage is set by the worker when it starts that stage. Status and stage stay as those rules set them.
- The build checks boxes in the ticket file. The build leaves the rest of the ticket under the stage rules.
- A step file states the present situation. The build line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. The step rewrites that file on each run.
- No step writes an outcome to `plan/*/reports/` or to a separate check file or a separate stop file.
- Any extra report goes in the `reports/` folder inside the ticket's folder, `issues/<nn>-<name>/reports/`.
