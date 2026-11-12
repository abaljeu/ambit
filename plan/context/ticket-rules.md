# Ticket rules

Rules every ticket work file follows.

- Tickets are numbered in order of creation, never to show sequence. No ticket is ever numbered 0. The next ticket is always N+1, where N is the highest existing ticket number in that Project. Run order comes from each ticket's dependency or ordering note, not from its number.

- A ticket's stage is set by the worker when it starts that stage.
- The build records its outcome in the ticket file `<nn>-<name>-ticket.md`. A stop and a check verdict are recorded in that ticket file.
- The review writes the review file `<nn>-review.md`.
- No step writes outcomes to `plan/*/reports/` or to separate check or stop files.
- Any extra report goes in the `reports/` folder inside the ticket's folder, `issues/<nn>-<name>/reports/`.
