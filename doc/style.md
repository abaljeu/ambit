# Custom styling

The target element is the `<div class="amb-text">` inside each row — not the outer row div.
User-defined classes are applied to that element only.

## Implementation phases

### Phase 1 — Classes in the model, rendered from hand-edited data
Useful result: hand-edit `{.h1}` in the data file and see the style applied in the browser.

- rename system classes to `amb-` prefix in View.fs and the app stylesheet (prerequisite)
- add `cssClasses: CssClasses` to `Node` (type defined in `CssClass.fs`; operations: `empty`, `ofList`, `toList`, `toggle`, `contains`)
- add `Op.SetClasses(nodeId, oldClasses, newClasses)`
- extend `Snapshot.read`/`write` with `{.class}` metadata block syntax
- extend `encodeNode`/`decodeNode` with `cssClasses` field
- update `makeRowElement` to apply `node.cssClasses` to the `.text` div
- ship a default `user.css` with a handful of predefined classes; add `<link>` to gambol.html
- add `GET /amble/user.css` endpoint (serves `dataDir/user.css`, falls back to default)
- simple temporary UI: Alt-C pops up a dialog; if the entered name is a legal CSS identifier and does not start with `amb-`, toggle its presence in the node's `cssClasses`

 
### Phase 2 — CSS editable via the server; live reload
Useful result: edit class definitions through `POST /amble/css` and see changes immediately without a page reload. (Can be exercised via curl or a temporary button before the UI exists.)

- add `POST /amble/css` endpoint (writes raw CSS text to `dataDir/user.css`)
- client: on CSS save, bump the `<link id="user-css">` href with a cache-busting parameter to live-refresh the stylesheet

### Phase 3 — Side panel UI
Useful result: full keyboard-driven class assignment and CSS editing without touching files directly.

- side panel: fixed, togglable, general-purpose; live-updates to reflect current selection
- Alt-C activates the panel (showing it first if hidden); Escape deactivates (hides or leaves visible per app setting)
- editable class list: navigate with arrows, Delete to remove, `<Add a class>` with type-completion
- CSS property grid: Enter on a class opens it; leaving a row commits and refreshes the stylesheet
- Tab switches between the class list and the property grid

## Document storage

User class assignments live in the `Node` record as `cssClasses: CssClasses`.
They travel through the existing op/change pipeline:
- new `Op.SetClasses(nodeId, oldClasses, newClasses)` case (same optimistic-concurrency pattern as `SetText`)
- posted via the existing `POST /amble/changes` endpoint
- applied to the graph and persisted to disk by the FileAgent — no new server endpoint needed
- JSON serialization (`encodeNode`/`decodeNode`): gains a `cssClasses` field; old API messages decode with `cssClasses = CssClass.empty`
- Snapshot text format (disk): after stripping indentation tabs, if line content starts with `{`, the `{...}` block is metadata; everything after `}` is node text
  - metadata is optional; lines without it are written and parsed as plain text (backward compatible)
  - `.classname` sigil for CSS classes, space-separated: `{.h1 .blue}Third item`
  - other sigils reserved for future use (`$name`, `#tag`, `=formula`) but not implemented
  - braces are not allowed unquoted inside the metadata block (CSS class names are safe — no braces possible)
  - if a node's text itself starts with `{`, the line is written with an empty metadata prefix: `{}{text...}` to disambiguate

`makeRowElement` in View.fs reads `node.cssClasses` and adds each name to the `<div class="amb-text">` classList.

## CSS store

One CSS store per document (single document for now).
Class definitions and row class assignments are as permanent as the document — stored and persisted with it.

CSS mechanism: a `user.css` file on the server, referenced by the HTML as a normal stylesheet.
When a CSS property row is committed, the app saves the updated `user.css` to the server, then refreshes
the stylesheet live by updating the `<link>` tag's `href` with a cache-busting parameter — no page reload.
The CSS file is the authoritative store for class definitions — not reconstructed from the data model.

The editor only manages simple single-class rules of the form `.classname { ... }` where the selector is exactly one plain class name.
All other rules present in `user.css` — including rules targeting system classes (e.g. `.amb-row`), compound or descendant selectors (e.g. `x > y { ... }`), at-rules, or anything else outside that pattern — are left entirely untouched: they remain in the file, take effect in the browser, but are invisible to the editor.
When the editor writes an updated `user.css`, it rewrites only the rules it owns and preserves everything else verbatim.

No distinction between built-in and user-defined classes in code (not counting system classes which are not available for customizing).
The app ships a default `user.css` containing the predefined classes.
If no user CSS exists in `dataDir` yet, the server falls back to the default.
Once the user makes any edit, their version is saved to `dataDir` and served from there.

The `amb-` prefix is reserved for all app system classes (e.g. `amb-row`, `amb-selected`, `amb-focused`).
User class names must not start with `amb-`; no other restrictions.

## Applying classes to rows

Selected row(s) can have classes added/removed by the user.

- display current classes on the selection
- add a class from the defined list
- remove a class from the current set

## Multi-class combinations

Deferred, but a concrete use case exists: a "theme" class (e.g. `gothic`) set on a root node,
combined with content classes (e.g. `h1`, `codeblock`) on descendants, allowing flavoring of a
whole subtree via one assignment.

Note: since the DOM is flat (all `.text` divs are siblings), CSS descendant selectors won't work
naturally — the view updater would need to propagate or synthesise compound classes.


## Per-node inline styles (deferred)

Motivation: one-off values (e.g. a specific hex colour) that don't warrant naming a class.
Concern: inline styles override class styles in non-obvious ways and add complexity to the data model.
Decision: deferred — see if the class system covers enough ground in practice first.

## UI

Displayed in a fixed side panel — does not disturb document layout.
The side panel is a general-purpose area (class editor is one of potentially several uses).
Width: just enough for property/value style definitions.
Target: tablet with keyboard + PC.

Panel states:
- **Hidden:** Alt-C displays the panel and activates it (document navigation suspended, panel has keyboard focus).
- **Visible, inactive:** document navigation works normally; panel updates live to reflect the current row selection.
- **Active:** document navigation is suspended; panel has keyboard focus. Activated by Alt-C.

Escape deactivates the panel. If the app setting is to keep the panel visible, Escape leaves it visible but inactive. If the setting is to hide the panel, Escape deactivates and hides it. A mouse click on the document also deactivates the panel.

Navigation within the UI is keyboard-driven.

With multiple rows selected, the panel shows the intersection of classes across the top-level selected rows (classes held by all of them). Descendants of selected rows are not included in the calculation.

The panel contains two editors:

**Classes** — an editable list, dynamic height:
- applied classes shown as a navigable list
- `<Add a class>` item opens a type-completion input: all class names from `user.css` appear as a dropdown, filtered as you type; selecting one applies it; typing a name not in the list creates a new class
- class name list is extracted from the loaded stylesheet via the CSSOM (`document.styleSheets`) — no separate fetch or text parsing needed
- arrow keys navigate the list; Delete/Backspace removes the focused class
- Tab moves focus to the styles grid

**Styles** — an editable grid, two columns (`name` and `value`), dynamic height:
- Enter on a class in the classes list opens its properties here
- navigate between rows with arrow keys, Enter, Tab, or mouse
- leaving a row (by any of the above) commits it — saves `user.css` and live-refreshes the stylesheet
- `<Add a property>` row at the bottom; Delete/Backspace on a row removes it
- Tab moves focus back to the class list