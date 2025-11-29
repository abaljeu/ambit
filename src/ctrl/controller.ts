import * as Editor from '../editor.js';
import * as SceneEditor from '../scene-editor.js';
import { ArraySpan } from '../arrayspan.js';
import { model } from '../model.js';
import { Doc, DocLine } from '../doc.js';
import { RowCell, Site, SiteRow, SiteRowId } from '../site.js';
import * as Change from '../change.js';
import { CellSelection, CellBlock, CellTextSelection, CellSpec, NoSelection } from '../cellblock.js';
import { SceneCell } from '../sitecells.js';

import * as WebUI from '../web/ui.js';
import * as Ops from '../ops.js';

export function setMessage(message: string): void {
	WebUI.setMessage(message);
}

// export function links() {
// 	// Get the content from the Editor div
// 	const textareaValue = Editor.getEditorContent();

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
// }

function updateAllFoldIndicators() {
	const scene = model.scene;
	for (const row of Editor.rows()) {
		updateFoldIndicator(row);
	}
}
class KeyBinding {
	constructor(
		readonly combo: string,
		readonly handler: (sel: CellTextSelection) => boolean
	) {}
}

class BlockKeyBinding {
	constructor(
		readonly combo: string,
		readonly handler: (sel: CellBlock) => boolean
	) {}
}

const blockKeyBindings: BlockKeyBinding[] = [
	new BlockKeyBinding("C-]", handleBlockZoomIn),
	new BlockKeyBinding("C-[", handleBlockZoomOut),
	new BlockKeyBinding("ArrowUp", handleBlockArrowUp),
	new BlockKeyBinding("ArrowDown", handleBlockArrowDown),
	new BlockKeyBinding("ArrowRight", handleBlockArrowRight),
	new BlockKeyBinding("ArrowLeft", handleBlockArrowLeft),
	new BlockKeyBinding("S-ArrowUp", handleBlockShiftArrowUp),
	new BlockKeyBinding("S-ArrowDown", handleBlockShiftArrowDown),
	new BlockKeyBinding("C-ArrowUp", handleBlockSwapUp),
	new BlockKeyBinding("C-ArrowDown", handleBlockSwapDown),
	new BlockKeyBinding("Tab", handleBlockTab),
	new BlockKeyBinding("S-Tab", handleBlockShiftTab),
	new BlockKeyBinding("C-.", handleBlockToggleFold),
	new BlockKeyBinding("F5",  () => false),
	new BlockKeyBinding("C-F5",  () => false),
];

const textKeyBindings: KeyBinding[] = [
	new KeyBinding("C-]", handleZoomIn),
	new KeyBinding("C-[", handleZoomOut),
	new KeyBinding("F12", (_)=>false),
	new KeyBinding("F5",  (_)=>false),
	new KeyBinding("C-F5",  () => false),
	new KeyBinding("Tab",  handleTab),
	new KeyBinding("S-Tab",  handleShiftTab),
	new KeyBinding("C-s",  (_)=>{ save(); return true; }),
	new KeyBinding("C-b",  (sel)=>handleAddMarkup(sel, "b")),
	new KeyBinding("Enter", handleEnter),
	new KeyBinding("Backspace", handleBackspace),
	new KeyBinding("Delete", handleDelete),
	new KeyBinding("ArrowUp", handleArrowUp),
	new KeyBinding("ArrowDown", handleArrowDown),
	new KeyBinding("ArrowLeft", handleArrowLeft),
	new KeyBinding("ArrowRight", handleArrowRight),
	new KeyBinding("S-ArrowLeft",  handleShiftArrowLeft),
	new KeyBinding("S-ArrowRight",  handleShiftArrowRight),
	new KeyBinding("S-ArrowUp", handleShiftArrowUp),
	new KeyBinding("S-ArrowDown", handleShiftArrowDown),
	new KeyBinding("Home",  handleHome),
	new KeyBinding("End", handleEnd),
	new KeyBinding("S-Home",  handleShiftHome),
	new KeyBinding("S-End",  handleShiftEnd),
	new KeyBinding("C-ArrowLeft", handleWordLeft),
	new KeyBinding("C-ArrowRight",  handleWordRight),
	new KeyBinding("C-S-ArrowLeft", handleShiftWordLeft),
	new KeyBinding("C-S-ArrowRight",  handleShiftWordRight),
	new KeyBinding("C-ArrowUp",  handleSwapUp),
	new KeyBinding("C-ArrowDown",  handleSwapDown),
	new KeyBinding("C-.",  handleToggleFold),
];

function findKeyBinding(e : KeyboardEvent): () => boolean {
	if ( // just a mod key was pressed.
		e.key === "Control" ||
		e.key === "Shift" ||
		e.key === "Alt" ||
		e.key === "Meta") 
		return () => false;
	const mods =
		`${e.ctrlKey ? "C-" : ""}` +
		`${e.altKey ? "A-" : ""}` +
		`${e.shiftKey ? "S-" : ""}` +
		`${e.metaKey ? "M-" : ""}`;
	const combo =  mods + e.key;
	WebUI.setMessage(combo);

	// // Update selection from browser if needed
	// const selection = Editor.currentSelection();
	// if (selection && selection instanceof PureTextSelection) {
	// 	const siteRow = model.site.findRow(selection.rowid);
	// 	model.site.setSelection(
	// 		new CellTextSelection(
	// 			siteRow, 
	// 			selection.cellIndex, 
	// 			selection.focus, 
	// 			selection.anchor));
	// }
	
	const cellSelection = model.site.cellSelection;
	if (cellSelection instanceof CellTextSelection) {
		let binding = textKeyBindings.find(kb => kb.combo === combo);
		if (binding) return (() => binding.handler(cellSelection));
		if (combo.length == 1) {
			return (() => handleInsertChar(cellSelection, combo));
		} else if (combo.length == 3 && combo[0] == 'S') {
			// shifted character.
			return (() => handleInsertChar(cellSelection, combo[2]));
		}
		return (() => true);
	} else if (cellSelection instanceof CellBlock) {
		let binding = blockKeyBindings.find(kb => kb.combo === combo);
		if (binding) return (() => binding.handler(cellSelection));
		return (() => true);
	}
	return (() => true);
}

 export function editorHandleKey(e : KeyboardEvent) {
	const binding = findKeyBinding(e);
	const result = binding();
	if (result === true) {
		e.preventDefault();
		updateAllFoldIndicators();
	}
 }


 
 function handleEnter(sel: CellTextSelection) : boolean {
	if (sel.focus !== sel.anchor) {
		Ops.replaceCellRange(sel.activeRowCell, sel.focus, sel.anchor, '');
		return true;
	}
	splitRow(sel);
	return true;
 }
 function splitRow(sel: CellTextSelection) {
	const docLine = sel.row.docLine;
	const beforeLength = sel.row.cellTextPosition(sel.activeRowCell.cell);
	const beforeText = docLine.content.substring(0, beforeLength + sel.focus);
	const afterText = docLine.content.substring(beforeLength + sel.focus);

	const newDocLine = Doc.createLine(beforeText);
	const insertBefore = Change.makeInsertBefore(docLine, [newDocLine]);
	Doc.processChange(insertBefore);
	const textChange = Change.makeLineTextChange(sel.row, afterText);
	Doc.processChange(textChange);
	Ops.setCaretInCell(sel.activeRowCell, 0, 0);
 }

 // removes prevRow.
function joinRows(prevRow: SiteRow, nextRow: SiteRow) {
	if (!prevRow.valid || !nextRow.valid) return;
	const prevDocLine = prevRow.docLine;
	const nextDocLine = nextRow.docLine;

	if (prevDocLine.children.length > 0) {
		return;
	}

	const concatenated = prevDocLine.content + nextDocLine.content;
	const change = Change.makeLineTextChange(nextRow, concatenated);
	Doc.processChange(change);
	Doc.processChange(new Change.Remove([prevDocLine]));
	updateAllFoldIndicators();
}
function joinCells(row : SiteRow, cell: SceneCell) {
	const pos = row.cellTextPosition(cell);
	const newText = row.docLine.content.substring(0, pos-1) + row.docLine.content.substring(pos);
	const change = Change.makeLineTextChange(row, newText);
	Doc.processChange(change);
}
function handleBackspace(sel: CellTextSelection) : boolean {
	// Check if there's a selection using active cell's selection range
	if (sel.focus !== sel.anchor) {
		Ops.replaceCellRange(sel.activeRowCell, sel.focus, sel.anchor, '');
		return true;
	}
	if (sel.focus > 0) { // delete selected text.
		Ops.replaceCellRange(sel.activeRowCell, sel.focus-1, sel.focus, '');
		return true;
	}
	 if (sel.cellIndex <= sel.row.indent) { // join row to previous
		const prev = sel.row.previous;
		const prevLastCellIndex = prev.cells.count - 1;
		const prevLastCell = prev.cells.at(prevLastCellIndex);
		if (!prevLastCell) return true;
		const prevLastCellLength = prevLastCell.text.length;
		joinRows(sel.row.previous, sel.row)
		Ops.setCaretInCell(sel.activeRowCell, prevLastCellIndex, prevLastCellLength);
		return true;
	}
	// join cell to previous  
	joinCells(sel.row, sel.activeRowCell.cell);
	return true;
}

function handleDelete(sel: CellTextSelection) : boolean {
	if (sel.focus !== sel.anchor) {
		// Delete the selection
		Ops.deleteCellRange(sel.activeRowCell, sel.focus, sel.anchor);
		return true;
	}
	const activeCell = sel.activeRowCell.cell;
	if (sel.focus < activeCell.text.length) {
		Ops.deleteCellRange(sel.activeRowCell, sel.focus, sel.focus+1);
		return true;
	}
	if (activeCell == sel.row.cells.at(sel.row.cells.count - 1)) {
		joinRows(sel.row, sel.row.next);
		return true;
	}
	const nextCell = sel.row.cells.at(sel.cellIndex + 1);
	if (!nextCell) return true;
	joinCells(sel.row, nextCell);
	return true;
}

function handleBlockArrowUp(block: CellBlock) : boolean {
	const activeSiteRow = block.focusRow;
	const parentSiteRow = block.parentSiteRow;
	const activeChildIndex = parentSiteRow.children.indexOf(activeSiteRow);
	
	if (activeChildIndex === -1)
		return true;
	
	const isActiveAtStart = activeChildIndex === block.startRowIndex;
	const startChildIndex = block.startRowIndex;
	const isStartAtFirst = startChildIndex <= 0;
	
	let newStartIndex = block.startRowIndex;
	let newEndIndex = block.endRowIndex;
	let newActiveSiteRow = activeSiteRow;
	let newActiveCellIndex = block.focusCellIndex;
	
	if (!isActiveAtStart) {
		// Set active cell to start row
		const startRow = parentSiteRow.children[startChildIndex];
		newStartIndex = block.endRowIndex;
		newEndIndex = block.startRowIndex;
	} else if (!isStartAtFirst) {
		// Shift the selection (start and end) up one
		newStartIndex = block.startRowIndex - 1;
		newEndIndex = block.endRowIndex - 1;
		const newStartRow = parentSiteRow.children[newStartIndex];
		newActiveSiteRow = newStartRow;
		newActiveCellIndex = newStartIndex;
	} else {
		// Do nothing
		return true;
	}
	
	const newCellSelection = new CellBlock(parentSiteRow, newStartIndex, newEndIndex,
		newActiveCellIndex, -1);
	
	model.site.setCellBlock(newCellSelection);
	model.scene.updatedSelection();
	return true;
}

function handleArrowUp(sel: CellTextSelection) : boolean {
	const prevRow = model.scene.rowUp(sel.row);
	if (!prevRow.valid)
		return true;
	Ops.moveCursorToRow(prevRow);
	return true;
}

 function handleArrowDown(sel: CellTextSelection) : boolean {
	const nextRow = model.scene.rowDown(sel.row);
	if (!nextRow.valid)
		return true;
	Ops.moveCursorToRow(nextRow);
	return true;
}

function handleBlockArrowDown(block: CellBlock) : boolean {
		const activeSiteRow = block.focusRow;
		const parentSiteRow = block.parentSiteRow;
		const activeChildIndex = parentSiteRow.children.indexOf(activeSiteRow);
		
		if (activeChildIndex === -1)
			return true;
		
		const isActiveAtStart = activeChildIndex === block.startRowIndex;
		const isActiveAtEnd = activeChildIndex === block.endRowIndex;
		const endChildIndex = block.endRowIndex;
		const isEndAtLast = endChildIndex >= parentSiteRow.children.length - 1;
		
		let newStartIndex = block.startRowIndex;
		let newEndIndex = block.endRowIndex;
		let newActiveSiteRow = activeSiteRow;
		let newActiveCellIndex = block.focusCellIndex;
		
		if (!isActiveAtEnd) {
			// Set active cell to end row
			newStartIndex = block.startRowIndex;
			newEndIndex = block.endRowIndex;
		} else if (!isEndAtLast) {
			// Shift the selection (start and end) down one
			newStartIndex = block.startRowIndex + 1;
			newEndIndex = block.endRowIndex + 1;
			const newEndRow = parentSiteRow.children[newEndIndex];
			newActiveSiteRow = newEndRow;
			newActiveCellIndex = newEndIndex;
		} else {
			// Do nothing
			return true;
		}
		
		const newCellSelection = new CellBlock(parentSiteRow, newStartIndex, newEndIndex,
			newActiveCellIndex, -1);
			
		model.site.setCellBlock(newCellSelection);
		model.scene.updatedSelection();
		return true;
}


function handleBlockShiftArrowUp(block: CellBlock): boolean {
	const activeSiteRow = block.focusRow;
	const parentSiteRow = block.parentSiteRow;
	const activeChildIndex = parentSiteRow.children.indexOf(activeSiteRow);
	
	if (activeChildIndex === -1)
		return true;
	
	const isActiveAtTop = activeChildIndex === block.startRowIndex;
	const isActiveAtBottom = activeChildIndex === block.endRowIndex;
	
	let newStartIndex = block.startRowIndex;
	let newEndIndex = block.endRowIndex;
	let newActiveSiteRow = activeSiteRow;
	let newParentSiteRow = parentSiteRow;
	
	let newActiveCellIndex = block.focusCellIndex;
	const activeCellIndex = block.focusCellIndex;
	// Try to move to previous sibling
	if (activeChildIndex > 0) {
		const prevSibling = parentSiteRow.children[activeChildIndex - 1];
		if (isActiveAtTop) {
			newStartIndex = activeChildIndex - 1;
			newActiveSiteRow = prevSibling;
			newActiveCellIndex = activeCellIndex;
		} else if (isActiveAtBottom) {
			newEndIndex = activeChildIndex - 1;
			newActiveSiteRow = prevSibling;
			newActiveCellIndex = activeCellIndex;
		} else {
			newStartIndex = activeChildIndex - 1;
			newActiveSiteRow = prevSibling;
			newActiveCellIndex = activeCellIndex;
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
	
	const newCellSelection = CellBlock.create(newParentSiteRow, newStartIndex, newEndIndex);
	
	model.site.setCellBlock(newCellSelection);
	model.scene.updatedSelection();
	return true;
}

function handleShiftArrowUp(sel: CellTextSelection): boolean {
	Ops.selectRow(sel.row);
	return true;
}



function handleBlockShiftArrowDown(block: CellBlock): boolean {
	const focusRow = block.focusRow;
	const parentRow = block.parentSiteRow;
	const focusChildIndex = parentRow.children.indexOf(focusRow);
	
	if (focusChildIndex === -1) return true;
	
	const isActiveAtTop = focusChildIndex === block.startRowIndex;
	const isActiveAtBottom = focusChildIndex === block.endRowIndex;
	
	let newParentRow = parentRow;
	let newFocus = block.focusRowIndex;
	let newAnchor = block.anchorRowIndex;
	let newFocusCellIndex = block.focusCellIndex;
	const focusCellIndex = block.focusCellIndex;

		// Try to move to next sibling
		if (focusChildIndex < parentRow.children.length - 1) {
			const nextSibling = parentRow.children[focusChildIndex + 1];
			if (isActiveAtBottom) {
				newFocus = focusChildIndex + 1;
				newFocusCellIndex = focusCellIndex;
			} else if (isActiveAtTop) {
				newFocus = focusChildIndex + 1;
				newFocusCellIndex = focusCellIndex;
			} else {
				newFocus = focusChildIndex + 1;
				newFocusCellIndex = focusCellIndex;
			}
		} else {
			// No next sibling, select parent instead
			const grandparent = parentRow.parent;
			if (grandparent === SiteRow.end) return true;
			
			const parentIndex = grandparent.children.indexOf(parentRow);
			if (parentIndex === -1) return true;
			
			newParentRow = grandparent;
		}
		
	const newCellSelection = CellBlock.create( newParentRow, newFocus, newAnchor);
	
	model.site.setCellBlock(newCellSelection);
	model.scene.updatedSelection();
	return true;
}

function handleShiftArrowDown(sel: CellTextSelection): boolean {
	Ops.selectRow(sel.row);
	return true;
}

function handleArrowLeft(sel: CellTextSelection) : boolean {
	if (sel.focus === 0) {
		// Cursor at left end of cell, find previous editable cell

		const prevCell = Ops.findPreviousEditableCell(sel.row,sel.activeRowCell.cell);
		if (prevCell) {
			Ops.setCaretInCell(prevCell, prevCell.cell.text.length);
		}
	} else {
		Ops.setCaretInCell(sel.activeRowCell, sel.focus-1);
	}
	return true;
}

function handleBlockArrowRight(sel: CellBlock) : boolean {
	const s = new CellTextSelection(sel.focusRow, sel.focusCellIndex, 0,0);
	Ops.setCaretInCell(s.activeRowCell, 0);
	return true;
}
function handleBlockArrowLeft(sel: CellBlock) : boolean {
	const s = new CellTextSelection(sel.focusRow, sel.focusCellIndex, sel.focusCellIndex, sel.focusCellIndex);
	Ops.setCaretInCell(s.activeRowCell, 0);
	return true;
}
function handleArrowRight(sel: CellTextSelection) : boolean {
	if (sel.focus === sel.activeRowCell.cell.text.length) {
		// Cursor at right end of cell, find next editable cell
		const nextCell = Ops.findNextEditableCell(sel.row,sel.activeRowCell.cell);
		if (nextCell) {
			Ops.setCaretInCell(nextCell, 0);
		}
	} else {
		Ops.setCaretInCell(sel.activeRowCell, sel.focus+1);
	}
	return true;
}

function handleHome(sel: CellTextSelection) : boolean {
	if (sel.focus === 0 && sel.anchor === 0) {
		const nextCell = sel.row.cells.at(sel.row.indent);
		if (nextCell) {
			Ops.setCaretInCell(new RowCell(sel.row, nextCell), 0);
		}
	}
	else 
		Ops.setCaretInCell(sel.activeRowCell, 0);
	return true;
}

function handleEnd(sel: CellTextSelection) : boolean {
	const activeCell = sel.activeRowCell.cell;
	if (sel.focus === activeCell.text.length && sel.anchor === activeCell.text.length) {
		const nextCell = sel.row.cells.at(sel.row.cells.count - 1);
		if (nextCell) {
			Ops.setCaretInCell(new RowCell(sel.row, nextCell), nextCell.text.length);
		}
	}
	else 
		Ops.setCaretInCell(sel.activeRowCell, activeCell.text.length);
	return true;
}

function handleShiftArrowLeft(sel: CellTextSelection) : boolean {
	if (sel.focus > 0) {
		Ops.extendSelection(sel.activeRowCell, sel.focus - 1);
	}
	return true;
}
function handleZoomIn(sel: CellTextSelection) : boolean {
	if (model.site.zoomIn()) {
		SceneEditor.setEditorContent(new ArraySpan(
			model.scene.rows,0, model.scene.rows.length));
		Ops.setCaretInCell(sel.activeRowCell, sel.focus);
	}
	return true;
}
function handleBlockZoomIn(block: CellBlock) : boolean {
	return handleZoomIn(textSelectionFromBlock(block));
}
function handleZoomOut(sel: CellTextSelection) : boolean {
	if (model.site.zoomOut()) {
		SceneEditor.setEditorContent(new ArraySpan(
			model.scene.rows,0, model.scene.rows.length));
		Ops.setCaretInCell(sel.activeRowCell, sel.focus);
	}
	return true;
}

function handleBlockZoomOut(block: CellBlock) : boolean {
	return handleZoomOut(textSelectionFromBlock(block));
}
function textSelectionFromBlock(block: CellBlock): CellTextSelection {
	return new CellTextSelection(block.focusRow, block.focusCellIndex, 0,0);
}
function handleShiftArrowRight(sel: CellTextSelection) : boolean {
	const maxOffset = sel.activeRowCell.cell.text.length;
	if (sel.focus < maxOffset) {
		Ops.extendSelection(sel.activeRowCell, sel.focus + 1);
	}
	return true;
}

function handleShiftHome(sel: CellTextSelection) : boolean {
	Ops.extendSelection(sel.activeRowCell, 0);
	return true;
}

function handleShiftEnd(sel: CellTextSelection) : boolean {
	Ops.extendSelection(sel.activeRowCell, sel.activeRowCell.cell.text.length);
	return true;
}

function handleShiftWordLeft(sel: CellTextSelection) : boolean {
	const cellText = sel.activeRowCell.cell.text;
	const newCellLocalOffset = findWordLeft(cellText, sel.focus);
	
	if (newCellLocalOffset >= 0) {
		Ops.extendSelection(sel.activeRowCell, newCellLocalOffset);
	} else {
		Ops.extendSelection(sel.activeRowCell, 0);
	}
	return true;
}

function handleShiftWordRight(sel: CellTextSelection) : boolean {
	const cellText = sel.activeRowCell.cell.text;
	const newCellLocalOffset = findWordRight(cellText, sel.focus);
	
	if (newCellLocalOffset >= 0) {
		Ops.extendSelection(sel.activeRowCell, newCellLocalOffset);
	} else {
		Ops.extendSelection(sel.activeRowCell, cellText.length);
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

function handleWordLeft(sel: CellTextSelection) : boolean {
	const text = sel.activeRowCell.cell.text;
	const newOffset = findWordLeft(text, sel.focus);
	
	if (newOffset >= 0) {
		Ops.setCaretInCell(sel.activeRowCell, newOffset);
		return true;
	}
	
	// At start of cell, move to previous cell
	const prevCell = Ops.findPreviousEditableCell(sel.row, sel.activeRowCell.cell);
	if (prevCell) {
		const prevText = prevCell.cell.text;
		const prevOffset = findWordLeft(prevText, prevText.length);
		if (prevOffset >= 0) {
			Ops.setCaretInCell(prevCell, prevOffset);
		} else {
			Ops.setCaretInCell(prevCell, prevText.length);
		}
	}
	return true;
}

function handleWordRight(sel: CellTextSelection) : boolean {
	const text = sel.activeRowCell.cell.text;
	const newOffset = findWordRight(text, sel.focus);
	
	if (newOffset >= 0) {
		Ops.setCaretInCell(sel.activeRowCell, newOffset);
		return true;
	}
	
	// At end of cell, move to next cell
	const nextCell = Ops.findNextEditableCell(sel.row, sel.activeRowCell.cell);
	if (nextCell) {
		const nextText = nextCell.cell.text;
		const nextOffset = findWordRight(nextText, 0);
		if (nextOffset >= 0) {
			Ops.setCaretInCell(nextCell, nextOffset);
		} else {
			Ops.setCaretInCell(nextCell, 0);
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
function handleBlockSwapUp(block: CellBlock): boolean {
	const parentSiteRow = block.parentSiteRow;
	const startChildIndex = block.startRowIndex;
	
	// Find previous sibling before block's start row
	if (startChildIndex === 0) {
		return false;
	}
	
	const prevSibling = parentSiteRow.children[startChildIndex - 1];
	const endRow = parentSiteRow.children[block.endRowIndex];
	
	// Find what comes after the end row to move before it
	const endRowNextSibling = endRow.docLine.nextSibling();
	if (endRowNextSibling === DocLine.end) {
		moveBelow(prevSibling.docLine, parentSiteRow.docLine);
	} else {
		moveBefore(prevSibling.docLine, endRowNextSibling);
	}
	
	const newCellSelection = CellBlock.create(parentSiteRow, startChildIndex - 1, block.endRowIndex-1);
	model.site.setCellBlock(newCellSelection);
	model.scene.updatedSelection();
	return true;
}

function handleSwapUp(sel: CellTextSelection): boolean {
	const thisIndex = sel.row.parent.children.indexOf(sel.row);
	const block = CellBlock.create(sel.row.parent, thisIndex, thisIndex);
	handleBlockSwapUp(block);
	// const prevRow = sel.row.previous;
	// if (!prevRow.valid) 
	// 	return false;
	
	// const docCur = sel.row.docLine;
	// const docPrev = prevRow.docLine;
	
	// moveBefore(docCur, docPrev);
	Ops.setCaretInCell(sel.activeRowCell, 0);
	return true;
}

function handleBlockSwapDown(block: CellBlock): boolean {
	const parentSiteRow = block.parentSiteRow;
	const endChildIndex = block.endRowIndex;
	
	// Find next sibling after block's end row
	if (endChildIndex >= parentSiteRow.children.length - 1) {
		return false;
	}
	
	const nextSibling = parentSiteRow.children[endChildIndex + 1];
	const startRow = parentSiteRow.children[block.startRowIndex];
	
	// Move next row before block's start row
	moveBefore(nextSibling.docLine, startRow.docLine);
	
	const newCellSelection = CellBlock.create( parentSiteRow, block.startRowIndex+1, endChildIndex + 1);
	model.site.setCellBlock(newCellSelection);
	model.scene.updatedSelection();
	return true;
}

function handleSwapDown(sel: CellTextSelection): boolean {
	const thisIndex = sel.row.parent.children.indexOf(sel.row);
	const block = CellBlock.create(sel.row.parent, thisIndex, thisIndex);
	handleBlockSwapDown(block);

// 	const cur = sel.row;
	
// 	// Skip over all descendants to find the next row that's not a child
// 	const descendantCount = cur.treeLength;
// 	let nextRow = sel.row;
// 	for (let i = 0; i < descendantCount; i++) {
// 		nextRow = nextRow.next;
// 		if (!nextRow.valid) return false;
// 	}
	
// 	const docCur = cur.docLine;
// 	const docNext = nextRow.docLine;
	
// 	moveBefore(docNext, docCur);
	Ops.setCaretInCell(sel.activeRowCell, 0);
	return true;
}

function handleToggleFold(sel: CellTextSelection) : boolean {
	sel.row.toggleFold();
	return true;
}
function handleBlockToggleFold(block: CellBlock) : boolean {
	for (const siteRow of block.rows()) {
		siteRow.toggleFold();
	}
	return true;
}
function handleAddMarkup(sel: CellTextSelection, tagName: string): boolean {
// current site selection
	// const currentRow = Editor.currentRow();
	// const cellSelection = model.site.cellSelection;
	// if (cellSelection instanceof CellBlock) {
	// 	return true;
	// } else if (cellSelection instanceof CellTextSelection) {
	// 	const htmlStart = cellSelection.focus;
	// 	const htmlEnd = cellSelection.anchor;

	// const currentRow = Editor.currentRow();
	// const siteRow = currentSiteRow();
	// if (siteRow === SiteRow.end) return false;
	
	// const htmlContent = currentRow.htmlContent;
	
	// const operations = HtmlUtil.computeTagToggleOperations(htmlContent, htmlStart, htmlEnd, tagName);
	// if (operations.length === 0) return false;
	
	// const cur = model.scene.findRow(currentRow.id);
	// const docLine = cur.siteRow.docLine;
	
	// for (const op of operations) {
	// 	const change = Change.makeTextChange(siteRowFromRow(currentRow), op.offset, op.deleteLength, op.insertText);
	// 	Doc.processChange(change);
	// }
	
	// const updatedRow = Editor.findRow(currentRow.id);
	// updatedRow.setSelectionInRow(htmlStart, htmlEnd);
	
	// return true;
	// }
	return false;
}

function updateFoldIndicator(editorRow: Editor.Row) {
	const siteRow = model.site.findRow(new SiteRowId(editorRow.id));
	
	if (!siteRow.hasChildren) {
		editorRow.setFoldIndicator(' ');
	} else if (siteRow.folded) {
		editorRow.setFoldIndicator('+');
	} else {
		editorRow.setFoldIndicator('-');
	}
}

function handleInsertChar(sel: CellTextSelection, ch : string) {
	if (sel.focus !== sel.anchor) {
		// Replace selection with character
		Ops.replaceCellRange(sel.activeRowCell, sel.focus, sel.anchor, ch);
		return true;
	}
	
	// Insert character at focus position
	Ops.replaceCellRange(sel.activeRowCell, sel.focus, sel.focus, ch);
	return true;
}

function handleBlockTab(block: CellBlock): boolean {
	const parentSiteRow = block.parentSiteRow;
	const startChildIndex = block.startRowIndex;
	const endChildIndex = block.endRowIndex;
	
	// Look for a row previous to start
	if (startChildIndex === 0) {
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
			const newCellSelection = CellBlock.create( previousRow, newStartIndex, newEndIndex);
			model.site.setCellBlock(newCellSelection);
			model.scene.updatedSelection();
			return true;
		}
	}
	return true;
}

function handleTab(sel: CellTextSelection) : boolean {
	const cellTextPos = sel.row.cellTextPosition(sel.activeRowCell.cell);
	
	if (cellTextPos + sel.focus == 0) { // beginning of line
		const sprev = sel.row.previous;
		if (sprev === SiteRow.end)
			return true;
		moveBelow(sel.row.docLine, sprev.docLine);
		Ops.setCaretInCell(sel.activeRowCell, sel.focus + 1);
		return true;
	}
	// start of cell
	if (sel.focus == 0) {
		return true;
	}
	// end of cell // if (sel.focus == sel.activeRowCell.cell.text.length) 
	// or middle of cell
	else {
		const oldContent = sel.activeRowCell.row.content;
		const offset = sel.activeRowCell.cellTextPosition() + sel.focus;
		const newContent = oldContent.substring(0, offset) + '\t' + oldContent.substring(offset);
		const change = Change.makeLineTextChange(sel.row, newContent);
		Doc.processChange(change);

		const cellIndex = sel.activeRowCell.cellIndex + 1;
		const nextCell = sel.row.cells.at(cellIndex);
		if (nextCell) {
			Ops.setCaretInCell(new RowCell(sel.row, nextCell), 0);
		}
		return true;
	}
}
function moveAfterParent(docLine : DocLine) {
	const parent = docLine.parent;
	const nextSibling = parent.nextSibling();
	if (nextSibling !== DocLine.end) {
		moveBefore(docLine, nextSibling);
	} else {
		const grandparent = parent.parent;
		if (grandparent !== DocLine.end) {
			moveBelow(docLine, grandparent);
		} else {
			return true;
		}
	}
}

function handleBlockShiftTab(block: CellBlock): boolean {
	const parentSiteRow = block.parentSiteRow;
	const startChildIndex = block.startRowIndex;
	const endChildIndex = block.endRowIndex;
	
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
	if (topLevelRows.length > 0) {
		const firstRow = topLevelRows[0];
		const newParent = firstRow.parent;
		if (newParent !== SiteRow.end) {
			const newStartIndex = newParent.children.indexOf(firstRow);
			const newEndIndex = newParent.children.
				indexOf(topLevelRows[topLevelRows.length - 1]);
			
			if (newStartIndex !== -1 && newEndIndex !== -1) {
				const newCellSelection = CellBlock.create(
					newParent,
					newStartIndex,
					newEndIndex
				);
				model.site.setCellBlock(newCellSelection);
				model.scene.updatedSelection();
			}
		}
	}
	return true;
}


function handleShiftTab(sel: CellTextSelection) : boolean {
	const cellTextPos = sel.row.cellTextPosition(sel.activeRowCell.cell);
	
	if (cellTextPos + sel.focus == 0) {
		if (sel.row.indent == 0)
			return true;
		moveAfterParent(sel.row.docLine);
		Ops.setCaretInCell(sel.activeRowCell, 0);
		return true;
	}
	
	// Delete tab character before cursor if there is one
	const textBefore = sel.activeRowCell.cell.text.substring(0, sel.focus);
	if (textBefore.endsWith('\t')) {
		Ops.replaceCellRange(sel.activeRowCell, sel.focus - 1, sel.focus, '');
	}
	return true;
}

export function loadDoc(data: string, filePath: string): Doc {
	let doc = model.addOrUpdateDoc(data, filePath);
	model.scene.loadFromSite(model.site.root);
	SceneEditor.setEditorContent(new ArraySpan(
		model.scene.rows,0, model.scene.rows.length));

	// links();
	return doc;
}

export function save() {
	model.save();
}


export function moveCursor(offset: number, anchor: number) {
	const cellSpec = model.activeCell;
	if (!cellSpec) 
		return;
	Ops.setCaret(cellSpec, offset, anchor);
}

export function moveCursorToCell(cell: Editor.Cell, offset: number) {
	const anchor = offset;
	if (!cell) 
		return;
	const editorRow = cell.Row;
	const index = editorRow.getCellIndex(cell);
	const siteRow = model.site.findRow(new SiteRowId(cell.Row.id));
	const cellSpec = new CellSpec(siteRow, index);
	Ops.setCaret(cellSpec, offset, anchor);
}