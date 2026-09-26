# Workspace C-style Brace Format

Status: Implemented (first slice)
Authority: C-style brace languages (first extension: `.cs`). Not a general “any code” parser.
See also: [[doc/reference/formats/code-shape.md]], [[doc/roadmap/workspace-format-plain.md]], [[doc/roadmap/workspace-format-code.md]]

Comment-ref profiles (`//-> `) stay in [[doc/roadmap/workspace-format-code.md]] and are out of scope here.

## Two-pass shape

**Pass 1** (character grammar only) in [[src/Shared/documents/CStyleBrace.fs]]:

```ebnf
Document ::= Statement*
Statement ::= OtherText [ OpenBrace Block CloseBrace ]
Block ::= Statement*
```

OtherText may span newlines and may be empty. No indent, cssClass, or keyword cases.

**Pass 2:** split each OtherText on newlines; attach `{`/`}` to the preceding text line (`code-brace`); then Plain indent nest. Allman and K&R fall out. Brace-only lines are not nodes.

**Persistence** (outside the grammar): warm Keep emits previous raw slices when the outline matches; cold/Insert synthesizes Allman braces for `code-brace` nodes.

## Dispatch

`.cs` → `DocumentCodec.CStyle` via [[src/Shared/documents/DocumentFormat.fs]].

## Verification

- Same-line close+open fixture (if/else **text** only) builds the expected tree and warm round-trips bytes.
- Allman switch: no brace-only nodes; `code-brace` on braced statements.
- Classify `foo.cs` → CStyle.
