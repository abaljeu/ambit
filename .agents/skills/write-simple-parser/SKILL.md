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

Follow [[.agents/rules/core-agent-behavior.md]] and [[.agents/rules/fsharp-source.md]]. Pair with [[.agents/skills/implement-fsharp-feature/SKILL.md]] for TDD layout and [[.agents/skills/add-shared-test/SKILL.md]] for fixtures.

## Before coding

1. Write a **small explicit grammar** (EBNF with few terminals) in a well-known style (EBNF / recursive descent / PEG-sized) that matches the problem size. Done: the EBNF is written and agreed before non-trivial code.
2. Confirm the grammar covers the fixtures with generic rules — not language keywords. Done: each fixture maps to a generic rule.
3. Ask: would a senior engineer say this is overcomplicated? If yes, simplify to the EBNF. Done: the grammar is the simplest form that still covers the fixtures.

## Grammar and lexing

Prefer conventional grammars over bespoke machines. Use regex for **local lexical** concerns (tokenize a token, split lines, detect a brace-only line) when a pattern is clearer than hand-rolled char loops. Nested structure (braces, trees) stays in the grammar/passes — one nesting home, not a mega-regex.

## Separate passes

Keep one concern per pass:

| Pass | Responsibility |
|------|----------------|
| Structure | Brace/token tree (or equivalent skeleton) |
| Layout | Line/indent/whitespace after structure exists |
| Persistence / warm Keep | Emit previous raw when match — **orthogonal** to parse shape |

Keep artifact round-trip and raw-byte preservation outside the grammar machine.

## Prefer

- One line-mode unless the EBNF needs more
- Layout state in the layout pass, not pending stacks in the grammar
- Generic rules that cover the fixture instead of keyword forks
- Nesting handled in grammar/passes

## Tests vs parser

Tests may use realistic language snippets as **fixtures**. Parsers stay **generic** (structure + layout rules, not a language front-end).
