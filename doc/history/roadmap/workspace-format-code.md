# Workspace Code Text Format

Status: Draft
Authority: Target design for code-like workspace files that should follow plain-text rules with language comment references.
See also: [[doc/roadmap/workspace-format-plain.md]], [[doc/roadmap/workspace-text-outline-conversion.md]], [[doc/roadmap/reference-expressions.md]]

This format inherits [[doc/roadmap/workspace-format-plain.md]] unless explicitly overridden here. That includes export behavior, line/indent handling, identity (` #name-token`), metadata (`cssClasses`), and diagnostics.

## Reference override

For languages with a single-line comment token, a profile may define a **comment ref prefix** that is both valid language comment text and an outliner ref marker (example: `//-> `).

When a prefix is defined, ref-only lines use:

```
<indent> <comment-ref-prefix> <ref-target>
```

`<ref-target>` matches plain format rules:

```
"#" <name-token>
| <workspace-relative-path> "#" <name-token>
```

Import resolves prefixed ref-only lines to Ref edges exactly as plain refs. Export uses the language prefix when available; otherwise it falls back to plain `-> ` form. Inline occurrences remain plain text.

## Verification Targets

- Everything in [[doc/roadmap/workspace-format-plain.md]] still holds unless overridden here.
- A profile with `//-> ` round-trips ref-only lines as comments while preserving Ref edges.
- Files without a profile continue to use and round-trip plain `-> ` ref lines.
