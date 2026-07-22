---
name: write-simple-parser
description: >-
  Writes simple document parsers and format codecs with a small conventional
  grammar (EBNF), regex for local lexing when useful, separate structure/layout
  passes, and no persistence mixed into the grammar. Use when adding or changing
  parsers, codecs, or document formats under src/Shared/documents, or when
  designing brace/token/indent parsing.
---

# Write Simple Parser

Follow [[.cursor/rules/core-agent-behavior.mdc]] and [[.cursor/rules/fsharp-source.mdc]].
Pair with [[.cursor/skills/implement-fsharp-feature/SKILL.md]] for TDD layout and
[[.cursor/skills/add-shared-test/SKILL.md]] for fixtures.

## Before coding

1. Write a **small explicit grammar** (EBNF with few terminals) in a well-known style
   (EBNF / recursive descent / PEG-sized) that matches the problem size.
2. Confirm the grammar covers the fixtures with generic rules — not language keywords.
3. Ask: would a senior engineer say this is overcomplicated? If yes, simplify to the EBNF.

## Grammar and lexing

- Prefer conventional grammars over bespoke machines.
- Use regex for **local lexical** concerns (tokenize a token, split lines, detect a
  brace-only line) when a pattern is clearer than hand-rolled char loops.
- Nested structure (braces, trees) stays in the grammar/passes — not a mega-regex.

## Separate passes

Do not mix concerns in one machine:

| Pass | Responsibility |
|------|----------------|
| Structure | Brace/token tree (or equivalent skeleton) |
| Layout | Line/indent/whitespace after structure exists |
| Persistence / warm Keep | Emit previous raw when match — **orthogonal** to parse shape |

Do not fold artifact round-trip or raw-byte preservation into the grammar machine.

## Avoid

- Dual line-modes
- Pending stacks that encode layout state the grammar should not need
- Keyword special cases when a generic rule covers the fixture
- Parsing nested structure with a single regex

## Tests vs parser

- Tests may use realistic language snippets as **fixtures**.
- Parsers stay **generic** (structure + layout rules, not a language front-end).

## Checklist

- [ ] EBNF written and agreed before non-trivial code
- [ ] Structure pass independent of layout pass
- [ ] Keep / raw emit outside the grammar machine
- [ ] Regex only for local lexing; nesting in grammar/passes
- [ ] No dual modes or keyword forks unless the EBNF requires them
- [ ] Fixture-driven tests; parser remains generic
