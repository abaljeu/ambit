import { postDoc } from './ambit.js';
import * as lm from './elements.js';
import * as Editor from './editor.js';
import { Scene, SceneRow } from './scene.js';
import { ArraySpan } from './arrayspan.js';
import { model } from './model.js';
import { Doc, DocLine } from './doc.js';
import { Site, SiteRow } from './site.js';
import * as Change from './change.js';


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
	new KeyBinding("Tab", handleTab),
	new KeyBinding("S-Tab", handleShiftTab),
	new KeyBinding("C-s", () => { save(); return true; }),
	new KeyBinding("Enter", handleEnter),
	new KeyBinding("Backspace", (row) => handleBackspace(row)),
	new KeyBinding("Delete", (row) => handleDelete(row)),
	new KeyBinding("ArrowUp", handleArrowUp),
	new KeyBinding("ArrowDown", handleArrowDown),
	new KeyBinding("ArrowLeft", handleArrowLeft),
	new KeyBinding("ArrowRight", handleArrowRight),
	new KeyBinding("C-ArrowUp", handleSwapUp),
	new KeyBinding("C-ArrowDown", handleSwapDown),
	new KeyBinding("C-.", handleToggleFold),
];

function findKeyBinding(combo: string): KeyBinding {
	let binding = keyBindings.find(kb => kb.combo === combo);
	if (binding) return binding;
	if (combo.length == 1) {
		return new KeyBinding(combo, (row) => handleInsertChar(row, combo));
	}
	return new KeyBinding("", () => false);
}

 export function editorHandleKey(e : KeyboardEvent) {
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
	lm.messageArea.textContent = combo;

	const currentRow = Editor.currentRow();
	if (!currentRow.valid()) return;
	const binding = findKeyBinding(combo);
	if (binding) {
		const result = binding.handler(currentRow);
		if (result === true) {
			e.preventDefault();
		}
	} 
		updateAllFoldIndicators();
 }

 function handleEnter(currentRow: Editor.Row) : boolean {
	// Get HTML string offset (includes tag lengths) for proper splitting
	const htmlOffset = currentRow.getHtmlOffsetWithTabs();
	
	const docLine = docLineFromRow(currentRow);
	const beforeText = currentRow.bareContent.substring(0, htmlOffset-currentRow.indent);
	const newDocLine = Doc.createLine(beforeText);
	const insertBefore = Change.makeInsertBefore(docLine, [newDocLine]);
	Doc.processChange(insertBefore);
	const textChange = Change.makeTextChange(docLine, 0, htmlOffset-currentRow.indent, '');
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

function handleBackspace(currentRow: Editor.Row) : boolean {
	
	if (currentRow.caretOffset === 0) {
		const prevRow = currentRow.Previous;
		const prevPosition = prevRow.visibleTextLengthWithTabs;
		if (!prevRow.valid()) return true;
		joinRows(prevRow, currentRow);
		Editor.findRow(currentRow.id).setCaretInRowWithTabs(prevPosition);
		return true;
	}
	else { // we're not at the beginning of the row so just delete the character
		const htmlOffset = currentRow.getHtmlOffsetWithTabs();
		deleteChar(currentRow, htmlOffset-1);
		return true;
	}
}
function deleteChar(currentRow: Editor.Row, offset: number) {
	const cur = model.scene.findRow(currentRow.idString);
	const scur = cur.siteRow;
	const change = Change.makeTextChange(scur.docLine,offset, 1, '');
	Doc.processChange(change);
	currentRow.setCaretInRowWithTabs(offset);

}
function handleDelete(currentRow: Editor.Row) : boolean {
	if (currentRow.caretOffsetWithTabs >= currentRow.visibleTextLengthWithTabs) {
		const nextRow = currentRow.Next;
		if (!nextRow.valid()) return true;
		joinRows(currentRow, nextRow);
		// currentRow.setCaretInRow(0); position was okay already.
		return true;
	}
	else {
		const htmlOffset = currentRow.getHtmlOffsetWithTabs();
		deleteChar(currentRow, htmlOffset);
	 return true; 
	}
}
function handleArrowUp(currentRow: Editor.Row) : boolean {
	const prevP = currentRow.Previous;
	if (!prevP.valid()) return true;
	
	prevP.moveCaretToThisRowWithTabs();
	return true;
}

 function handleArrowDown(currentRow: Editor.Row) : boolean {
	const nextP = currentRow.Next;
	if (!nextP.valid()) return true;
	
	nextP.moveCaretToThisRowWithTabs();
	return true;
 }

function handleArrowLeft(currentRow: Editor.Row) : boolean {
	if (currentRow.caretOffsetWithTabs > 0) {
		// Move cursor left within current row
		currentRow.setCaretInRowWithTabs(currentRow.caretOffsetWithTabs - 1);
	} else {
		// Move to end of previous row (need visible text length)
		const prevRow = currentRow.Previous;
		if (prevRow.valid()) {
			prevRow.setCaretInRowWithTabs(prevRow.visibleTextLengthWithTabs);
		}
	}
	return true;
}

function handleArrowRight(currentRow: Editor.Row) : boolean {
	// Get visible text length to check if at end
	const visibleLengthWithTabs = currentRow.visibleTextLengthWithTabs;
	
	if (currentRow.caretOffsetWithTabs < visibleLengthWithTabs) {
		// Move cursor right within current row
		currentRow.setCaretInRowWithTabs(currentRow.caretOffsetWithTabs + 1);
	} else {
		// Move to beginning of next row
		const nextRow = currentRow.Next;
		if (nextRow.valid()) {
			nextRow.setCaretInRowWithTabs(0);
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
function handleSwapUp(currentRow: Editor.Row): boolean {
    const prevRow = currentRow.Previous;
    if (!prevRow.valid()) 
		return false;
    
    const cur = model.scene.findRow(currentRow.idString);
	const prev = model.scene.findRow(prevRow.idString);
    
    const docCur = cur.siteRow.docLine;
    const docPrev = prev.siteRow.docLine;
    
    return performRowSwap(docCur, docPrev, currentRow.idString);
}

function handleSwapDown(currentRow: Editor.Row): boolean {
    const cur = model.scene.findRow(currentRow.idString);
    
    // Skip over all descendants to find the next row that's not a child
    const descendantCount = cur.treeLength;
    let nextRow = currentRow;
    for (let i = 0; i < descendantCount; i++) {
        nextRow = nextRow.Next;
        if (!nextRow.valid()) return false;
    }
    
    const next = model.scene.findRow(nextRow.idString);
    
    const docCur = cur.siteRow.docLine;
    const docNext = next.siteRow.docLine;
   
    return performRowSwap(docNext, docCur, currentRow.idString);
}

function performRowSwap(
    lineToMove: DocLine, 
    lineBefore: DocLine, 
    currentRowId: string
): boolean {
    moveBefore(lineToMove, lineBefore);
    Editor.findRow(currentRowId).setCaretInRowWithTabs(0);
    return true;
}
function handleToggleFold(currentRow: Editor.Row) : boolean {
	const sceneRow = model.scene.findRow(currentRow.idString);
	const siteRow = sceneRow.siteRow;
	siteRow.toggleFold();
	return true;
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

function insertChar(currentRow : Editor.Row, ch : string) {
	const cur = model.scene.findRow(currentRow.idString);
	const scur = cur.siteRow;
	const htmlOffset = currentRow.getHtmlOffsetWithTabs();
	const change = Change.makeTextChange(scur.docLine,htmlOffset, 0,ch);
	Doc.processChange(change);
	currentRow.setCaretInRowWithTabs(htmlOffset+ 1);
}

function handleInsertChar(currentRow : Editor.Row, ch : string) {
	if (currentRow.caretOffsetWithTabs <= currentRow.indent) {
		return true; // can't edit in tabs
	}
	insertChar(currentRow, ch);
	return true;
}

function handleTab(currentRow: Editor.Row) : boolean {
	const visibleOffset = currentRow.caretOffsetWithTabs;
	
	if (visibleOffset <= currentRow.indent) {
		const cur = model.scene.findRow(currentRow.idString);
		const scur = cur.siteRow;
			const sprev = scur.previous;
		if (sprev === SiteRow.end) return false;
		moveBelow(scur.docLine, sprev.docLine);
		const replacementRow = Editor.findRow(currentRow.id)
		replacementRow.setCaretInRowWithTabs(visibleOffset+ 1);
		return true;
	}

	insertChar(currentRow, '\t');
	return true;
}
function docLineFromRow(row: Editor.Row): DocLine {
	const cur = model.scene.findRow(row.idString);
	return cur.siteRow.docLine;
}
 function handleShiftTab(currentRow: Editor.Row) : boolean {
    const offset = currentRow.caretOffsetWithTabs;
    // Get visible text for indent checking
	if (currentRow.indent >= offset) {
		if (currentRow.indent <= 0) return true;
		// Move after parent
		// Move before parent's sibling if any.
		const docLine = docLineFromRow(currentRow);
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
			Editor.findRow(currentRow.id).setCaretInRowWithTabs(offset-1);
		}
	} else {
		// find tab to the left of the cursor in HTML content
		const visibleOffset = currentRow.caretOffsetWithTabs;
		const htmlOffset = currentRow.getHtmlOffsetWithTabs();
		const tabIndex = currentRow.contentWithTabs.substring(0, htmlOffset).lastIndexOf('\t');
		if (tabIndex === -1) return false;

		const docLine = docLineFromRow(currentRow);
		const change = Change.makeTextChange(docLine, htmlOffset, 0, '\t');
		Doc.processChange(change);
		currentRow.setCaretInRowWithTabs(visibleOffset + 1);
	}
	return true;
}

export function loadDoc(data: string, filePath: string): Doc {
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
