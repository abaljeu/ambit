import * as Editor from './editor.js';
import * as SceneEditor from './scene-editor.js';
import { ArraySpan } from './arrayspan.js';
import { model } from './model.js';
import { Doc, DocLine } from './doc.js';
import { Site, SiteRow } from './site.js';
import * as Change from './change.js';
import * as HtmlUtil from './htmlutil.js';
import { CellSelection, CellBlock, CellTextSelection, CellSpec } from './cellblock.js';
import { PureTextSelection } from './web/pureData.js';

import * as WebUI from './web/ui.js';
import { SceneRow } from './scene.js';


export function cellLocalToRowLevelOffset(row: Editor.Row, cell: Editor.Cell, cellLocalOffset: number): number {
	// Convert cell-local offset to row-level offset by accumulating visible text lengths
	// of all cells before the given cell
	const cells = row.contentCells;
	let rowLevelOffset = 0;
	
	for (const c of cells) {
		if (c.newEl === cell.newEl) {
			// Found the target cell, add the cell-local offset
			rowLevelOffset += cellLocalOffset;
			break;
		}
		// Add the full visible text length of cells before the target cell
		rowLevelOffset += c.visibleTextLength;
	}
	return rowLevelOffset;
}
export function setCaretInSite(row: SiteRow,
                cellIndex: number,
                focus: number, anchor: number = -1): void {
    const _anchor = anchor === -1 ? focus : anchor;
    model.site.setSelection(new CellTextSelection(row, cellIndex, focus, _anchor));
    model.scene.updatedSelection();
}
export function setCaretInRow(row: Editor.Row, 
        cellIndex: number,
        focus: number, anchor: number = -1): void {
    const sceneRow = model.scene.findRow(row.id);
    const siteRow = sceneRow.siteRow;
    setCaretInSite(siteRow, cellIndex, focus, anchor);
}
export function setCaret(cellSpec : CellSpec, offset: number, anchor: number = -1): void {
    setCaretInSite(cellSpec.row, cellSpec.cellIndex, offset, anchor);
}
// Methods to manage CellSelection selection
export function selectRow(initialRow: Editor.Row): void {
	const rowId = initialRow.id;
	const sceneRow = model.scene.findRow(rowId);
	const siteRow = sceneRow.siteRow;
	const parentSiteRow = siteRow.parent;
	const childIndex = parentSiteRow.children.indexOf(siteRow);
	if (siteRow === SiteRow.end) 
        return;
	
	if (childIndex === -1) return;
	const cellSelection = new CellBlock(
		parentSiteRow,
		childIndex,
		childIndex,
		0,
		-1, // -1 means all columns
		siteRow,
		0 // active cell is first cell
	);
	
	model.site.setCellBlock(cellSelection);
	model.scene.updatedSelection();
}
