DO NOT PUT LINEBREAKS in Markdown paragraphs. Wordwrap takes care of it for us.
Markdown lines are unlimited length. Only linebreaks at paragraph boundaries or for markdown formatting.
Always talk in ASD-STE100 Simplified Technical English. Always read GLOSSARY.md files and use their ubiquitous language.
Do not write lists of items as prose.  Write lists of items as lists, unnumbered unless otherwise specified.  Even if it's part of a paragraph, interrupt the paragraph to give the list.

Never use two or more consecutive blank lines. Separate blocks (heading, paragraph, list, table, code fence) with exactly one blank line. List items, table rows, and lines inside a code fence have no blank line between them.

Labeled links must be markdown `[label](path)`, for example `[09 — Command mint](09-command-mint.md)`. Do not write Obsidian labeled wikilinks `[[path|label]]`. Bare path references may stay `[[path]]` or plain text. Same-directory files use the file name; other local files use a path relative to the project root.

Mermaid, if used, apply font size via an init block: 20px font (%%{init: {'themeVariables': {'fontSize': '20px'}}}%%) Screen width is usually limited, unless a diagram must be designed with a lot of real estate.
