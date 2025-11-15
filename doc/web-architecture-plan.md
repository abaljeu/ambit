## Web layer isolation plan

This document describes a staged plan to:

- Keep all DOM-aware code inside `src/web`.
- Ensure `src/web` does not depend on non‑web modules.
- Ensure non‑web modules do not touch the DOM directly, and use `src/web` as an
  abstraction.
- Move `Cell` and `Row` toward referentially transparent, immutable behaviour.

Links to related docs: [[architecture]], [[cellblock]].


### 1. Target structure

- **Core model / scene / controller**
  - Files: `model.ts`, `doc.ts`, `site.ts`, `scene.ts`, `controller.ts`,
    `cellblock.ts`, etc.
  - No imports from the DOM, `document`, `window`, `HTMLElement`, or `src/web`.
  - All state is kept in model / site / scene objects.
  - Output for the UI is expressed as plain data (`SceneRow`, `SceneRowCells`,
    selection state, etc.).

- **Web layer**
  - Directory: `src/web`.
  - Only this layer imports DOM types (`HTMLElement`, `HTMLSpanElement`, etc.),
    and uses `document`, `window`, or browser APIs.
  - All modules outside `src/web` treat it as an implementation detail behind a
    small API surface.
  - Internals of `src/web` may be subdivided (for example):
    - `web/editor-dom.ts`: DOM primitive types and helpers
      – tag names, CSS class names, `CellElement`, `RowElement`,
      `getNodeAndOffsetFromTextOffset`, `getTextOffsetFromNode`, etc.
    - `web/cell.ts`: DOM‑backed `Cell` implementation.
    - `web/row.ts`: DOM‑backed `Row`, `RowSpan`, and row‑level operations
      (`rows`, `addBefore`, `addAfter`, `replaceRows`, `setEditorContent`,
      etc.).
    - `web/selection.ts` (optional future): helpers for caret and selection
      operations that currently live in `Cell` / `Row`.

- **Thin adapters**
  - Files in the root `src` directory (for example `scene-editor.ts`) can act
    as adapters that convert between:
    - Scene data types (`SceneRow`, `SceneRowCells`, `CellSelectionState`) and
      web layer types (`Row`, `Cell`).
  - These adapters can depend on both `scene.ts` and `src/web`, but contain no
    DOM calls themselves.


### 2. Goal: `web/` depends only on web

**Goal:** modules under `src/web` do not depend on any non‑web modules. In
particular, they should not import from `scene.ts`, `controller.ts`, `model.ts`
or similar.

**Plan:**

1. **Remove `scene` imports from `web/row.ts`.**
   - Today `web/row.ts` imports `SceneRow`, `SceneRowCells`, and
     `CellSelectionState`.
   - Replace these imports with local types that describe only what the web
     layer needs, for example:
     - `WebCellData = { type: 'indent' | 'text'; text: string; width: number }`
     - `WebRowData = { id: string; cells: readonly WebCellData[] }`
     - `WebCellSelectionState = { cellIndex: number; selected: boolean;
       active: boolean }`
   - Change `Row.setContent` to take `WebRowData` or just `readonly WebCellData[]`.
   - Change `Row.updateCellBlockStyling` to take `readonly WebCellSelectionState[]`.

2. **Move the Scene→Web mapping into `scene-editor.ts`.**
   - In `scene-editor.ts`, construct `WebRowData` from `SceneRow` on the fly
     using existing `SceneRow.cells` and `CellSelectionState` information.
   - Call `Row.setContent` with `WebRowData` instead of passing `SceneRowCells`
     through `web`.

3. **Audit other `src/web` files for non‑web imports.**
   - If a `web` file imports from non‑web modules, move the dependency into an
     adapter next to the non‑web code, and pass plain data into the `web`
     module instead.

After these steps, `src/web` will only depend on:

- Other modules under `src/web`.
- Global browser APIs and TypeScript DOM lib types.


### 3. Goal: non‑web code never touches the DOM

**Goal:** files outside `src/web` do not access the DOM at all. They may only
interact with DOM through small, explicit APIs exposed by `src/web`.

**Plan:**

1. **Search for DOM usage outside `src/web`.**
   - Look for `document`, `window`, `HTMLElement`, `HTMLDivElement`,
     `HTMLSpanElement`, `getElementById`, `querySelector`, and `addEventListener`
     in non‑web files.

2. **Wrap these usages with `web` APIs.**
   - For event wiring, create functions in a `web/events.ts` module such as:
     - `attachEditorKeyHandler(handler: (e: KeyboardEvent) => void): void`
     - `attachClickHandler(selector: string, handler: (ev: MouseEvent) => void)`
   - For DOM reads or writes to the editor area, use the existing `Row` /
     `Cell` APIs instead of manual DOM queries.

3. **Update call sites.**
   - Replace direct DOM access in non‑web files with calls into `src/web`.
   - Example: code that currently uses `lm.newEditor` and `document` from
     outside `web` should instead call a function in `src/web` that returns
     `Row` objects or performs the relevant update.

4. **Enforce via linting or review.**
   - As a guideline: any non‑web module that needs UI behaviour should import
     from `src/web` or an adapter, never `document` or `window`.


### 4. Cell and Row as referentially transparent, immutable objects

Here we define what it means, in this project, for `Cell` and `Row` to behave
*as if* they were referentially transparent and immutable, and how to move
toward that state.

#### 4.1 Desired properties

For this codebase, **`Cell` and `Row` should act like immutable value objects**
from the perspective of all callers:

- **Creation is pure.**
  - Constructing a `Cell` or `Row` given some input data yields an object whose
    observable properties are determined entirely by that data (and stable DOM
    structure).

- **No in‑place mutation visible to callers.**
  - Public methods on `Cell` and `Row` should:
    - Either be **queries** that do not change persistent state.
    - Or be **commands** whose side effects are confined to DOM state that the
      web layer owns.
  - From the model / scene / controller perspective, there are no observable
    mutations of `Cell` or `Row` objects; they can be treated as read‑only
    views.

- **Stable identity for a DOM element.**
  - For a given underlying `RowElement` or `CellElement`, creating multiple
    `Row` / `Cell` wrappers should not violate any invariants; they behave like
    new references to the same immutable value.

- **No shared mutable data with model.**
  - `Cell` / `Row` should never mutate model or scene data directly. Any
    changes to model or scene flow outward from controller / scene code.

Because `Cell` and `Row` wrap live DOM elements, they cannot be *literally*
pure in the functional sense. The goal is that they are **effectively
immutable view objects** for all non‑web code.


#### 4.2 How to accomplish this

1. **Restrict public API to queries plus DOM‑local commands.**
   - Keep methods like `visibleText`, `visibleTextLength`, `htmlContent`,
     `indent`, `caretOffset`, and selection helpers as **queries** that read DOM
     state but do not change model or scene.
   - Methods that modify DOM, such as `setContent`, `setCaret`, and
     `setSelection`, are **commands** owned by the web layer. Only the web
     layer should call them.

2. **Keep ownership rules strict.**
   - `Row` and `Cell` objects are created and managed only inside `src/web`.
   - External code never keeps them around as long‑lived mutable objects. It
     uses them transiently or works with IDs / pure data instead.

3. **Separate pure view data from DOM wrappers (future enhancement).**
   - Introduce pure data types such as:
     - `ViewCell = { kind: 'indent' | 'text'; text: string; width: number }`
     - `ViewRow = { id: string; cells: readonly ViewCell[] }`
   - Have `scene-editor.ts` construct these from `SceneRow` and pass them into
     the web layer.
   - `Row.setContent` becomes a mapping from `ViewRow` to DOM, which is a pure
     function of the input data plus the existing row element.

4. **Avoid hidden implicit state in `Row` / `Cell`.**
   - `_cachedCells` is fine as a lazy cache, as long as it is internal and does
     not leak mutated data structures to callers.
   - Do not store references to scene or model objects inside `Row` / `Cell`.


#### 4.3 What counts as referential transparency here

Within the context of this UI layer, a reasonable working definition is:

- For a given DOM structure and scene data, methods on `Cell` and `Row` return
  the same results every time they are called, unless the DOM or scene change
  via a clearly identified command (for example, a controller operation that
  re‑renders the row).

- Any change to model / scene is reflected by:
  - Rebuilding or updating the DOM through explicit web‑layer functions.
  - Optionally constructing new `Row` / `Cell` wrappers for changed elements.

Under this definition, `Cell` and `Row` are not pure in the academic sense, but
they are **pure enough** for callers that treat them as read‑only snapshots
of the DOM.


#### 4.4 Notes and possible alternatives

- Some caret and selection operations inherently depend on global DOM state
  (`window.getSelection()`), so they cannot be strictly referentially
  transparent.
  - These can be isolated into helper functions that are clearly marked as
    effectful.
  - Callers should treat their return values as ephemeral.

- We could introduce a fully pure layer:
  - Define a `PureRow` / `PureCell` that are plain data without any DOM
    references.
  - `Row` / `Cell` in `src/web` become *renderers* from pure data into DOM.
  - This would give very strong referential transparency, but at the cost of
    duplicating some logic.

- For now, the incremental approach is:
  - Keep all DOM‑touching code inside `src/web`.
  - Make sure `Row` / `Cell` expose only query‑style APIs to the rest of the
    system.
  - Treat them as immutable view objects owned by the web layer.


