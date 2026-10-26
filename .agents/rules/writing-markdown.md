# Writing markdown

## Repetition
DO NOT REPEAT YOURSELF.
A link must occur once only within a file
Instruction in one file should not be repeated in another file, but link to the other.

## Lines

A paragraph stays on one line. Word wrap displays that line. A line break is a paragraph boundary or markdown formatting. Line length is unlimited.

## Lists

Never write a list of things as prose.

Write lists of things as unnumbered lists unless otherwise specified.

A list that starts inside a paragraph interrupts that paragraph.

###  Counterexample

```
Done: the ticket names its type, its blockers, its inputs, its process, its outputs, its stop line, and its check. The implementer uses the ticket and the code.
```

Write that list as:

```
Done: the ticket names

- its type
- its blockers
- its inputs
- its process
- its outputs
- its stop line
- its check

The implementer uses the ticket and the code.
```

## Blocks

Separate each block with exactly one blank line. A block is a heading, a paragraph, a list, a table, or a code fence. List items, table rows, and lines inside a code fence have no blank line between them.

## Links

- A labeled link is markdown `[label](path)`, for example `[09 — Command mint](09-command-mint.md)`.
- A bare path reference stays `[[path]]` or plain text.
- A file in the same directory uses the file name.
- Any other local file uses a path relative to the project root.

### Counterexample

```
[[09-command-mint.md|09 — Command mint]]
```

That is an Obsidian labeled wikilink. The labeled form is `[09 — Command mint](09-command-mint.md)`.

## Language

- Write in ASD-STE100 Simplified Technical English.
- Read the GLOSSARY.md files and use their ubiquitous language.

## Diagrams

A Mermaid diagram sets the font size in an init block to 20px: `%%{init: {'themeVariables': {'fontSize': '20px'}}}%%`. Screen width is limited. A diagram that needs a wide canvas is the exception.

## Positivity

Skip negative statements. Instead make sure it's implied by the positive statement.

WRONG: "This folder does not take these steps. (list of steps)"

RIGHT: "This folder takes these steps. (list of steps)"

## Precision

Avoid prose when you can express things more exactly.

WORSE:

  "The text should fade in with a bouncy spring effect"

The agent interpreted that differently every time. When I switched to code blueprints:

BETTER: 
✅  const BEATS = { TITLE_IN: 0, SUBTITLE: 30, SCENE_END: 90 };
    // AnimatedText variant="title" size={52} entrance="scale"
    //   springPreset="bouncy" startFrame={BEATS.TITLE_IN}

## Technical writing

A technical spec is a list in this shape:

```
## Heading
[ ] Item
### Subheading
[ ] Item
[ ] Item
[ ] Item
### Subheading
```

### Facts

[ ] One fact per item.
[ ] A heading groups the items under it.
[ ] A checkbox is the fact.
[ ] [64 — Place Edit changes as Order.Change](plan/core-refinement/issues/64-merge-postevent-and-postgraphonly-into-change.md) is this form.
