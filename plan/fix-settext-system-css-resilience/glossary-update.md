# Glossary update: File Node

## What changed

### CONTEXT.md

1. **File Node** expanded under About the Software with the user-locked gloss:
   - stands for a real on-disk file, identified by **relative path**
   - cold load / stub → create File Node without reading text yet (`Unparsed`)
   - after read/parse → same File Node
   - prefer “the file” at that path; `_Avoid_` includes **file body**
2. **Directory File** cold-bootstrap sentence: “other File bodies Unparsed” → “other File Nodes Unparsed”

### Left alone

- `.agents/skills/writing-great-skills/GLOSSARY.md` — no edit (no contradictory “file body” found)
- `WORK.md` — no board mutation suggested

## Exact definition (CONTEXT.md)

**File Node**:
A Node whose Kind is File; a Graph node that stands for a real on-disk file, identified by a relative path (e.g. `SYSTEM/user.css`). Always say File Node, not bare “file,” when referring to the Node. Cold load / stub: know the path exists and create the File Node without reading the file's text yet (Unparsed). After reading/parsing that file's text, it is the same File Node. Prefer “the file” at that relative path — not “file body.”
_Avoid_: file (bare, for a Node), document, page, note, file body
