import * as Editor from './editor.js';
import { PureCellKind } from './web/pureData.js';
import { SceneCell } from './sitecells.js';
import * as SceneEditor from './scene-editor.js';
import { ArraySpan } from './arrayspan.js';
import { model } from './model.js';
import { Doc, DocLine } from './doc.js';
import { RowCell, Site, SiteRow, SiteRowId } from './site.js';
import * as Change from './change.js';
import * as HtmlUtil from './htmlutil.js';
import { CellSelection, CellBlock, CellTextSelection, CellSpec } from './cellblock.js';
import { PureTextSelection } from './web/pureData.js';

import * as WebUI from './web/ui.js';


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
export function setCaretInCell(rowCell: RowCell, 
        focus: number, anchor: number = -1): void {
    const siteRow = model.site.findRow(rowCell.row.id);
    setCaretInSite(siteRow, rowCell.cellIndex, focus, anchor);
}
export function setCaret(cellSpec : CellSpec, offset: number, anchor: number = -1): void {
    setCaretInSite(cellSpec.row, cellSpec.cellIndex, offset, anchor);
}
// Methods to manage CellSelection selection
export function selectRow(siteRow: SiteRow): void {
	const parentSiteRow = siteRow.parent;
	const childIndex = parentSiteRow.children.indexOf(siteRow);
	if (siteRow === SiteRow.end) 
        return;
	
	if (childIndex === -1) return;
	const cellSelection = CellBlock.create(
		parentSiteRow,
		childIndex,
		childIndex);
	
	model.site.setCellBlock(cellSelection);
	model.scene.updatedSelection();
}

export function extendSelection(rowCell: RowCell, newFocus: number): void {
	const anchor = model.site.cellSelection instanceof CellTextSelection 
		? model.site.cellSelection.anchor 
		: newFocus;
	setCaretInCell(rowCell, newFocus, anchor);
}
export function moveCursorToRow(targetRow: SiteRow) {
	const x = Editor.caretX();
	const editorRow = Editor.findRow(targetRow.id.value);
	const offsetResult = editorRow.offsetAtX(x);
	if (offsetResult) {
		const cellIndex = editorRow.getCellIndex(offsetResult.cell);
		setCaretInCell(new RowCell(targetRow, targetRow.cells.at(cellIndex)), offsetResult.offset, 
			offsetResult.offset);
	}
	return true;
}
export function findPreviousEditableCell(row: SiteRow, fromCell: SceneCell): RowCell | null {
	const contentCells = row.cells;
	const fromIndex = contentCells.indexOf(fromCell);
	if (fromIndex < 0) return null;
	
	// Look backwards in current row
	for (let i = fromIndex - 1; i >= 0; i--) {
		const cell = row.cells.at(i);
		if (cell.type === PureCellKind.Text) {
			return new RowCell(row, cell);
		}
	}
	
	// Not found in current row, look in previous row
	const prevRow = row.previous;
	if (prevRow.valid) {
		const prevContentCells = prevRow.cells;
		if (prevContentCells.count > 0) {
			return new RowCell(prevRow, prevContentCells.at(prevContentCells.count - 1));
		}
	}
	return null;
}

export function findNextEditableCell(row: SiteRow, fromCell: SceneCell): RowCell | null {
	const contentCells = row.cells;
	const fromIndex = contentCells.indexOf(fromCell);
	if (fromIndex < 0) return null;
	
	// Look forwards in current row
	for (let i = fromIndex + 1; i < contentCells.count; i++) {
		const cell = contentCells.at(i);
		if (cell.type === PureCellKind.Text) {
			return new RowCell(row, cell);
		}
	}
	
	// Not found in current row, look in next row
	const nextRow = row.next;
	if (nextRow.valid) {
		return findNextEditableCell(nextRow, contentCells.at(0));
	}
	return null;
}

export function deleteCellRange(currentCell: RowCell, visibleStart: number, visibleEnd: number) {
	replaceCellRange(currentCell, visibleStart, visibleEnd, '');
}

export function replaceCellRange(currentRowCell: RowCell, visibleStart: number, visibleEnd: number, 
		newText: string) {
	// Ensure start < end
	const start = Math.min(visibleStart, visibleEnd);
	const end = Math.max(visibleStart, visibleEnd);
	
	const change = Change.makeCellTextChange(
		currentRowCell,
		start,
		end - start,
		newText
	);
	Doc.processChange(change);
	setCaretInCell(currentRowCell, start+newText.length);
}
