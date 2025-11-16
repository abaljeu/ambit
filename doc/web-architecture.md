## Web layer and pure view plan

This plan describes how to:

- Keep all DOM‑aware code inside `src/web`.
- Prevent non‑web code from touching the DOM directly.
- Introduce `PureRow` / `PureCell` as the canonical, immutable representation
  of editor content.

Related docs: [[architecture]], [[cellblock]].


### 1. Layers and responsibilities

- **Core (model / site / scene / controller)**
  - Files: `model.ts`, `doc.ts`, `site.ts`, `scene.ts`, `controller.ts`,
    `cellblock.ts`, etc.
  - No direct DOM access (`document`, `window`, `HTMLElement`, etc.).
  - No imports from `src/web`.
  - Work in terms of domain types and pure view types (see §2).

- **Web layer (`src/web`)**
  - The only place that imports DOM types and uses browser APIs.
  - Responsible for:
    - Rendering `PureRow` / `PureCell` into the DOM.
    - Reading caret/selection geometry from the DOM and returning pure offsets.
  - Exposes a small API: “given pure view data, update the DOM” and
    “given a DOM position, return pure offsets.”

- **Adapters (thin glue)**
  - Example: `scene-editor.ts`.
  - Convert between scene types (`SceneRow`, `CellBlock`, etc.) and
    pure view types.
  - May depend on both core and web, but contain no DOM calls themselves.


### 2. Pure view types

Define pure, DOM‑free view types in a small module (for example `src/view.ts`):

- **`PureCell`**
  - `kind: 'indent' | 'text'`
  - `text: string`  // already decoded; no HTML entities
  - `width: number` // layout hint; `-1` for flex, `>= 0` for fixed

- **`PureRow`**
  - `id: string`              // `SceneRowId.value`
  - `indent: number`
  - `cells: readonly PureCell[]`

- **`PureCellSelectionState`**
  - `cellIndex: number`
  - `selected: boolean`
  - `active: boolean`

Properties:

- These objects are immutable: no setters, no mutation helpers.
- Any change is represented by a new `PureRow` / `PureCell` value.
- Any function from pure types to pure types is referentially transparent.


### 3. Web layer API (DOM wrappers around pure types)

Inside `src/web`, we keep the current `Row` / `Cell` classes as DOM wrappers,
but make them work strictly in terms of the pure types:

- **Rendering**
  - `Row.setContent(pureRow: PureRow): void`
  - `Row.updateCellBlockStyling(states: readonly PureCellSelectionState[]): void`

- **Readback (for tests and diagnostics)**
  - `Row.toPureRow(): PureRow`
  - `Cell.toPureCell(): PureCell`

- **Row / cell location**
  - Functions like `rows()`, `findRow(id)`, `at(index)`, `currentRow()`,
    `caretX()` return DOM‑backed wrappers but never expose raw DOM outside
    `src/web`.

Key rule: **`src/web` depends only on DOM + pure view types**, not on
`scene.ts`, `model.ts`, or controller.


### 4. Adapters: scene ↔ pure ↔ web

The adapters do all conversion between core types and pure types, and then call
the web layer:

- **Scene → Pure (render path)**
  - For a `SceneRow`, construct the corresponding `PureRow`:
    - `id` from `SceneRow.id.value`.
    - `indent` from `SceneRow.indent`.
    - `cells` from `SceneRow.cells`.
  - For cell‑block selection, construct `PureCellSelectionState[]` from
    `CellBlock` information.

- **Pure → Web**
  - Call `Row.setContent(pureRow)` and `Row.updateCellBlockStyling(...)` to
    render.
  - Use `setEditorContent(pureRows)` / `replaceRows(oldSpan, pureRows)` style
    helpers that accept sequences of `PureRow`.

- **Web → Pure (feedback path)**
  - When user input requires model changes:
    - Use DOM queries in `src/web` to compute caret/selection offsets.
    - Return results as pure values:
      - Character offsets.
      - New `PureRow` / `PureCell` content to be applied by model/scene.

The core never sees DOM; it only sees pure view types and domain types.

