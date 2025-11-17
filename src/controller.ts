import * as Editor from './editor.js';
import * as SceneEditor from './scene-editor.js';
import { ArraySpan } from './arrayspan.js';
import { model } from './model.js';
import { Doc, DocLine } from './doc.js';
import { Site, SiteRow } from './site.js';
import * as Change from './change.js';
import * as HtmlUtil from './htmlutil.js';
import { CellSelection, CellBlock, CellTextSelection } from './cellblock.js';
import { PureTextSelection } from './web/pureData.js';

import * as WebUI from './web/ui.js';

export function setMessage(message: string): void {
	WebUI.setMessage(message);
}

export function links() {
// 	// Get the content from the Editor div
// 	const textareaValue = getEditorContent();

// 	// Clear previous links
// 	lm.linksDiv.innerHTML = '';

// 	// Regular expression to find wikilinks
// 	const wikilinkRegex = /\[\[([a-zA-Z0-9 _\.-]+)\]\]/g;
// 	let match;
// 	let linksHTML = '';

// 	// Find all matches and generate links
// 	while ((match = wikilinkRegex.exec(textareaValue)) !== null) {
// 		const linkText = match[1]; // Get the text inside [[ ]]
// 		linksHTML += `<a href="ambit.php?doc=${encodeURIComponent(linkText) + '.amb'}">${linkText}</a><br>`;
// 	}

// 	// Inject the generated links into the links div
// 	lm.linksDiv.innerHTML = linksHTML;
}

function updateAllFoldIndicators() {
	const scene = model.scene;
	for (const row of Editor.rows()) {
		updateFoldIndicator(row);
	}
}
class KeyBinding {
	constructor(
		readonly combo: string,
		readonly handler: (row: Editor.Row) => boolean | void
	) {}
}

const keyBindings: KeyBinding[] = [
	new KeyBinding("F12", () => false),
	new KeyBinding("F5", () => false),
	new KeyBinding("C-F5", () => false),
	new KeyBinding("Tab", handleTab),
	new KeyBinding("S-Tab", handleShiftTab),
	new KeyBinding("C-s", () => { save(); return true; }),
	new KeyBinding("C-b", () => handleAddMarkup("b")),
	new KeyBinding("Enter", handleEnter),
	new KeyBinding("Backspace", () => handleBackspace()),
	new KeyBinding("Delete", () => handleDelete()),
	new KeyBinding("ArrowUp", handleArrowUp),
	new KeyBinding("ArrowDown", handleArrowDown),
	new KeyBinding("ArrowLeft", handleArrowLeft),
	new KeyBinding("ArrowRight", handleArrowRight),
	new KeyBinding("S-ArrowLeft", handleShiftArrowLeft),
	new KeyBinding("S-ArrowRight", handleShiftArrowRight),
	new KeyBinding("S-ArrowUp", handleShiftArrowUp),
	new KeyBinding("S-ArrowDown", handleShiftArrowDown),
	new KeyBinding("Home", handleHome),
	new KeyBinding("End", handleEnd),
	new KeyBinding("S-Home", handleShiftHome),
	new KeyBinding("S-End", handleShiftEnd),
	new KeyBinding("C-ArrowLeft", handleWordLeft),
	new KeyBinding("C-ArrowRight", handleWordRight),
	new KeyBinding("C-S-ArrowLeft", handleShiftWordLeft),
	new KeyBinding("C-S-ArrowRight", handleShiftWordRight),
	new KeyBinding("C-ArrowUp", handleSwapUp),
	new KeyBinding("C-ArrowDown", handleSwapDown),
	new KeyBinding("C-.", handleToggleFold),
];

function findKeyBinding(combo: string): KeyBinding {
	let binding = keyBindings.find(kb => kb.combo === combo);
	if (binding) return binding;
	if (combo.length == 1) {
		return new KeyBinding(combo, (row) => handleInsertChar(row, combo));
	} else if (combo.length == 3 && combo[0] == 'S') {
		// shifted character.
		return new KeyBinding(combo, (row) => handleInsertChar(row, combo[2]));
	}
	return new KeyBinding("", () => true);
}

function getCurrentRow(): Editor.Row {
	// If there's an active CellSelection, use its activeSiteRow to determine current row
	const selection = Editor.currentSelection();
	const cellSelection = model.site.cellSelection;


	if (selection && selection instanceof PureTextSelection) {
		const sceneRow = model.scene.findRow(selection.rowid.toString());
		model.site.setSelection(
			new CellTextSelection(
				sceneRow.siteRow, 
				selection.cellIndex, 
				selection.focus, 
				selection.anchor));
		return Editor.findRow(selection.rowid.value);
	} 
	else if (cellSelection instanceof CellBlock) {
		const activeSiteRow = cellSelection.activeSiteRow;
		const sceneRow = model.scene.findOrCreateSceneRow(activeSiteRow);
		const editorRow = Editor.findRow(sceneRow.id.value);
		if (editorRow !== Editor.endRow) {
			return editorRow;
		} else {
			return Editor.currentRow();
		}
	}
	else if (cellSelection instanceof CellTextSelection) {
		const sceneRow = model.scene.findOrCreateSceneRow(cellSelection.row);
		const editorRow = Editor.findRow(sceneRow.id.value);
		if (editorRow !== Editor.endRow) {
			return editorRow;
		}
	}
	
	// Otherwise, use browser selection to determine current row
	return Editor.currentRow();
}

 export function editorHandleKey(e : KeyboardEvent) {
	if ( // just a mod key was pressed.
		e.key === "Control" ||
		e.key === "Shift" ||
		e.key === "Alt" ||
		e.key === "Meta") 
		return;
	const mods =
		`${e.ctrlKey ? "C-" : ""}` +
		`${e.altKey ? "A-" : ""}` +
		`${e.shiftKey ? "S-" : ""}` +
		`${e.metaKey ? "M-" : ""}`;
	const combo =  mods + e.key;
	WebUI.setMessage(combo);

	const currentRow = getCurrentRow();
	if (!currentRow.valid()) 
		return;
	const binding = findKeyBinding(combo);
	if (binding) {
		const result = binding.handler(currentRow);
		if (result === true) {
			e.preventDefault();
		}
	} 
		updateAllFoldIndicators();
 }


 
 function handleEnter() : boolean {
	const currentRow = Editor.currentRow();
	const pure = currentRow.toPureRow();

	const htmlOffset = currentRow.getHtmlOffset();
	// const _htmlOffset = currentRow.getHtmlOffset();
	// if (_htmlOffset !== htmlOffset) {
	// 	return true;

	const docLine = docLineFromRow(currentRow);
	const beforeText = currentRow.htmlContent.substring(0, htmlOffset);
	const newDocLine = Doc.createLine(beforeText);
	const insertBefore = Change.makeInsertBefore(docLine, [newDocLine]);
	Doc.processChange(insertBefore);
	const textChange = Change.makeTextChange(docLine, 0, htmlOffset, '');
	Doc.processChange(textChange);
 	return true;
 }

function joinRows(prevRow: Editor.Row, nextRow: Editor.Row) {
	const prevDocLine = docLineFromRow(prevRow);
	const nextDocLine = docLineFromRow(nextRow);

	if (prevDocLine.children.length > 0) {
		return;
	}
	// 1) Move nextDocLine to immediately after prevDocLine
	const after = prevDocLine.nextSibling();
	if (after == nextDocLine) {}
	else { // normally next will be after an ancestor, but if not, doesn't matter.
		Doc.processChange(new Change.MoveBefore(nextDocLine, prevDocLine.parent));
	}

	// 2) Set nextDocLine text to concatenation of both lines' text
	const concatenated = prevDocLine.content + nextDocLine.content;
	Doc.processChange(new Change.TextChange(nextDocLine, concatenated));

	// 3) Remove prevDocLine
	Doc.processChange(new Change.Remove([prevDocLine]));
	updateAllFoldIndicators();
}

function handleBackspace() : boolean {
	// Check if there's a selection using active cell's selection range
	const currentRow = Editor.currentRow();
	const activeCell = currentRow.activeCell;
	if (!activeCell)
		return true;
	const selectionRange = activeCell.getHtmlSelectionRange();
	if (selectionRange.start !== selectionRange.end) {
		const cellStart = currentRow.getCellLineOffset(activeCell);
		const start = selectionRange.start + cellStart;
		const end = selectionRange.end + cellStart;

		deleteRowRange(currentRow, cellStart, end);
		return true;
	}
	const caret = activeCell.caretOffset();
	if (caret > 0) { // delete selected text.
		const start = cellLocalToRowLevelOffset(currentRow, activeCell, caret)-1;
		const end = start + 1;

		deleteRowRange(currentRow, start, end);
		return true;
	}
	 if (activeCell == currentRow.contentCells[0]) { // join row to previous
		const prevRow = currentRow.previous;
		const prevPosition = prevRow.visibleTextLength;
		if (!prevRow.valid()) return true;
		joinRows(prevRow, currentRow);
		const cellIndex = currentRow.getCellIndex(activeCell);
		setCaretInRow(currentRow, cellIndex, prevPosition);
		return true;
	}
	 // join cell to previous  
	const cellOffset = cellLocalToRowLevelOffset(currentRow, activeCell, 0);
	deleteRowRange(currentRow, cellOffset-1, cellOffset);
	return true;
}
function cellLocalToRowLevelOffset(row: Editor.Row, cell: Editor.Cell, cellLocalOffset: number): number {
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

function getActiveCellIndex(row: Editor.Row): number {
	const cellSelection = model.site.cellSelection;
	if (cellSelection instanceof CellBlock) {
		return cellSelection.activeCellIndex;
	} else if (cellSelection instanceof CellTextSelection) {
		return cellSelection.cellIndex;
	}
	return 0;
}

function deleteRowRange(currentRow: Editor.Row, visibleStart: number, visibleEnd: number) {
	const cur = model.scene.findRow(currentRow.id);
	const scur = cur.siteRow;
	const htmlContent = currentRow.htmlContent;
	
	// Ensure start < end
	const start = Math.min(visibleStart, visibleEnd);
	const end = Math.max(visibleStart, visibleEnd);
	
	// Convert visible offsets to HTML offsets
	const htmlStart = HtmlUtil.visibleOffsetToHtmlOffset(htmlContent, start);
	const htmlEnd = HtmlUtil.visibleOffsetToHtmlOffset(htmlContent, end);
	
	// Delete the selected text
	const change = Change.makeTextChange(
		scur.docLine,
		htmlStart,
		htmlEnd - htmlStart,
		''
	);
	Doc.processChange(change);
	
	// Place cursor at the start of where the selection was
	const updatedRow = Editor.findRow(currentRow.id);
	const cellIndex = getActiveCellIndex(updatedRow);
	setCaretInRow(updatedRow, cellIndex, start);
}

function deleteVisibleCharBefore(currentRow: Editor.Row, htmlOffset: number) {
	const cur = model.scene.findRow(currentRow.id);
	const scur = cur.siteRow;
	const htmlContent = currentRow.htmlContent;
	
	// Find the previous visible character, skipping HTML tags
	const visibleChar = HtmlUtil.findPreviousVisibleChar(htmlContent, htmlOffset);
	
	if (visibleChar) {
		const change = Change.makeTextChange(
			scur.docLine, 
			visibleChar.start, 
			visibleChar.length, 
			''
		);
		Doc.processChange(change);
		
		// Get updated row and convert HTML offset to visible offset
		const updatedRow = Editor.findRow(currentRow.id);
		const newVisibleOffset = HtmlUtil.htmlOffsetToVisibleOffset(
			updatedRow.htmlContent, 
			visibleChar.start
		);
		const cellIndex = getActiveCellIndex(updatedRow);
		setCaretInRow(updatedRow, cellIndex, newVisibleOffset);
	}
}

function deleteVisibleCharAt(currentRow: Editor.Row, htmlOffset: number) {
	const cur = model.scene.findRow(currentRow.id);
	const scur = cur.siteRow;
	const htmlContent = currentRow.htmlContent;
	
	// Find the next visible character, skipping HTML tags
	const visibleChar = HtmlUtil.findNextVisibleChar(htmlContent, htmlOffset);
	
	if (visibleChar) {
		const change = Change.makeTextChange(
			scur.docLine, 
			visibleChar.start, 
			visibleChar.length, 
			''
		);
		Doc.processChange(change);
		
		// Get updated row and convert HTML offset to visible offset
		const updatedRow = Editor.findRow(currentRow.id);
		const newVisibleOffset = HtmlUtil.htmlOffsetToVisibleOffset(
			updatedRow.htmlContent, 
			htmlOffset
		);
		const cellIndex = getActiveCellIndex(updatedRow);
		setCaretInRow(updatedRow, cellIndex, newVisibleOffset);
	}
}
function handleDelete() : boolean {
	// Check if there's a selection using active cell's selection range
	const currentRow = Editor.currentRow();
	const activeCell = currentRow.activeCell;
	if (activeCell) {
		const selectionRange = activeCell.getHtmlSelectionRange();
		if (selectionRange.start !== selectionRange.end) {
			// Convert cell-local offsets to row-level offsets
			const rowLevelStart = cellLocalToRowLevelOffset(currentRow, activeCell, selectionRange.start);
			const rowLevelEnd = cellLocalToRowLevelOffset(currentRow, activeCell, selectionRange.end);
			// Delete the selection
			deleteRowRange(currentRow, rowLevelStart, rowLevelEnd);
			return true;
		}
	}
	
	const caret = currentRow.caretOffset;
	const offset = caret?.offset ?? 0;
	if (offset >= currentRow.visibleTextLength) {
		const nextRow = currentRow.next;
		if (!nextRow.valid()) return true;
		joinRows(currentRow, nextRow);
		// currentRow.setCaretInRow(0); position was okay already.
		return true;
	}
	else {
		const htmlOffset = currentRow.getHtmlOffset();
		deleteVisibleCharAt(currentRow, htmlOffset);
		return true; 
	}
}
function findPreviousEditableCell(row: Editor.Row, fromCell: Editor.Cell): Editor.Cell | null {
	const contentCells = row.contentCells;
	const fromIndex = contentCells.findIndex(cell => cell.newEl === fromCell.newEl);
	if (fromIndex < 0) return null;
	
	// Look backwards in current row
	for (let i = fromIndex - 1; i >= 0; i--) {
		return contentCells[i];
	}
	
	// Not found in current row, look in previous row
	const prevRow = row.previous;
	if (prevRow.valid()) {
		const prevContentCells = prevRow.contentCells;
		if (prevContentCells.length > 0) {
			return prevContentCells[prevContentCells.length - 1];
		}
	}
	
	return null;
}

function findNextEditableCell(row: Editor.Row, fromCell: Editor.Cell): Editor.Cell | null {
	const contentCells = row.contentCells;
	const fromIndex = contentCells.findIndex(cell => cell.newEl === fromCell.newEl);
	if (fromIndex < 0) return null;
	
	// Look forwards in current row
	for (let i = fromIndex + 1; i < contentCells.length; i++) {
		return contentCells[i];
	}
	
	// Not found in current row, look in next row
	const nextRow = row.next;
	if (nextRow.valid()) {
		const nextContentCells = nextRow.contentCells;
		if (nextContentCells.length > 0) {
			return nextContentCells[0];
		}
	}
	
	return null;
}

function handleArrowUp() : boolean {
	const cellSelection = model.site.cellSelection;
	
	// If cellSelection is empty, initialize it to current row
	if (cellSelection instanceof CellTextSelection) {
		const prevP = cellSelection.row.previous;
		if (!prevP.valid)
			return true;
		
		const currentRow = Editor.currentRow();
		const prevRow = Editor.findRow(prevP.id.toString());
		const x = Editor.caretX();
		const offsetResult = prevRow.offsetAtX(x);
		if (offsetResult) {
			const cellIndex = prevRow.getCellIndex(offsetResult.cell);
			setCaretInRow(prevRow, cellIndex, offsetResult.offset);
		}
		return true;
	} else {
		const activeSiteRow = cellSelection.activeSiteRow;
		const parentSiteRow = cellSelection.parentSiteRow;
		const activeChildIndex = parentSiteRow.children.indexOf(activeSiteRow);
		
		if (activeChildIndex === -1)
			return true;
		
		const isActiveAtStart = activeChildIndex === cellSelection.startChildIndex;
		const startChildIndex = cellSelection.startChildIndex;
		const isStartAtFirst = startChildIndex <= 0;
		
		let newStartIndex = cellSelection.startChildIndex;
		let newEndIndex = cellSelection.endChildIndex;
		let newActiveSiteRow = activeSiteRow;
		let newActiveCellIndex = cellSelection.activeCellIndex;
		
		if (!isActiveAtStart) {
			// Set active cell to start row
			const startRow = parentSiteRow.children[startChildIndex];
			newActiveSiteRow = startRow;
			newActiveCellIndex = startChildIndex;
		} else if (!isStartAtFirst) {
			// Shift the selection (start and end) up one
			newStartIndex = cellSelection.startChildIndex - 1;
			newEndIndex = cellSelection.endChildIndex - 1;
			const newStartRow = parentSiteRow.children[newStartIndex];
			newActiveSiteRow = newStartRow;
			newActiveCellIndex = newStartIndex;
		} else {
			// Do nothing
			return true;
		}
		
		const newCellSelection = new CellBlock(
			parentSiteRow,
			newStartIndex,
			newEndIndex,
			cellSelection.startColumnIndex,
			cellSelection.endColumnIndex,
			newActiveSiteRow,
			newActiveCellIndex
		);
		
		model.site.setCellBlock(newCellSelection);
		model.scene.updatedSelection();
		return true;
	}
}

 function handleArrowDown() : boolean {
	const cellSelection = model.site.cellSelection;
	
	// If cellSelection is empty, initialize it to current row
	if (cellSelection instanceof CellTextSelection) {
		const currentRow = Editor.currentRow();
		const nextP = currentRow.next;
		if (!nextP.valid())
			return true;
		const nextRow = Editor.findRow(nextP.id.toString());
		const x = Editor.caretX();
		const offsetResult = nextRow.offsetAtX(x);
		if (offsetResult) {
			const cellIndex = nextRow.getCellIndex(offsetResult.cell);
			setCaretInRow(nextRow, cellIndex, offsetResult.offset);
		}
		return true;
	} else if (cellSelection instanceof CellBlock) {
		const activeSiteRow = cellSelection.activeSiteRow;
		const parentSiteRow = cellSelection.parentSiteRow;
		const activeChildIndex = parentSiteRow.children.indexOf(activeSiteRow);
		
		if (activeChildIndex === -1)
			return true;
		
		const isActiveAtStart = activeChildIndex === cellSelection.startChildIndex;
		const isActiveAtEnd = activeChildIndex === cellSelection.endChildIndex;
		const endChildIndex = cellSelection.endChildIndex;
		const isEndAtLast = endChildIndex >= parentSiteRow.children.length - 1;
		
		let newStartIndex = cellSelection.startChildIndex;
		let newEndIndex = cellSelection.endChildIndex;
		let newActiveSiteRow = activeSiteRow;
		let newActiveCellIndex = cellSelection.activeCellIndex;
		
		if (!isActiveAtEnd) {
			// Set active cell to end row
			const endRow = parentSiteRow.children[endChildIndex];
			newActiveSiteRow = endRow;
			newActiveCellIndex = endChildIndex;
		} else if (!isEndAtLast) {
			// Shift the selection (start and end) down one
			newStartIndex = cellSelection.startChildIndex + 1;
			newEndIndex = cellSelection.endChildIndex + 1;
			const newEndRow = parentSiteRow.children[newEndIndex];
			newActiveSiteRow = newEndRow;
			newActiveCellIndex = newEndIndex;
		} else {
			// Do nothing
			return true;
		}
		
		const newCellSelection = new CellBlock(
			parentSiteRow,
			newStartIndex,
			newEndIndex,
			cellSelection.startColumnIndex,
			cellSelection.endColumnIndex,
			newActiveSiteRow,
			newActiveCellIndex
		);
		
		model.site.setCellBlock(newCellSelection);
		model.scene.updatedSelection();
		return true;
	}
	return true;

}

function handleShiftArrowUp(): boolean {
	const cellSelection = model.site.cellSelection;
	
	// If cellSelection is empty, initialize it to current row
	if (cellSelection instanceof CellTextSelection) {
		const currentRow = Editor.currentRow();
		initCellSelectionToRow(currentRow);
		return true;
	}
	
	const activeSiteRow = cellSelection.activeSiteRow;
	const parentSiteRow = cellSelection.parentSiteRow;
	const activeChildIndex = parentSiteRow.children.indexOf(activeSiteRow);
	
	if (activeChildIndex === -1)
		return true;
	
	const isActiveAtTop = activeChildIndex === cellSelection.startChildIndex;
	const isActiveAtBottom = activeChildIndex === cellSelection.endChildIndex;
	
	let newStartIndex = cellSelection.startChildIndex;
	let newEndIndex = cellSelection.endChildIndex;
	let newActiveSiteRow = activeSiteRow;
	let newParentSiteRow = parentSiteRow;
	
	let newActiveCellIndex = cellSelection.activeCellIndex;
	
	// Try to move to previous sibling
	if (activeChildIndex > 0) {
		const prevSibling = parentSiteRow.children[activeChildIndex - 1];
		if (isActiveAtTop) {
			newStartIndex = activeChildIndex - 1;
			newActiveSiteRow = prevSibling;
			newActiveCellIndex = activeChildIndex - 1;
		} else if (isActiveAtBottom) {
			newEndIndex = activeChildIndex - 1;
			newActiveSiteRow = prevSibling;
			newActiveCellIndex = activeChildIndex - 1;
		} else {
			newStartIndex = activeChildIndex - 1;
			newActiveSiteRow = prevSibling;
			newActiveCellIndex = activeChildIndex - 1;
		}
	} else {
		// No previous sibling, select parent instead
		const grandparent = parentSiteRow.parent;
		if (grandparent === SiteRow.end) return true;
		
		const parentIndex = grandparent.children.indexOf(parentSiteRow);
		if (parentIndex === -1) return true;
		
		newParentSiteRow = grandparent;
		newStartIndex = parentIndex;
		newEndIndex = parentIndex;
		newActiveSiteRow = parentSiteRow;
		newActiveCellIndex = parentIndex;
	}
	
	const newCellSelection = new CellBlock(
		newParentSiteRow,
		newStartIndex,
		newEndIndex,
		cellSelection.startColumnIndex,
		cellSelection.endColumnIndex,
		newActiveSiteRow,
		newActiveCellIndex
	);
	
	model.site.setCellBlock(newCellSelection);
	model.scene.updatedSelection();
	return true;
}

function handleShiftArrowDown(): boolean {
	const cellSelection = model.site.cellSelection;
	
	// If cellSelection is empty, initialize it to current row
	if (cellSelection instanceof CellTextSelection) {
		const currentRow = Editor.currentRow();
		initCellSelectionToRow(currentRow);
		return true;
		} else if (cellSelection instanceof CellBlock) {
		
		const activeSiteRow = cellSelection.activeSiteRow;
		const parentSiteRow = cellSelection.parentSiteRow;
		const activeChildIndex = parentSiteRow.children.indexOf(activeSiteRow);
		
		if (activeChildIndex === -1) return true;
		
		const isActiveAtTop = activeChildIndex === cellSelection.startChildIndex;
		const isActiveAtBottom = activeChildIndex === cellSelection.endChildIndex;
		
		let newStartIndex = cellSelection.startChildIndex;
		let newEndIndex = cellSelection.endChildIndex;
		let newActiveSiteRow = activeSiteRow;
		let newParentSiteRow = parentSiteRow;
		let newActiveCellIndex = cellSelection.activeCellIndex;
		
		// Try to move to next sibling
		if (activeChildIndex < parentSiteRow.children.length - 1) {
			const nextSibling = parentSiteRow.children[activeChildIndex + 1];
			if (isActiveAtBottom) {
				newEndIndex = activeChildIndex + 1;
				newActiveSiteRow = nextSibling;
				newActiveCellIndex = activeChildIndex + 1;
			} else if (isActiveAtTop) {
				newStartIndex = activeChildIndex + 1;
				newActiveSiteRow = nextSibling;
				newActiveCellIndex = activeChildIndex + 1;
			} else {
				newStartIndex = activeChildIndex + 1;
				newActiveSiteRow = nextSibling;
				newActiveCellIndex = activeChildIndex + 1;
			}
		} else {
			// No next sibling, select parent instead
			const grandparent = parentSiteRow.parent;
			if (grandparent === SiteRow.end) return true;
			
			const parentIndex = grandparent.children.indexOf(parentSiteRow);
			if (parentIndex === -1) return true;
			
			newParentSiteRow = grandparent;
			newStartIndex = parentIndex;
			newEndIndex = parentIndex;
			newActiveSiteRow = parentSiteRow;
		}
		
		const newCellSelection = new CellBlock(
			newParentSiteRow,
			newStartIndex,
			newEndIndex,
			cellSelection.startColumnIndex,
			cellSelection.endColumnIndex,
			newActiveSiteRow,
			newActiveCellIndex
		);
		
		model.site.setCellBlock(newCellSelection);
		model.scene.updatedSelection();
		return true;
	}
	return true;
}

function handleArrowLeft() : boolean {
	clearCellSelection();
	const currentRow = Editor.currentRow();
	const activeCell = currentRow.activeCell;
	if (!activeCell) return true;
	
	const caret = currentRow.caretOffset;
	const offset = caret?.offset ?? 0;
	
	if (offset === 0) {
		// Cursor at left end of cell, find previous editable cell
		const prevCell = findPreviousEditableCell(currentRow, activeCell);
		if (prevCell) {
			// Move to end of previous cell
			prevCell.setCaret(prevCell.visibleTextLength);
		}
	} else {
		// Move cursor left within current cell
		activeCell.setCaret(offset - 1);
	}
	return true;
}

function handleArrowRight() : boolean {
	clearCellSelection();
	const currentRow = Editor.currentRow();
	const activeCell = currentRow.activeCell;
	if (!activeCell) return true;
	
	const caret = currentRow.caretOffset;
	const offset = caret?.offset ?? 0;
	
	if (offset === activeCell.visibleTextLength) {
		// Cursor at right end of cell, find next editable cell
		const nextCell = findNextEditableCell(currentRow, activeCell);
		if (nextCell) {
			// Move to start (offset 0) of next cell
			nextCell.setCaret(0);
		}
	} else {
		// Move cursor right within current cell
		const cellIndex = currentRow.getCellIndex(activeCell);
		setCaretInRow(currentRow, cellIndex, offset + 1);
	}
	return true;
}

function handleHome() : boolean {
	const currentRow = Editor.currentRow();
	const cellIndex = currentRow.contentCells.length > 0 ? currentRow.getCellIndex(currentRow.contentCells[0]) : 0;
	setCaretInRow(currentRow, cellIndex, 0);
	return true;
}

function handleEnd() : boolean {
	const currentRow = Editor.currentRow();
	const lastCell = currentRow.contentCells[currentRow.contentCells.length - 1];
	const cellIndex = lastCell ? currentRow.getCellIndex(lastCell) : 0;
	setCaretInRow(currentRow, cellIndex, currentRow.visibleTextLength);
	return true;
}

function extendSelectionInCell(
	cell: Editor.Cell,
	cellLocalOffset: number
): void {
	// Delegate to cell's extendSelectionInCell method
	cell.extendSelectionInCell(cellLocalOffset);
}

function handleShiftArrowLeft() : boolean {
	const currentRow = Editor.currentRow();
	const activeCell = currentRow.activeCell;
	if (!activeCell) return true;
	
	const caret = currentRow.caretOffset;
	const offset = caret?.offset ?? 0;
	if (offset > 0) {
		extendSelectionInCell(activeCell, offset - 1);
	}
	return true;
}

function handleShiftArrowRight() : boolean {
	const currentRow = Editor.currentRow();
	const activeCell = currentRow.activeCell;
	if (!activeCell) return true;
	
	const caret = currentRow.caretOffset;
	const offset = caret?.offset ?? 0;
	const maxOffset = activeCell.visibleTextLength;
	if (offset < maxOffset) {
		extendSelectionInCell(activeCell, offset + 1);
	}
	return true;
}

function handleShiftHome() : boolean {
	const currentRow = Editor.currentRow();
	const activeCell = currentRow.activeCell;
	if (!activeCell) return true;
	
	extendSelectionInCell(activeCell, 0);
	return true;
}

function handleShiftEnd() : boolean {
	const currentRow = Editor.currentRow();
	const activeCell = currentRow.activeCell;
	if (!activeCell) return true;
	
	extendSelectionInCell(activeCell, activeCell.visibleTextLength);
	return true;
}

function handleShiftWordLeft() : boolean {
	const currentRow = Editor.currentRow();
	const activeCell = currentRow.activeCell;
	if (!activeCell) return true;
	
	const caret = currentRow.caretOffset;
	if (!caret) return true;
	
	// Work at cell level: use cell's visible text and cell-local offset
	const cellText = activeCell.visibleText;
	const cellLocalOffset = caret.offset;
	const newCellLocalOffset = findWordLeft(cellText, cellLocalOffset);
	
	if (newCellLocalOffset >= 0) {
		extendSelectionInCell(activeCell, newCellLocalOffset);
	} else {
		// At start, extend to start of cell
		extendSelectionInCell(activeCell, 0);
	}
	return true;
}

function handleShiftWordRight() : boolean {
	const currentRow = Editor.currentRow();
	const activeCell = currentRow.activeCell;
	if (!activeCell) return true;
	
	const caret = currentRow.caretOffset;
	if (!caret) return true;
	
	// Work at cell level: use cell's visible text and cell-local offset
	const cellText = activeCell.visibleText;
	const cellLocalOffset = caret.offset;
	const newCellLocalOffset = findWordRight(cellText, cellLocalOffset);
	
	if (newCellLocalOffset >= 0) {
		extendSelectionInCell(activeCell, newCellLocalOffset);
	} else {
		// At end, extend to end of cell
		extendSelectionInCell(activeCell, activeCell.visibleTextLength);
	}
	return true;
}

function findWordLeft(text: string, offset: number): number {
	// If at start, can't go left
	if (offset <= 0) return -1;
	
	// Check if we're in a word (alphanumeric or underscore)
	const isWordChar = (pos: number) => {
		if (pos < 0 || pos >= text.length) return false;
		const ch = text[pos];
		return /[a-zA-Z0-9_]/.test(ch);
	};
	
	// If we're in a word, move to start of current word
	if (isWordChar(offset - 1)) {
		let pos = offset - 1;
		while (pos > 0 && isWordChar(pos - 1)) {
			pos--;
		}
		return pos;
	}
	
	// We're not in a word, skip non-word characters
	let pos = offset - 1;
	while (pos > 0 && !isWordChar(pos - 1)) {
		pos--;
	}
	
	// Now find start of word
	if (pos > 0 && isWordChar(pos - 1)) {
		while (pos > 0 && isWordChar(pos - 1)) {
			pos--;
		}
		return pos;
	}
	
	return 0;
}

function findWordRight(text: string, offset: number): number {
	// If at end, can't go right
	if (offset >= text.length) return -1;
	
	// Check if we're in a word (alphanumeric or underscore)
	const isWordChar = (pos: number) => {
		if (pos < 0 || pos >= text.length) return false;
		const ch = text[pos];
		return /[a-zA-Z0-9_]/.test(ch);
	};
	
	// If we're in a word, move to end of current word
	if (isWordChar(offset)) {
		let pos = offset;
		while (pos < text.length && isWordChar(pos)) {
			pos++;
		}
		// Now skip to start of next word
		while (pos < text.length && !isWordChar(pos)) {
			pos++;
		}
		return pos;
	}
	
	// We're not in a word, skip to start of next word
	let pos = offset;
	while (pos < text.length && !isWordChar(pos)) {
		pos++;
	}
	return pos;
}

function handleWordLeft() : boolean {
	const currentRow = Editor.currentRow();
	const text = currentRow.htmlContent;
	const caret = currentRow.caretOffset;
	const offset = caret?.offset ?? 0;
	
	const newOffset = findWordLeft(text, offset);
	if (newOffset >= 0) {
		const cellIndex = caret?.cell ? currentRow.getCellIndex(caret.cell) : getActiveCellIndex(currentRow);
		setCaretInRow(currentRow, cellIndex, newOffset);
		return true;
	}
	
	// At start of row, move to end of previous row
	const prevRow = currentRow.previous;
	if (prevRow.valid()) {
		const prevText = prevRow.visibleText;
		const prevOffset = findWordLeft(prevText, prevText.length);
		if (prevOffset >= 0) {
			const cellIndex = getActiveCellIndex(prevRow);
			setCaretInRow(prevRow, cellIndex, prevOffset);
		} else {
			const lastCell = prevRow.contentCells[prevRow.contentCells.length - 1];
			const cellIndex = lastCell ? prevRow.getCellIndex(lastCell) : 0;
			setCaretInRow(prevRow, cellIndex, prevText.length);
		}
	}
	return true;
}

function handleWordRight() : boolean {
	const currentRow = Editor.currentRow();
	const text = currentRow.htmlContent;
	const caret = currentRow.caretOffset;
	const offset = caret?.offset ?? 0;
	
	const newOffset = findWordRight(text, offset);
	if (newOffset >= 0) {
		const cellIndex = caret?.cell ? currentRow.getCellIndex(caret.cell) : getActiveCellIndex(currentRow);
		setCaretInRow(currentRow, cellIndex, newOffset);
		return true;
	}
	
	// At end of row, move to start of next row
	const nextRow = currentRow.next;
	if (nextRow.valid()) {
		const nextText = nextRow.visibleText;
		const nextOffset = findWordRight(nextText, 0);
		if (nextOffset >= 0) {
			const cellIndex = getActiveCellIndex(nextRow);
			setCaretInRow(nextRow, cellIndex, nextOffset);
		} else {
			const firstCell = nextRow.contentCells[0];
			const cellIndex = firstCell ? nextRow.getCellIndex(firstCell) : 0;
			setCaretInRow(nextRow, cellIndex, 0);
		}
	}
	return true;
}

export function moveBefore(line: DocLine, targetBefore: DocLine): void {
	if (line == DocLine.end || targetBefore == DocLine.end) return;
	
	const change = new Change.MoveBefore(line, targetBefore);
    Doc.processChange(change);
}
export function moveBelow(line: DocLine, targetBelow: DocLine): void {
	if (line == DocLine.end || targetBelow == DocLine.end) return;
	
	const change = new Change.MoveBelow(line, targetBelow);
    Doc.processChange(change);
}
// moves up to the previous visible row.  doesn't pay attention to structure otherwise.
function handleSwapUp(): boolean {
	const currentRow = Editor.currentRow();
	const cellSelection = model.site.cellSelection;
	if (cellSelection instanceof CellBlock) {
		const parentSiteRow = cellSelection.parentSiteRow;
		const startChildIndex = cellSelection.startChildIndex;
		
		// Find previous sibling before block's start row
		if (startChildIndex === 0) {
			// No previous sibling, do nothing
			return false;
		}
		
		const prevSibling = parentSiteRow.children[startChildIndex - 1];
		const endRow = parentSiteRow.children[cellSelection.endChildIndex];
		
		// Find what comes after the end row to move before it
		const endRowNextSibling = endRow.docLine.nextSibling();
		if (endRowNextSibling === DocLine.end) {
			// End row is last, move previous row to be last child of parent
			moveBelow(prevSibling.docLine, parentSiteRow.docLine);
		} else {
			// Move previous row after block's end row (before the next sibling)
			moveBefore(prevSibling.docLine, endRowNextSibling);
		}
		
		// Update block definition - after moving prevSibling, indices shift:
		// startChildIndex becomes startChildIndex - 1 (since we removed one before)
		// endChildIndex stays the same (the moved row is now after it)
		const newCellSelection = new CellBlock(
			parentSiteRow,
			startChildIndex - 1,
			cellSelection.endChildIndex-1,
			cellSelection.startColumnIndex,
			cellSelection.endColumnIndex,
			cellSelection.activeSiteRow,
			cellSelection.activeCellIndex
		);
		model.site.setCellBlock(newCellSelection);
		model.scene.updatedSelection();
		return true;
	} else {
		const prevRow = currentRow.previous;
		if (!prevRow.valid()) 
			return false;
		
		const cur = model.scene.findRow(currentRow.id);
		const prev = model.scene.findRow(prevRow.id);
		
		const docCur = cur.siteRow.docLine;
		const docPrev = prev.siteRow.docLine;
		
		return performRowSwap(docCur, docPrev, currentRow.id);
	}
}

function handleSwapDown(): boolean {
	const cellSelection = model.site.cellSelection;
	if (cellSelection instanceof CellBlock) {
		const parentSiteRow = cellSelection.parentSiteRow;
		const endChildIndex = cellSelection.endChildIndex;
		
		// Find next sibling after block's end row
		if (endChildIndex >= parentSiteRow.children.length - 1) {
			// No next sibling, do nothing
			return false;
		}
		
		const nextSibling = parentSiteRow.children[endChildIndex + 1];
		const startRow = parentSiteRow.children[cellSelection.startChildIndex];
		
		// Move next row before block's start row
		moveBefore(nextSibling.docLine, startRow.docLine);
		
		// Update block definition - after moving nextSibling, indices shift:
		// startChildIndex stays the same (the moved row is now before it)
		// endChildIndex becomes endChildIndex + 1 (since we added one before)
		const newCellSelection = new CellBlock(
			parentSiteRow,
			cellSelection.startChildIndex+1,
			endChildIndex + 1,
			cellSelection.startColumnIndex,
			cellSelection.endColumnIndex,
			cellSelection.activeSiteRow,
			cellSelection.activeCellIndex
		);
		model.site.setCellBlock(newCellSelection);
		model.scene.updatedSelection();
		return true;
	} else {
		const currentRow = Editor.currentRow();
		const cur = model.scene.findRow(currentRow.id);
		
		// Skip over all descendants to find the next row that's not a child
		const descendantCount = cur.treeLength;
		let nextRow = currentRow;
		for (let i = 0; i < descendantCount; i++) {
			nextRow = nextRow.next;
			if (!nextRow.valid()) return false;
		}
		
		const next = model.scene.findRow(nextRow.id);
		
		const docCur = cur.siteRow.docLine;
		const docNext = next.siteRow.docLine;
		
		return performRowSwap(docNext, docCur, currentRow.id);
	}
}

function performRowSwap(
    lineToMove: DocLine, 
    lineBefore: DocLine, 
    currentRowId: string
): boolean {
    moveBefore(lineToMove, lineBefore);
    const row = Editor.findRow(currentRowId);
    const firstCell = row.contentCells[0];
    const cellIndex = firstCell ? row.getCellIndex(firstCell) : 0;
    setCaretInRow(row, cellIndex, 0);
    return true;
}
function handleToggleFold() : boolean {
	const currentRow = Editor.currentRow();
	const sceneRow = model.scene.findRow(currentRow.id);
	const siteRow = sceneRow.siteRow;
	siteRow.toggleFold();
	return true;
}
function currentSiteRow(): SiteRow {
	const currentRow = Editor.currentRow();
	const cellSelection = model.site.cellSelection;
	if (cellSelection instanceof CellBlock) {
		return cellSelection.activeSiteRow;
	} else if (cellSelection instanceof CellTextSelection) {
		return cellSelection.row;
	}
	return SiteRow.end;
}
function handleAddMarkup(tagName: string): boolean {
// current site selection
	const currentRow = Editor.currentRow();
	const cellSelection = model.site.cellSelection;
	if (cellSelection instanceof CellBlock) {
		return true;
	} else if (cellSelection instanceof CellTextSelection) {
		const htmlStart = cellSelection.focus;
		const htmlEnd = cellSelection.anchor;

	const currentRow = Editor.currentRow();
	const siteRow = currentSiteRow();
	if (siteRow === SiteRow.end) return false;
	
	const htmlContent = currentRow.htmlContent;
	
	const operations = HtmlUtil.computeTagToggleOperations(htmlContent, htmlStart, htmlEnd, tagName);
	if (operations.length === 0) return false;
	
	const cur = model.scene.findRow(currentRow.id);
	const docLine = cur.siteRow.docLine;
	
	for (const op of operations) {
		const change = Change.makeTextChange(docLine, op.offset, op.deleteLength, op.insertText);
		Doc.processChange(change);
	}
	
	const updatedRow = Editor.findRow(currentRow.id);
	updatedRow.setSelectionInRow(htmlStart, htmlEnd);
	
	return true;
	}
	return false;
}

function updateFoldIndicator(editorRow: Editor.Row) {
	const scene = model.scene;
	const sceneRow = scene.findRow(editorRow.id);
	
	if (!sceneRow.siteRow.hasChildren) {
		editorRow.setFoldIndicator(' ');
	} else if (sceneRow.siteRow.folded) {
		editorRow.setFoldIndicator('+');
	} else {
		editorRow.setFoldIndicator('-');
	}
}

function insertChar(currentRow : Editor.Row, ch : string) {
	const caret = currentRow.caretOffset;
	if (!caret) return;
	
	// Convert cell-local offset to row-level offset
	const rowLevelOffset = cellLocalToRowLevelOffset(currentRow, caret.cell, caret.offset);
	insertCharAtPosition(currentRow, rowLevelOffset, ch);
}

function insertCharAtPosition(currentRow : Editor.Row, visibleOffset : number, ch : string) {
	const cur = model.scene.findRow(currentRow.id);
	const scur = cur.siteRow;
	const htmlContent = currentRow.htmlContent;
	const htmlOffset = HtmlUtil.visibleOffsetToHtmlOffset(htmlContent, visibleOffset);
	const escapedCh = HtmlUtil.escapeHtml(ch);
	const change = Change.makeTextChange(scur.docLine, htmlOffset, 0, escapedCh);
	Doc.processChange(change);
	const updatedRow = Editor.findRow(currentRow.id);
	const caret = currentRow.caretOffset;
	const cellIndex = caret?.cell ? updatedRow.getCellIndex(caret.cell) : getActiveCellIndex(updatedRow);
	setCaretInRow(updatedRow, cellIndex, visibleOffset + 1);
}

function handleInsertChar(currentRow : Editor.Row, ch : string) {
	const caret = currentRow.caretOffset;
	if (!caret || caret.offset < 0) {
		return true;
	}
	
	// Check if there's a selection using active cell's selection range
	const activeCell = currentRow.activeCell;
	if (activeCell) {
		const selectionRange = activeCell.getHtmlSelectionRange();
		if (selectionRange.start !== selectionRange.end) {
			// Convert cell-local offsets to row-level offsets
			const rowLevelStart = cellLocalToRowLevelOffset(currentRow, activeCell, selectionRange.start);
			const rowLevelEnd = cellLocalToRowLevelOffset(currentRow, activeCell, selectionRange.end);
			// Delete the selection first, then insert
			const start = Math.min(rowLevelStart, rowLevelEnd);
			deleteRowRange(currentRow, rowLevelStart, rowLevelEnd);
			// After deletion, get the updated row and insert at the start position
			const updatedRow = Editor.findRow(currentRow.id);
			insertCharAtPosition(updatedRow, start, ch);
			return true;
		}
	}
	
	insertChar(currentRow, ch);
	return true;
}

function docLineFromRow(row: Editor.Row): DocLine {
	const cur = model.scene.findRow(row.id);
	return cur.siteRow.docLine;
}
function moveAfterParent(docLine : DocLine) {
	// Move after parent
	// Move before parent's sibling if any.
	const parent = docLine.parent;
	const nextSibling = parent.nextSibling();
	if (nextSibling !== DocLine.end) {
		moveBefore(docLine, nextSibling);
	} else {
		// else move to end of grandparent.
		const grandparent = parent.parent;
		if (grandparent !== DocLine.end) {
			moveBelow(docLine, grandparent);
		} else {
			return true;
		}
	}

}

function handleTab() : boolean {
	const cellSelection = model.site.cellSelection;
	if (cellSelection instanceof CellBlock) {
		const parentSiteRow = cellSelection.parentSiteRow;
		const startChildIndex = cellSelection.startChildIndex;
		const endChildIndex = cellSelection.endChildIndex;
		
		// Look for a row previous to start
		if (startChildIndex === 0) {
			// No previous row, return
			return false;
		}
		const previousRow = parentSiteRow.children[startChildIndex - 1];
		const topLevelRows: SiteRow[] = [];
		for (let i = startChildIndex; i <= endChildIndex; i++) {
			topLevelRows.push(parentSiteRow.children[i]);
		}
		for (const siteRow of topLevelRows) {
			moveBelow(siteRow.docLine, previousRow.docLine);
		}
		
		if (topLevelRows.length > 0) {
			const firstRow = topLevelRows[0];
			const lastRow = topLevelRows[topLevelRows.length - 1];
			const newStartIndex = previousRow.children.indexOf(firstRow);
			const newEndIndex = previousRow.children.indexOf(lastRow);
			
			if (newStartIndex !== -1 && newEndIndex !== -1) {
				// Update cellSelection with new parent and indices
				const newCellSelection = new CellBlock(
					previousRow,
					newStartIndex,
					newEndIndex,
					cellSelection.startColumnIndex,
					cellSelection.endColumnIndex,
					cellSelection.activeSiteRow,
					cellSelection.activeCellIndex
				);
				model.site.setCellBlock(newCellSelection);
				model.scene.updatedSelection();
				return true;
			}
		}
		return true;
	}
	const currentRow = Editor.currentRow();
	const caret = currentRow.caretOffset;
	if (!caret) return true;
	
	const rowLevelOffset = cellLocalToRowLevelOffset(currentRow, caret.cell, caret.offset);
	
	if (rowLevelOffset == 0) {
		const cur = model.scene.findRow(currentRow.id);
		const scur = cur.siteRow;
			const sprev = scur.previous;
		if (sprev === SiteRow.end)
			return true;
		moveBelow(scur.docLine, sprev.docLine);
		const replacementRow = Editor.findRow(currentRow.id)
		const cellIndex = caret.cell ? replacementRow.getCellIndex(caret.cell) : getActiveCellIndex(replacementRow);
		setCaretInRow(replacementRow, cellIndex, rowLevelOffset + 1);
		return true;
	}

	insertChar(currentRow, '\t');
	return true;
}
function handleShiftTab() : boolean {
	const cellSelection = model.site.cellSelection;
	if (cellSelection instanceof CellBlock)  {
		const parentSiteRow = cellSelection.parentSiteRow;
		const startChildIndex = cellSelection.startChildIndex;
		const endChildIndex = cellSelection.endChildIndex;
		
		// Get all top-level rows of block selection
		const topLevelRows: SiteRow[] = [];
		for (let i = startChildIndex; i <= endChildIndex; i++) {
			topLevelRows.push(parentSiteRow.children[i]);
		}
		
		// Move each row after its parent (iterate in reverse to maintain order)
		for (const siteRow of [...topLevelRows].reverse()) {
			moveAfterParent(siteRow.docLine);
		}
		
		// After moves, all rows will have the same new parent (grandparent)
		// Find the new parent and indices
		if (topLevelRows.length > 0) {
			const firstRow = topLevelRows[0];
			const newParent = firstRow.parent;
			if (newParent !== SiteRow.end) {
				// Find the new indices in the new parent's children
				const newStartIndex = newParent.children.indexOf(firstRow);
				const newEndIndex = newParent.children.
					indexOf(topLevelRows[topLevelRows.length - 1]);
				
				if (newStartIndex !== -1 && newEndIndex !== -1) {
					// Update block selection with new parent and indices
					const newCellSelection = new CellBlock(
						newParent,
						newStartIndex,
						newEndIndex,
						cellSelection.startColumnIndex,
						cellSelection.endColumnIndex,
						cellSelection.activeSiteRow,
						cellSelection.activeCellIndex
					);
					model.site.setCellBlock(newCellSelection);
					model.scene.updatedSelection();
				}
			}
		}
		return true;
	} 
	const currentRow = Editor.currentRow();
	const caret = currentRow.caretOffset;
    if (!caret) return true;
    
    // Convert cell-local offset to row-level offset
    const rowLevelOffset = cellLocalToRowLevelOffset(currentRow, caret.cell, caret.offset);

    // Get visible text for indent checking
	if (0 == rowLevelOffset) {
			if (currentRow.indent == 0)
				return true;
			const docLine = docLineFromRow(currentRow);
			moveAfterParent(docLine);
			const updatedRow = Editor.findRow(currentRow.id);
			const firstCell = updatedRow.contentCells[0];
			const cellIndex = firstCell ? updatedRow.getCellIndex(firstCell) : 0;
			setCaretInRow(updatedRow, cellIndex, 0);
	} else {
		const htmlOffset = currentRow.getCellLineOffset(caret.cell);
		if (htmlOffset === -1)
			return false;

		const docLine = docLineFromRow(currentRow);
		const change = Change.makeTextChange(docLine, htmlOffset-1, 1, '');
		Doc.processChange(change);
		const cellIndex = currentRow.getCellIndex(caret.cell);
		setCaretInRow(currentRow, cellIndex, rowLevelOffset + 1);
	}
	return true;
}

export function loadDoc(data: string, filePath: string): Doc {
	let doc = model.addOrUpdateDoc(data, filePath);
	model.scene.loadFromSite(model.site.root);
	SceneEditor.setEditorContent(new ArraySpan(
		model.scene.rows, 0, model.scene.rows.length));

	links();
	return doc;
}

export function save() {
	model.save();
}


// Methods to manage CellSelection selection
export function initCellSelectionToRow(initialRow: Editor.Row): void {
	const rowId = initialRow.id;
	const sceneRow = model.scene.findRow(rowId);
	const siteRow = sceneRow.siteRow;
	const parentSiteRow = siteRow.parent;
	const childIndex = parentSiteRow.children.indexOf(siteRow);
	if (siteRow === SiteRow.end) return;
	
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

export function getCellSelection(): CellSelection {
	return model.site.cellSelection;
}

export function clearCellSelection(): void {
	// find active row and set caret to it.
	if (model.site.cellSelection instanceof CellBlock) {
		model.site.clearCellBlock();
		model.scene.updatedSelection();
	}
}

export function setCaretInRow(row: Editor.Row, 
	cell: number,
	offset: number): void {
	const sceneRow = model.scene.findRow(row.id);
	const siteRow = sceneRow.siteRow;
	model.site.setSelection(new CellTextSelection(siteRow, siteRow.indent, offset, offset));
	model.scene.updatedSelection();
}