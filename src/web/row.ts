import * as lm from '../elements.js';
import { ArraySpan } from '../arrayspan.js';
import { SceneRow, SceneRowCells, CellSelectionState } from '../scene.js';
import {
	RowElementTag,
	RowContentTag,
	RowContentClass,
	RowIndentClass,
	TextCellClass,
	CellFlexClass,
	CellFixedClass,
	VISIBLE_TAB,
	NOROWID,
	RowElement,
	CellElement,
	getTextOffsetFromNode,
} from './editor-dom.js';
import { Cell } from './cell.js';

function createRowElement(): RowElement {
	// Create newEditor element (3-span: fold-indicator + indentation + rowContent)
	const newEl = document.createElement(RowElementTag) as RowElement;
	const newElFold = document.createElement('span');
	newElFold.className = 'fold-indicator';
	newElFold.textContent = ' ';
	const newElContent = document.createElement(RowContentTag);
	newElContent.className = RowContentClass;
	const newElContentPool = lm.newEditor.dataset.newElContentPool;
	
	if (!newElContentPool) {
		newEl.appendChild(newElFold);
		newEl.appendChild(newElContent);
	}
	
	return newEl;
}

export function removeCarets(): void {
	window.getSelection()?.removeAllRanges();
	lm.newEditor.focus();
}

export class Row {
	private _cachedCells: readonly Cell[] = [];
	public equals(other: Row): boolean {
		return this.newEl === other.newEl;
	}
	public get cells(): readonly Cell[] {
		if (this._cachedCells.length === 0) {
			this._cachedCells = this.getContentSpans().map(span => new Cell(span));
		}
		return this._cachedCells;
	}
	public get contentCells(): readonly Cell[] {
		return this.cells.filter(cell => cell.isText);
	}
	public get activeCell(): Cell | null {
		// First check for CellBlock active cell (when in cell block mode)
		const cellBlockActive = this.cells.find(cell => cell.hasCellBlockActive());
		if (cellBlockActive) 
			return cellBlockActive;
		
		// Otherwise, check DOM focus (normal text editing mode)
		return this.cells.find(cell => cell.active) ?? null;
	}
	public getActiveCellIndex(): number {
		const contentCells = this.contentCells;
		const activeCell = this.activeCell;
		if (!activeCell) return -1;
		const index = contentCells.indexOf(activeCell);
		return index;
	}
	
	// Get cell index for a given Cell element
	public getCellIndex(cell: Cell): number {
		const allCells = this.cells;
		const index = allCells.indexOf(cell);
		return index;
	}
	public getCellLineOffset(cell : Cell): number {
		const currentCellIndex = this.getCellIndex(cell);
		if (currentCellIndex === -1) return -1;
		// Get the html offset of the current cell
		// Sum the html length of the previous cells, plus 1 for the \t
		const contentCells = this.contentCells;
		let htmlOffset = 0;
		for (let i = 0; i < currentCellIndex; i++) {
			htmlOffset += contentCells[i].htmlContent.length;
			htmlOffset += 1;
		}
		return htmlOffset;
	}
	constructor(
		public readonly newEl: RowElement
	) {}
	private getContentSpans(): readonly CellElement[] {
		const contentElement = this.newEl.querySelector(`.${RowContentClass}`) as HTMLElement;
		if (!contentElement) return [];
		// Get all children that are either indent or text cells, in document order
		return Array.from(contentElement.children).filter(child => 
			child.classList.contains(RowIndentClass) || 
			child.classList.contains(TextCellClass)
		) as CellElement[];
	}
	private getFoldIndicatorSpan(): HTMLSpanElement {
		return this.newEl.querySelector('.fold-indicator') as HTMLSpanElement;
	}

	public getHtmlOffset(): number {
		// Get visible offset from DOM (includes cell context)
		const caret = this.caretOffset;
		if (!caret) 
			return 0;
		
		// Calculate row-level HTML offset by:
		// 1. Getting HTML offset within the active cell
		// 2. Adding HTML content lengths of text cells before the active cell
		// 3. Adding \t lengths between text cells (since htmlContent joins with \t)
		const textCells = this.contentCells;
		let rowLevelHtmlOffset = 0;
		
		for (let i = 0; i < textCells.length; i++) {
			const cell = textCells[i];
			if (cell.newEl === caret.cell.newEl) {
				// Found the active cell, add its cell-local HTML offset
				rowLevelHtmlOffset += caret.cell.getHtmlOffset(caret.offset);
				break;
			}
			// Add the full HTML content length of text cells before the active cell
			rowLevelHtmlOffset += cell.htmlContent.length;
			// Add \t length if there are more cells after this one
			if (i < textCells.length - 1) {
				rowLevelHtmlOffset += 1; // \t character length
			}
		}
		
		return rowLevelHtmlOffset;
	}
	
	public get visibleText() : string {
		const cells = this.cells;
		if (cells.length === 0) return '';
		return cells.map(cell => cell.visibleText).join('');
	}
	public get visibleTextLength() : number {
		return this.visibleText.length;
	}
	public get htmlContent(): string {
		// Only include text cells (editable cells), not indent cells
		// Join multiple text cells with \t to represent internal tabs
		const textCells = this.contentCells;
		if (textCells.length === 0) return '';
		return textCells.map(cell => cell.htmlContent).join('\t');
	}
	public setContent(cells: SceneRowCells) {
		let rowContent = this.newEl.querySelector(`.${RowContentClass}`) as HTMLElement;
		if (!rowContent) {
			// If rowContent doesn't exist, create it
			rowContent = document.createElement(RowContentTag);
			rowContent.className = RowContentClass;
			this.newEl.appendChild(rowContent);
		}
		// Clear only rowContent's innerHTML, preserving fold-indicator
		rowContent.innerHTML = '';
		
		for (const cell of cells.cells) {
			if (cell.type === 'indent') {
				const indentSpan = document.createElement('span');
				indentSpan.className = RowIndentClass;
				indentSpan.textContent = VISIBLE_TAB;
				rowContent.appendChild(indentSpan);
			}
			else if (cell.type === 'text') {
				const textSpan = document.createElement('span');
				textSpan.className = TextCellClass;
				textSpan.contentEditable = 'true';
				// width of cell is source cell width in ems
				// if -1, fills container after all other cells are set (flex: 1)
				// if > 0, min 1em, max to fit content
				if (cell.width === -1 || cell.width === 0) {
					textSpan.classList.add(CellFlexClass);
				} else {
					textSpan.classList.add(CellFixedClass);
					textSpan.style.width = `${cell.width}em`;
				}
				
				textSpan.textContent = cell.text;
				rowContent.appendChild(textSpan);
			}
		}
		this._cachedCells = [];
	}
	public get indent(): number {
		// count the number of indent-units in the indentation span
		return this.cells.filter((c: Cell) => c.isIndent).length;
	}
	public setFoldIndicator(indicator: string) {
		const foldSpan = this.getFoldIndicatorSpan();
		if (foldSpan) foldSpan.textContent = indicator;
	}
	public get previous(): Row {
		const previousSibling = this.newEl?.previousElementSibling;
		if (!previousSibling) return endRow;
		return new Row(previousSibling as RowElement);
	}
	public get next(): Row {
		const nextSibling = this.newEl?.nextElementSibling;
		if (!nextSibling ) return endRow;
		return new Row(nextSibling as RowElement);
	}
	public valid(): boolean {
		return this.newEl !== null  && this.id !== NOROWID;
	}
	public get id(): string {
		return this.newEl?.dataset.lineId ?? NOROWID;
	}
	public moveCaretToThisRow(): void {
		const targetX = caretX();
		// Find which cell contains the X coordinate
		const result = this.offsetAtX(targetX);
		if (result) {
			// Delegate to that cell's moveCaretToThisCell() method
			result.cell.moveCaretToThisCell(targetX);
		}
	}
	public get caretOffset(): { cell: Cell, offset: number }  {
		// Find active cell using Cell.active() getter
		const activeCell = this.cells.find(cell => cell.active);
		if (!activeCell) {
			const firstCell = this.cells[0];
			if (!firstCell) {
				// No cells at all - return a sentinel
				return { cell: new Cell(document.createElement('span')), offset: 0 };
			}
			return { cell: firstCell, offset: 0 };
		}
		
		// Delegate to that cell's caretOffset() method (returns cell-local offset)
		const offset = activeCell.caretOffset();
		return { cell: activeCell, offset };
	}
	
	public setCaretInRow(visibleOffset: number) {
		// Find cell context first: determine which cell should contain the cursor based on row-level offset
		let cumulativeLength = 0;
		const contentCells = this.contentCells;
		for (const cell of contentCells) {
			const cellLength = cell.visibleTextLength;
			if (visibleOffset <= cumulativeLength + cellLength) {
				// Calculate cell-local offset by subtracting preceding cells' cumulative text lengths
				const cellLocalOffset = visibleOffset - cumulativeLength;
				// Delegate to that cell's setCaret() method
				cell.setCaret(cellLocalOffset);
				return;
			}
			cumulativeLength += cellLength;
		}
		// Handle edge case: offset beyond row length - place at end of last cell
		if (contentCells.length > 0) {
			const lastCell = contentCells[contentCells.length - 1];
			lastCell.setCaret(lastCell.visibleTextLength);
		}
	}
	public setSelectionInRow(visibleStart: number, visibleEnd: number): void {
		// Find cell context first: determine which cell contains the selection based on row-level offsets
		// Controller ensures start and end are in the same cell
		let cumulativeLength = 0;
		for (const cell of this.cells) {
			const cellLength = cell.visibleTextLength;
			// Check if both start and end are within this cell
			if (visibleStart <= cumulativeLength + cellLength && visibleEnd <= cumulativeLength + cellLength) {
				// Calculate cell-local offsets by subtracting preceding cells' cumulative text lengths
				const cellLocalStart = visibleStart - cumulativeLength;
				const cellLocalEnd = visibleEnd - cumulativeLength;
				// Delegate to that cell's setSelection() method
				cell.setSelection(cellLocalStart, cellLocalEnd);
				return;
			}
			cumulativeLength += cellLength;
		}
	}
	
	public extendSelectionInRow(visibleOffset: number): void {
		// Get current caret position (includes cell context)
		const caret = this.caretOffset;
		if (!caret) {
			// No active cell, start selection from beginning
			this.setSelectionInRow(0, visibleOffset);
			return;
		}
		
		// Use the caret's cell and offset as anchor
		// Calculate row-level offset from cell context
		let cumulativeLength = 0;
		for (const cell of this.cells) {
			if (cell === caret.cell) {
				const anchorOffset = cumulativeLength + caret.offset;
				this.setSelectionInRow(anchorOffset, visibleOffset);
				return;
			}
			cumulativeLength += cell.visibleTextLength;
		}
	}
	
	public getSelectionRange(): { start: number, end: number } {
		// Find active cell using Cell.active() getter
		const activeCell = this.cells.find(cell => cell.active);
		if (!activeCell) {
			// Return {start: 0, end: 0} if no cell is active
			return {start: 0, end: 0};
		}
		
		// Delegate to that cell's getSelectionRange() method (returns cell-local offsets)
		// Note: Returned offsets are cell-local, cell context is implicit (the active cell)
		return activeCell.getHtmlSelectionRange();
	}
	
	public offsetAtX(x: number): { cell: Cell, offset: number } | null {
		// Find which cell contains the X coordinate using getBoundingClientRect() on each cell
		for (const cell of this.contentCells) {
			const rect = cell.newEl.getBoundingClientRect();
			// Check if X is within cell bounds
			if (x >= rect.left && x <= rect.right) {
				// Delegate to that cell's offsetAtX() method to get cell-local offset
				const offset = cell.offsetAtX(x);
				return { cell, offset };
			}
		}
		// Return null if X is not in any cell
		return null;
	}

	// Update CSS classes for all cells in this row based on selection states
	// selectionStates: array of { cellIndex, selected, active } for each cell
	public updateCellBlockStyling(selectionStates: readonly CellSelectionState[]): void {
		for (const state of selectionStates) {
			const cell = this.cells[state.cellIndex];
			if (cell) {
				cell.updateCellBlockStyling({ 
					selected: state.selected, 
					active: state.active 
				});
			}
		}
	}
}

export function createRowElementFromSceneRow(sceneRow: SceneRow): Row {
	const newEl = createRowElement();
	const row = new Row(newEl);

	newEl.dataset.lineId = sceneRow.id.value;
	row.setContent(sceneRow.cells);
	return row;
}

export function findRow(id: string): Row {
	for (const row of rows()) {
		if (row.id == id) 
			return row;
	}
	return endRow;
}

// RowSpan is not an ArraySpan because the rows are virtual.
// it's implemented by taking a row and finding the next row.
export class RowSpan implements Iterable<Row> {
	constructor(public readonly row: Row, public readonly count: number) {}
	
	*[Symbol.iterator](): Iterator<Row> {
		let row = this.row;
		for (let i = 0; i < this.count; i++) {
			yield row;
			row = row.next;
		}
	}
	public endRow(): Row { // [row, row+count)
		let row = this.row;
		for (let i = 0; i < this.count; i++) {
			row = row.next;
		}
		return row;
	}
	public last() : Row {
		let row = this.row;
		for (let i = 0; i < this.count - 1; i++) {
			row = row.next;
		}
		return row;
	}
}

// Sentinel row used to indicate insertion at the start of the container
const endRowElement = document.createElement(RowElementTag) as RowElement;
export const endRow: Row = new Row(endRowElement);

// Iterator that yields row elements from the editor
function* rowElements(): IterableIterator<RowElement> {
	const rowElems = lm.newEditor.querySelectorAll(RowElementTag);
	
	for (let i = 0; i < rowElems.length; i++) {
		const element = rowElems[i] as RowElement;
		yield element;
	}
}

// create an iterator that yields rows from the DOM
export function* rows(): IterableIterator<Row> {
	for (const pair of rowElements()) {
		yield new Row(pair);
	}
}

export function at(index: number): Row {
	// if index is out of range, return endRow
	if (index < 0 || index >= lm.newEditor.childElementCount) return endRow;
	const element = lm.newEditor.children[index] as RowElement;
	return new Row(element);
}

// Create a new row and insert it after the given previous row.
// If previousRow is endRow, insert at the front of the container.
export function addBefore(targetRow: Row, scene: ArraySpan<SceneRow>)
	: RowSpan {
	if (lm.newEditor === null) 
		return new RowSpan(endRow, 0);
	
	let firstRow = endRow;
	for (const sceneRow of scene) {
		const row = createRowElementFromSceneRow(sceneRow);
		if (firstRow === endRow) firstRow = row;

		// Insert into newEditor
		if (targetRow === endRow) {
			lm.newEditor.appendChild(row.newEl);
		} else {
			lm.newEditor.insertBefore(row.newEl, targetRow.newEl);
		}
	}
	return new RowSpan(firstRow, scene.length);
}

export function addAfter(
	referenceRow: Row, 
	rowDataArray: ArraySpan<SceneRow>
): RowSpan {
	if (lm.newEditor === null) return new RowSpan(endRow, 0);
	let count = 0;
	let newBeforeRow = referenceRow.newEl;
	let first = endRow;
	
	for (const rowData of rowDataArray) {
		const row = createRowElementFromSceneRow(rowData);
		
		// Insert into newEditor
		if (newBeforeRow.nextSibling) {
			lm.newEditor.insertBefore(row.newEl, newBeforeRow.nextSibling);
		} else {
			lm.newEditor.appendChild(row.newEl);
		}
		
		if (first === endRow) first = row;
		newBeforeRow = row.newEl;
		count++;
	}
	
	return new RowSpan(first, count);
}

function getCurrentParagraphWithOffset(): { element: RowElement, offset: number } | null {
	const selection = window.getSelection();
	if (!selection || selection.rangeCount === 0) return null;
	
	const range = selection.getRangeAt(0);
	let node = selection.anchorNode;

	// Navigate up to find the row div in newEditor container
	let currentP: RowElement | null = null;
	while (node && node !== lm.newEditor) {
		if (node.nodeName === RowElementTag.toUpperCase() &&
			node.parentNode === lm.newEditor) {
			currentP = node as RowElement;
			break;
		}
		node = node.parentNode;
	}

	if (!currentP) return null;

	// Calculate offset within content span
	const contentSpan = currentP.querySelector(`.${RowContentClass}`) as HTMLSpanElement;
	if (!contentSpan) return { element: currentP, offset: 0 };

	// Use helper to calculate text offset from DOM position
	const offset = getTextOffsetFromNode(
		contentSpan,
		range.startContainer,
		range.startOffset
	);

	return { element: currentP, offset };
}

export function currentRow(): Row {
	const p = getCurrentParagraphWithOffset();
	if (!p) return endRow;
    
	const parent = p.element.parentNode as RowElement | null;
	if (parent === lm.newEditor) {
		// p.element is from newEditor
		return new Row(p.element);
	}
	
	return endRow;
}

export function caretX(): number {
	const sel = window.getSelection();
	if (!sel || sel.rangeCount === 0) return 0;
	const r = sel.getRangeAt(0).cloneRange();
	r.collapse(true);
	const rect = r.getBoundingClientRect();
	return rect.left;
}

export function replaceRows(oldRows: RowSpan, newRows: ArraySpan<SceneRow>): RowSpan {
	if (oldRows.count === 0 && newRows.length === 0) return new RowSpan(endRow, 0);
	
	const beforeStartRow = oldRows.row.previous;
	
	// Collect all rows first before removing (to avoid breaking the DOM chain)
	const rowsToRemove: Row[] = Array.from(oldRows);
	
	// Now remove them
	for (const row of rowsToRemove) {
		row.newEl.remove();
	}
	
	return addAfter(beforeStartRow, newRows);
}

export function setEditorContent(scene: ArraySpan<SceneRow>): RowSpan {
	// Clear the Editor
	lm.newEditor.innerHTML = '';

	// Create a Line element for each visible line
	const end = endRow;
	return addBefore(end, scene);
}


