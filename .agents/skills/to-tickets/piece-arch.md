# Architecture

The architecture this ticket needs. Decisions from the binding arch are written into the subsections.

Inputs: the ticket's one-line goal; the Project `spec.md` path; the Project `arch.md` path.
Depends: none.

Output: the `**Binding arch:**` line, and the `###` subsections of `## What to build`.

1. Read the given `arch.md`. Write the architecture decisions this ticket uses, and cite modules by name. When Sequence is `tracer-cut`, name the subsections by the modules the path crosses. When Sequence is `module-build`, name the capability. When Sequence is `expand-contract`, name the expand, migrate, or contract action this ticket performs.

Done: Binding arch names every arch file used, and the subsections state the decisions taken from those files.
