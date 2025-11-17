### Migration steps to achieve [[web-architecture]]

1. **Introduce `PureCell` / `PureRow` types.**
[x]   a. Add the pure types module [[src/web/editorData.ts]]
[x]   b. In scene-editor.ts, create sceneCellToPureCell, and sceneRowToPureRow 
[x]   c. Add sceneCellMatchesEditorCell, sceneRowMatchesEditorRow, using the pure objects.

2. **Refactor [[web/row.ts]] and `web/cell.ts`.**
 [x]  a. Change `Row.setContent` / `updateCellBlockStyling` to use the pure types.
 [x]  b. Add `toPureRow` / `toPureCell` helpers

3. **Move scene‑specific logic into `scene-editor.ts`.**
 [x]  a. Replace direct `SceneRow` / `SceneRowCells` imports from `src/web` with
     `PureRow` conversions in `scene-editor.ts`.
 [x]  d. Update a small path (e.g. `setEditorContent`) to accept `PureRow[]` and
     render them.

4. **Eliminate non‑web DOM usage.**
 [x] a. Move events.ts into web, but invert the dependencies.  ambit.ts is the main program that installs the scripted content.
 [x] b. Find DOM calls outside `src/web` and replace them with calls into web
     APIs that work with `Row` / `Cell` or pure offsets.
 [x] c. Move elements.ts to web.

5. **Tighten API and naming over time.**
   - Once the flow is stable, we can further separate “readback for tests”
     from “runtime rendering” if needed, but the primary invariants are:
     - DOM is only in `src/web`.
     - Shared state across layers is expressed in pure types.


