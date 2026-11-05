# Ticket rules

Rules every ticket work file follows.

- Tickets are numbered in order of creation, never to show sequence. No ticket is ever numbered 0. The next ticket is always N+1, where N is the highest existing ticket number in that Project. Run order comes from each ticket's dependency or ordering note, not from its number.

- A ticket's stage is set by the worker when it starts that stage.
- Any extra report, beyond the reports a work file names, goes in the `reports/` folder inside the ticket's folder, `issues/<nn>-<name>/reports/`. Reports that a work file names keep the path the work file gives them.
