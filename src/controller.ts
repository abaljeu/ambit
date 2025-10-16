import { postDoc } from './ambit.js';
import * as lm from './elements.js';
import * as Editor from './editor.js';
import { Scene, SceneRow } from './scene.js';
import { ArraySpan } from './arrayspan.js';
import { model } from './model.js';
import { Doc } from './doc.js';


 export function setMessage(message : string) {
// 	lm.messageArea.innerHTML = message;
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

 export function editorInput() {
// 	// Sync all editor content back to scene
// 	syncAllRowsToScene();
	
// 	// Update all fold indicators
// 	updateAllFoldIndicators();
	
// 	// Update wikilinks
// 	links();
 }

// function syncAllRowsToScene() {
// 	const scene = Scene;
// 	for (const row of Editor.rows()) {
// 		const sceneRow = scene.findByLineId(row.id);
// 		scene.updateRowData(row.id, row.content);
// 	}
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
		readonly handler: (row: Editor.Row) => boolean | void
	) {}
}

const keyBindings: KeyBinding[] = [
	// new KeyBinding("F5", () => false),
	// new KeyBinding("F6", () => false),
	// new KeyBinding("Tab", handleTab),
	// new KeyBinding("S-Tab", handleShiftTab),
	// new KeyBinding("C-s", () => { save(); return true; }),
	new KeyBinding("Enter", handleEnter),
	// new KeyBinding("Backspace", (row) => handleBackspace(row)),
	// new KeyBinding("Delete", (row) => handleDelete(row)),
	new KeyBinding("ArrowUp", handleArrowUp),
	new KeyBinding("ArrowDown", handleArrowDown),
	new KeyBinding("ArrowLeft", handleArrowLeft),
	new KeyBinding("ArrowRight", handleArrowRight),
	// new KeyBinding("C-ArrowUp", handleSwapUp),
	// new KeyBinding("C-ArrowDown", handleSwapDown),
	// new KeyBinding("C-.", handleToggleFold),
];

function findKeyBinding(combo: string): KeyBinding {
	return keyBindings.find(kb => kb.combo === combo) ?? new KeyBinding("", () => false);
}

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

	if (e.ctrlKey || e.metaKey || e.altKey || e.key.length > 1)
	{
		lm.messageArea.textContent = combo;
		const currentRow = Editor.CurrentRow();
		if (!currentRow.valid()) return;

		const binding = findKeyBinding(combo);
		if (binding) {
			const result = binding.handler(currentRow);
			if (result === true) {
				e.preventDefault();
			}
		}
	}
 }

 function handleEnter(currentRow: Editor.Row) : boolean {
	// Get HTML string offset (includes tag lengths) for proper splitting
	const htmlOffset = currentRow.getHtmlOffset();
	
	// split the current row at the cursor position
    const sceneRow = model.scene.findRow(currentRow.id);
    sceneRow.site.docLine.split(htmlOffset);
    // docLine notifies siteRow parents of change.
 	return true;
 }

// function joinRows(prevRow: Editor.Row, nextRow: Editor.Row) {
// 	const scene = Scene.data;
// 	console.log('Scene object:', scene);
// 	console.log('joinRows method:', typeof scene.joinRows);
// 	if (typeof scene.joinRows !== 'function') {
// 		console.error('scene.joinRows is not a function. Scene object:', scene);
// 		return;
// 	}
// 	const newRowData = scene.joinRows(prevRow.id, nextRow.id);
	
// 	prevRow.setContent(newRowData.content);
// 	Editor.deleteRow(nextRow);
	
// 	updateAllFoldIndicators();
// }

// function handleBackspace(currentRow: Editor.Row) : boolean {
	
// 	if (currentRow.visibleTextOffset === 0) {
// 		const prevRow = currentRow.Previous;
// 		const prevPosition = prevRow.visibleTextLength;
// 		if (!prevRow.valid()) return true;
// 		joinRows(prevRow, currentRow);
// 		prevRow.setCaretInRow(prevPosition);
// 		return true;
// 	}
// 	else { // we're not at the beginning of the row so just delete the character
// 		return false;
// 	}
// }
// function handleDelete(currentRow: Editor.Row) : boolean {
// 	if (currentRow.visibleTextOffset === currentRow.visibleTextLength) {
// 		const nextRow = currentRow.Next;
// 		if (!nextRow.valid()) return true;
// 		joinRows(currentRow, nextRow);
// 		// currentRow.setCaretInRow(0); position was okay already.
// 		return true;
// 	}
// 	else { return false; }
// }
function handleArrowUp(currentRow: Editor.Row) : boolean {
	const prevP = currentRow.Previous;
	if (!prevP.valid()) return true;
	
	prevP.moveCaretToThisRow();
	return true;
}

 function handleArrowDown(currentRow: Editor.Row) : boolean {
	const nextP = currentRow.Next;
	if (!nextP.valid()) return true;
	
	nextP.moveCaretToThisRow();
	return true;
 }

function handleArrowLeft(currentRow: Editor.Row) : boolean {
	if (currentRow.visibleTextOffset > 0) {
		// Move cursor left within current row
		currentRow.setCaretInRow(currentRow.visibleTextOffset - 1);
	} else {
		// Move to end of previous row (need visible text length)
		const prevRow = currentRow.Previous;
		if (prevRow.valid()) {
			const temp = document.createElement('div');
			temp.innerHTML = prevRow.content;
			const prevRowVisibleLength = temp.textContent?.length ?? 0;
			prevRow.setCaretInRow(prevRowVisibleLength);
		}
	}
	return true;
}

function handleArrowRight(currentRow: Editor.Row) : boolean {
	// Get visible text length to check if at end
	const temp = document.createElement('div');
	temp.innerHTML = currentRow.content;
	const visibleLength = temp.textContent?.length ?? 0;
	
	if (currentRow.visibleTextOffset < visibleLength) {
		// Move cursor right within current row
		currentRow.setCaretInRow(currentRow.visibleTextOffset + 1);
	} else {
		// Move to beginning of next row
		const nextRow = currentRow.Next;
		if (nextRow.valid()) {
			nextRow.setCaretInRow(0);
		}
	}
	return true;
}

// function handleSwapUp(currentRow: Editor.Row) : boolean {
// 	const prevP = currentRow.Previous;
// 	if (!prevP.valid()) return false;
	
// 	Editor.moveRowAbove(currentRow, prevP);
	
// 	// Sync editor to scene
// 	syncAllRowsToScene();
	
// 	// Update fold indicators (indentation context may have changed)
// 	updateAllFoldIndicators();
	
// 	currentRow.focus();
// 	return true;
// }

// function handleSwapDown(currentRow: Editor.Row) : boolean {
// 	const nextP = currentRow.Next;
// 	if (!nextP.valid()) return false;
	
// 	Editor.moveRowAbove(nextP, currentRow);
	
// 	// Sync editor to scene
// 	syncAllRowsToScene();
	
// 	// Update fold indicators (indentation context may have changed)
// 	updateAllFoldIndicators();
	
// 	currentRow.focus();
// 	return true;
// }

// function handleToggleFold(currentRow: Editor.Row) : boolean {
// 	const scene = Scene.data;
// 	const sceneRow = scene.findByLineId(currentRow.id);
	
// 	// Ask scene to calculate which rows should be toggled
// 	const affectedRows = scene.toggleFold(currentRow.id);
	
// 	// If no children to fold, do nothing
// 	if (affectedRows.length === 0) return true;
	
// 	// Update fold indicator for current row
// 	updateFoldIndicator(currentRow);
	
// 	// Apply visibility changes
// 	if (sceneRow.folded) {
// 		Editor.deleteAfter(currentRow, affectedRows.length);
// 	} else {
// 		const addedEditorRows = Editor.addAfter(currentRow, affectedRows);
		
// 		// foreach addedEditorRows and affectedRows, update the fold indicator
// 		let i = 0;
// 		for (const row of addedEditorRows) {
// 			updateFoldIndicator(row);
// 			i++;
// 		}
// 	}
	
// 	// Restore focus
// 	currentRow.focus();
// 	return true;
// }

function updateFoldIndicator(editorRow: Editor.Row) {
	const scene = model.scene;
	const sceneRow = scene.findRow(editorRow.id);
	
	if (!sceneRow.site.hasChildren) {
		editorRow.setFoldIndicator(' ');
	} else if (sceneRow.site.folded) {
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

// function rowDataFromEditorRow(editorRow: Editor.Row): Scene.RowData {
// 	return Scene.data.findByLineId(editorRow.id);
// }

// function handleTab(currentRow: Editor.Row) : boolean {
// 	const visibleOffset = currentRow.visibleTextOffset;
// 	let c = currentRow.content;
	
// 	// Get visible text for indent checking
// 	const temp = document.createElement('div');
// 	temp.innerHTML = c;
// 	const visibleText = temp.textContent ?? '';
	
// 	if (offsetIsInIndent(visibleOffset, visibleText)) {
// 		let rows : ArraySpan<SceneRow> = scene.indentRowAndChildren(rowDataFromEditorRow(currentRow));
// 		Editor.updateRows(rows);
// 	} else {
// 		// Insert tab at HTML offset
// 		const htmlOffset = currentRow.getHtmlOffset();
//         const newContent = c.substring(0, htmlOffset) + '\t' + c.substring(htmlOffset);
//         Scene.data.updateRowData(currentRow.id, newContent);
//         currentRow.setContent(newContent);
//         currentRow.setCaretInRow(visibleOffset + 1);
// 	}
// 	return true;
// }

 function handleShiftTab(currentRow: Editor.Row) : boolean {
//     const visibleOffset = currentRow.visibleTextOffset;
    
//     // Get visible text for indent checking
//     const temp = document.createElement('div');
//     temp.innerHTML = currentRow.content;
//     const visibleText = temp.textContent ?? '';
    
//     if (offsetIsInIndent(visibleOffset, visibleText)) {
//         const rows = Scene.data.deindentRowAndChildren(rowDataFromEditorRow(currentRow));
//         Editor.updateRows(rows);
//     } else {
//         // find tab to the left of the cursor in HTML content
//         const htmlOffset = currentRow.getHtmlOffset();
//         const tabIndex = currentRow.content.substring(0, htmlOffset).lastIndexOf('\t');
//         if (tabIndex === -1) return false;
        
//         const newContent = currentRow.content.substring(0, tabIndex)
//             + currentRow.content.substring(tabIndex + 1);
//         Scene.data.updateRowData(currentRow.id, newContent);
//         currentRow.setContent(newContent);
        
//         // Calculate visible position of the tab for cursor positioning
//         const tempBefore = document.createElement('div');
//         tempBefore.innerHTML = currentRow.content.substring(0, tabIndex);
//         const visibleTabPosition = tempBefore.textContent?.length ?? 0;
//         currentRow.setCaretInRow(visibleTabPosition);
//     }
 	return true;
 }

 export function loadDoc(data : string, filePath : string) : Doc {
    let doc = model.addOrUpdateDoc(data, filePath);
    model.scene.loadFromSite(model.site.getRoot());
    setEditorContent();
    setMessage("Loaded");
    links();
    return doc;
}

 export function save() {
// 	postDoc(Editor.docName(), Editor.getContent());
 }

 export function setEditorContent() {
 	Editor.setContent(new ArraySpan(model.scene.rows, 0, model.scene.rows.length));
// 		// Update fold indicator
// 		updateAllFoldIndicators();
 }

// export function getEditorContent(): string {
// 	// Convert visible tabs back to regular tabs for saving
// 	return Editor.getContent()
// }
