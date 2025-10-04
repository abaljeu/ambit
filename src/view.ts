import { PostDoc } from './ambit.js';
import * as lm from './elements.js';
import * as Editor from './editor.js';
import * as Scene from './scene.js';



export function setMessage(message : string) {
	lm.messageArea.innerHTML = message;
}

export function links() {
	// Get the content from the Editor div
	const textareaValue = getEditorContent();

	// Clear previous links
	lm.linksDiv.innerHTML = '';

	// Regular expression to find wikilinks
	const wikilinkRegex = /\[\[([a-zA-Z0-9 _\.-]+)\]\]/g;
	let match;
	let linksHTML = '';

	// Find all matches and generate links
	while ((match = wikilinkRegex.exec(textareaValue)) !== null) {
		const linkText = match[1]; // Get the text inside [[ ]]
		linksHTML += `<a href="ambit.php?doc=${encodeURIComponent(linkText) + '.amb'}">${linkText}</a><br>`;
	}

	// Inject the generated links into the links div
	lm.linksDiv.innerHTML = linksHTML;
}

export function editorInput() {
	// Sync all editor content back to scene
	syncAllRowsToScene();
	
	// Update all fold indicators
	updateAllFoldIndicators();
	
	// Update wikilinks
	links();
}

function syncAllRowsToScene() {
	const scene = Scene.data;
	for (const row of Editor.rows()) {
		const sceneRow = scene.findByLineId(row.id);
		scene.updateRowData(row.id, row.content);
	}
}

function updateAllFoldIndicators() {
	const scene = Scene.data;
	for (const row of Editor.rows()) {
		const sceneRow = scene.findByLineId(row.id);
		updateFoldIndicator(row, sceneRow);
	}
}

let lastCombo="";
export function editorKeyDown(e : KeyboardEvent) {
	if ( // just a mod key was pressed.
		e.key === "Control" ||
		e.key === "Shift" ||
		e.key === "Alt" ||
		e.key === "Meta"
	) return;
	const mods =
		`${e.ctrlKey ? "C-" : ""}` +
		`${e.altKey ? "A-" : ""}` +
		`${e.shiftKey ? "S-" : ""}` +
		`${e.metaKey ? "M-" : ""}`;
	const combo =  mods + e.key;

	// // Skip duplicates
	if (combo !== lastCombo) {
		console.log(combo);
		lastCombo = combo;
	}

	if (e.ctrlKey || e.metaKey || e.altKey || e.key.length > 1)
	{
		lm.messageArea.textContent = combo;
		const currentRow = Editor.CurrentRow();
		if (!currentRow.valid()) return;

		switch (combo) {
			case "F5": return;
			case "F6": return;
			case "Tab":
				handleTab(currentRow);
				 e.preventDefault();
				return;
			case "S-Tab":
				handleShiftTab(currentRow);
				e.preventDefault();
				return;
			case "C-s": 
				save();
				e.preventDefault();
				return;
			case "Enter":
				handleEnter(currentRow);
				e.preventDefault();
				return;
			case "Backspace":
				handleBackspace(currentRow);
				e.preventDefault();
				return;
			case "ArrowUp":
				handleArrowUp(currentRow);
				e.preventDefault();
				return;
			case "ArrowDown":
				handleArrowDown(currentRow);
				e.preventDefault();
				return;
			case "ArrowLeft":
				handleArrowLeft(currentRow);
				e.preventDefault();
				return;
			case "ArrowRight":
				handleArrowRight(currentRow);
				e.preventDefault();
				return;
			case "C-ArrowUp":
				handleSwapUp(currentRow);
				e.preventDefault();
				return;
			case "C-ArrowDown":
				handleSwapDown(currentRow);
				e.preventDefault();
				return;
			case "C-.":
				handleToggleFold(currentRow);
				e.preventDefault();
				return;
		}
		//e.preventDefault(); return;
	}
}

// Helper: Set cursor position in a paragraph
function setCursorInParagraph(p: HTMLElement, offset: number) {
	p.focus();
	const selection = window.getSelection();
	if (!selection) return;
	
	const range = document.createRange();
	const textNode = p.firstChild || p;
	range.setStart(textNode, offset);
	range.collapse(true);
	selection.removeAllRanges();
	selection.addRange(range);
}

function handleEnter(currentRow: Editor.Row) {
	// 1) Get the next row
	const nextRow = currentRow.Next;

	// split the current row at the cursor position
	const currentContent = currentRow.content;
	const currentRowNewContent = currentContent.substring(0, currentRow.offset); 
	const newRowContent = currentContent.substring(currentRow.offset);
	currentRow.setContent(currentRowNewContent);

	// 2) Update scene with the new contents (convert visible tabs to real tabs)
	const scene = Scene.data;
	const sceneRow : Scene.RowData = scene.findByLineId(currentRow.id);
	scene.updateRowData(currentRow.id, currentRowNewContent);
	const newSceneRow : Scene.RowData = scene.insertBefore(nextRow.id, newRowContent);
	
	// 3) Update editor with the new contents
	const newEditorRow = Editor.addBefore(nextRow, newSceneRow.id, newSceneRow.content);
	
	// 4) Update fold indicators for both rows
	updateFoldIndicator(currentRow, sceneRow);
	updateFoldIndicator(newEditorRow, newSceneRow);
	
	newEditorRow.setCaretInRow(0);
}

function handleBackspace(currentRow: Editor.Row) {
	const scene = Scene.data;
	
	if (currentRow.offset === 0) {
		const prevRow = currentRow.Previous;
		if (!prevRow.valid()) return;
		
		const prevRowOriginalLength = prevRow.content.length;
		// take the content of this row and add it to the previous row
		const newContent = prevRow.content + currentRow.content;
		prevRow.setContent(newContent);
		
		// Update scene
		scene.updateRowData(prevRow.id, newContent);
		scene.deleteRow(scene.findByLineId(currentRow.id));
		
		// Update fold indicators
		updateAllFoldIndicators();
		
		prevRow.setCaretInRow(prevRowOriginalLength);
		Editor.deleteRow(currentRow);
		return;
	}
	else { // we're not at the beginning of the row so just delete the character
		const newContent = currentRow.content.substring(0, currentRow.offset - 1) + 
			currentRow.content.substring(currentRow.offset);
		currentRow.setContent(newContent);
		
		// Update scene
		scene.updateRowData(currentRow.id, newContent);
		
		// Update fold indicators (indentation may have changed)
		updateAllFoldIndicators();
		
		currentRow.setCaretInRow(currentRow.offset - 1);
	}
}

function handleArrowUp(currentRow: Editor.Row) {
	const prevP = currentRow.Previous;
	if (!prevP.valid()) return;
	
	prevP.moveCaretToThisRow();
}

function handleArrowDown(currentRow: Editor.Row) {
	const nextP = currentRow.Next;
	if (!nextP.valid()) return;
	
	nextP.moveCaretToThisRow();
}

function handleArrowLeft(currentRow: Editor.Row) {
	if (currentRow.offset > 0) {
		// Move cursor left within current row
		currentRow.setCaretInRow(currentRow.offset - 1);
	} else {
		// Move to end of previous row
		const prevRow = currentRow.Previous;
		if (prevRow.valid()) {
			prevRow.setCaretInRow(prevRow.content.length);
		}
	}
}

function handleArrowRight(currentRow: Editor.Row) {
	if (currentRow.offset < currentRow.content.length) {
		// Move cursor right within current row
		currentRow.setCaretInRow(currentRow.offset + 1);
	} else {
		// Move to beginning of next row
		const nextRow = currentRow.Next;
		if (nextRow.valid()) {
			nextRow.setCaretInRow(0);
		}
	}
}

function handleSwapUp(currentRow: Editor.Row) {
	const prevP = currentRow.Previous;
	if (!prevP.valid()) return;
	
	Editor.moveRowAbove(currentRow, prevP);
	
	// Sync editor to scene
	syncAllRowsToScene();
	
	// Update fold indicators (indentation context may have changed)
	updateAllFoldIndicators();
	
	currentRow.focus();
}

function handleSwapDown(currentRow: Editor.Row) {
	const nextP = currentRow.Next;
	if (!nextP.valid()) return;
	
	Editor.moveRowAbove(nextP, currentRow);
	
	// Sync editor to scene
	syncAllRowsToScene();
	
	// Update fold indicators (indentation context may have changed)
	updateAllFoldIndicators();
	
	currentRow.focus();
}

function handleToggleFold(currentRow: Editor.Row) {
	const scene = Scene.data;
	const sceneRow = scene.findByLineId(currentRow.id);
	
	// Ask scene to calculate which rows should be toggled
	const affectedRows = scene.toggleFold(currentRow.id);
	
	// If no children to fold, do nothing
	if (affectedRows.length === 0) return;
	
	// Update fold indicator for current row
	updateFoldIndicator(currentRow, sceneRow);
	
	// Apply visibility changes
	if (sceneRow.folded) {
		// Folding - remove rows from DOM
		Editor.deleteAfter(currentRow, affectedRows.length);
	} else {
		// Unfolding - add rows back to DOM
		const addedEditorRows = Editor.addAfter(currentRow, affectedRows);
		
		// Update fold indicators for newly added rows
		for (let i = 0; i < addedEditorRows.length; i++) {
			updateFoldIndicator(addedEditorRows[i], affectedRows[i]);
		}
	}
	
	// Restore focus
	currentRow.focus();
}

function updateFoldIndicator(editorRow: Editor.Row, sceneRow: Scene.RowData) {
	const scene = Scene.data;
	const idx = scene.findIndexByLineId(sceneRow.id);
	const baseIndent = sceneRow.getIndentLevel();
	
	// Check if this row has any more-indented children
	let hasChildren = false;
	for (let i = idx + 1; i < scene.rows.length; i++) {
		const nextRow = scene.rows[i];
		const nextIndent = nextRow.getIndentLevel();
		
		if (nextIndent <= baseIndent) break;
		hasChildren = true;
		break;
	}
	
	if (!hasChildren) {
		editorRow.setFoldIndicator(' ');
	} else if (sceneRow.folded) {
		editorRow.setFoldIndicator('+');
	} else {
		editorRow.setFoldIndicator('-');
	}
}
function offsetIsInIndent(offset: number, rowText: string): boolean {
	const len = rowText.length;
	if (offset > len) return false;

	for (let i = 0; i < offset; i++) {
		if (rowText[i] != '\t') return false;
	}
	return true;
}

function rowDataFromEditorRow(editorRow: Editor.Row): Scene.RowData {
	return Scene.data.findByLineId(editorRow.id);
}

function handleTab(currentRow: Editor.Row) {
	const offset = currentRow.offset;
	let c = currentRow.content;
	if (offsetIsInIndent(offset, c)) {
		let rows : Scene.RowData[] = Scene.data.indentRowAndChildren(rowDataFromEditorRow(currentRow));
		Editor.updateRows(rows);
	} else {
        const newContent = c.substring(0, offset) + '\t' + c.substring(offset);
        Scene.data.updateRowData(currentRow.id, newContent);
        currentRow.setContent(newContent);
        currentRow.setCaretInRow(offset + 1);
	}
}

function handleShiftTab(currentRow: Editor.Row) {
    const offset = currentRow.offset;
    if (offsetIsInIndent(offset, currentRow.content)) {
        const rows = Scene.data.deindentRowAndChildren(rowDataFromEditorRow(currentRow));
        Editor.updateRows(rows);
    } else {
        // find tab to the left of the cursor
        const tabIndex = currentRow.content.substring(0, offset).lastIndexOf('\t');
        if (tabIndex === -1) return;
        const newContent = currentRow.content.substring(0, tabIndex)
            + currentRow.content.substring(tabIndex + 1);
        Scene.data.updateRowData(currentRow.id, newContent);
        currentRow.setContent(newContent);
        currentRow.setCaretInRow(tabIndex);
    }
}

export function save() {
	PostDoc(Editor.docName(), Editor.getContent());
}

export function setEditorContent(scene: Scene.Data) {
	Editor.setContent(scene.rows);
		// Update fold indicator
		updateAllFoldIndicators();
}

export function getEditorContent(): string {
	// Convert visible tabs back to regular tabs for saving
	return Editor.getContent()
}
