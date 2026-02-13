# Spec

Describing the observable aspects of the deployed program.

## Goals
- Workflowy-style outlining app

### Editing approach

- Use a visible `<textarea>` (or `<input>`) as the edit surface so its state is observable
  - Sticky to the webpage, outside the outline area
  - Focus stays in the input
  - Keydown/input events translate into operations

### Rendering approach

- Render visible outline “lines” from state
- Use event delegation from the outline container for clicks
- Keep a stable occurrence id per rendered line to preserve selection
- Apply targeted DOM patches for:
  - `replace` site node -> replace view line(s)
  - remove site node -> remove view line(s) including children
  - insert site node -> build line(s) recursively and insert

### Architecture Goals
Client-server
Low volume
Server side files plus transaction history

Client side instant speed editing, with concurrent posting to server
Assume client is correct; receive updates made by another client, from server and resolve client side.
