import * as lm from './elements.js';
import { Scene, SceneRow } from './scene.js';
import { ArraySpan } from './arrayspan.js';
import { Id, Pool } from './pool.js';

const RowElementTag: string = 'div';
const RowContentTag: string = 'span';
type RowContentElement = HTMLSpanElement;
type RowElement = HTMLDivElement;
const VISIBLE_TAB = '→'; // Visible tab character

type RowElementPair = { el: RowElement; newEl: RowElement; };

// export class RowId extends Id<'Row'> {
//     public constructor(value: string) {
//         if (!/^R[0-9A-Z]{6}$/.test(value)) {
//             throw new Error('Invalid RowId');
//         }
//         super(value);
//     }
// }
// class EditorRowPool extends Pool<Row, RowId> {
//     protected override fromString(value: string): RowId {
//         return new RowId(value);
//     }
//     public get end(): Row {
//         return endRow;
//     }
//     public readonly tag: string = 'R';
// }
// const editorRowPool = new EditorRowPool();
export function createRowElement(): RowElementPair {
	// Create editor element (2-span: fold-indicator + content with inline tabs)
	const el = document.createElement(RowElementTag) as RowElement;
	const elFold = document.createElement('span');
	elFold.className = 'fold-indicator';
	elFold.textContent = ' ';
	const elContent = document.createElement(RowContentTag);
	elContent.className = 'content';
	elContent.contentEditable = 'true';
	el.appendChild(elFold);
	el.appendChild(elContent);
	
	// Create newEditor element (3-span: fold-indicator + indentation + rowContent)
	const newEl = document.createElement(RowElementTag) as RowElement;
	const newElFold = document.createElement('span');
	newElFold.className = 'fold-indicator';
	newElFold.textContent = ' ';
	const newElIndent = document.createElement(RowContentTag);
	newElIndent.className = 'indentation';
	newElIndent.contentEditable = 'false';
	const newElContent = document.createElement(RowContentTag);
	newElContent.className = 'rowContent';
	newElContent.contentEditable = 'true';
	newEl.appendChild(newElFold);
	newEl.appendChild(newElIndent);
	newEl.appendChild(newElContent);
	
	return { el, newEl };
}
export function createRowElementFromSceneRow(sceneRow: SceneRow): Row {
	const pair = createRowElement();
	const row = new Row(pair.el, pair.newEl);

	pair.el.dataset.lineId = sceneRow.id.value;
	pair.newEl.dataset.lineId = sceneRow.id.value;
	const indent = sceneRow.indent;
	row.setContent(sceneRow.content, indent);
	return row;
}

export class Row {
		public equals(other: Row): boolean {
			return this.el === other.el;
		}
		constructor(
			public readonly el: RowElement,
			public readonly newEl: RowElement
		) {}
		private getContentSpanWithTabs(): RowContentElement {
			return this.el.querySelector('.content') as RowContentElement;
		}
		private getFoldIndicatorSpan(): RowContentElement {
			return this.el.querySelector('.fold-indicator') as RowContentElement;
		}
	public getHtmlOffsetWithTabs(): number {
		const contentSpan = this.getContentSpanWithTabs();
		if (!contentSpan) return 0;
		
		// Find the current cursor position in the DOM
		const selection = window.getSelection();
		if (!selection || selection.rangeCount === 0) return 0;
		
		const range = selection.getRangeAt(0);
		
		// Use getHtmlOffsetFromNode to compute HTML string offset
		return getHtmlOffsetFromNode(contentSpan, range.startContainer, range.startOffset);
	}
	public get visibleTextWithTabs() : string {
		const contentSpan = this.getContentSpanWithTabs();
		if (!contentSpan) return '';
		return contentSpan.textContent ?? '';
	}
	public get visibleTextLengthWithTabs() : number {
		return this.visibleTextWithTabs.length;
	}
	public get contentWithTabs(): string {
		const contentSpan = this.getContentSpanWithTabs();
		if (!contentSpan) return '';
		// Extract innerHTML to preserve HTML tags, then convert visible tabs
		return contentSpan.innerHTML.replace(new RegExp(VISIBLE_TAB, 'g'), '\t');
	}
	public get bareContent(): string {
		const contentSpan = this.getContentSpanWithTabs();
		if (!contentSpan) return '';
		// Extract innerHTML to preserve HTML tags, then convert visible tabs
		return contentSpan.innerHTML.replace(new RegExp(VISIBLE_TAB, 'g'), '\t')
			.substring(this.indent);
	}
	public setContent(value: string, sceneIndent: number) {
		const indent = sceneIndent < 0 ? 0 : sceneIndent;
		
		// Set content in editor (inline tabs + content)
		const contentSpan = this.getContentSpanWithTabs();
		if (contentSpan) {
			contentSpan.innerHTML = VISIBLE_TAB.repeat(indent) + value;
		}
		
		// Set content in newEditor (separate indentation + rowContent)
		const indentSpan = this.newEl.querySelector('.indentation') as HTMLSpanElement;
		const rowContentSpan = this.newEl.querySelector('.rowContent') as HTMLSpanElement;
		if (indentSpan && rowContentSpan) {
			// Clear and rebuild indentation units
			indentSpan.innerHTML = '';
			for (let i = 0; i < indent; i++) {
				const unit = document.createElement('span');
				unit.className = 'indent-unit';
				unit.textContent = '\u00A0'; // Non-breaking space to ensure element renders
				indentSpan.appendChild(unit);
			}
			rowContentSpan.innerHTML = value;
		}
	}
	public get indent(): number {
		// count the number of tabs at the beginning of the content
		const contentSpan = this.getContentSpanWithTabs();
		const innerHTML = contentSpan?.innerHTML ?? '';
		const tabs = innerHTML.match(new RegExp('^' + VISIBLE_TAB + '+'));
		if (tabs) return tabs[0].length;
		return 0;
	}
	public setFoldIndicator(indicator: string) {
		// Set in editor
		const foldSpan = this.getFoldIndicatorSpan();
		if (foldSpan) foldSpan.textContent = indicator;
		
		// Set in newEditor
		const newFoldSpan = this.newEl.querySelector('.fold-indicator') as HTMLSpanElement;
		if (newFoldSpan) newFoldSpan.textContent = indicator;
	}
	public get Previous(): Row {
		const previousSibling = this.el?.previousElementSibling;
		const newPreviousSibling = this.newEl?.previousElementSibling;
		if (!previousSibling || !newPreviousSibling) return endRow;
		return new Row(previousSibling as RowElement, newPreviousSibling as RowElement);
	}
	public get Next(): Row {
		const nextSibling = this.el?.nextElementSibling;
		const newNextSibling = this.newEl?.nextElementSibling;
		if (!nextSibling || !newNextSibling) return endRow;
		return new Row(nextSibling as RowElement, newNextSibling as RowElement);
	}
	public valid(): boolean {
		return this.el !== null;
	}
	public get id(): string {
		return  this.idString;
	}
	
	// Helper method to get the string representation for DOM operations
	public get idString(): string {
		return this.el?.dataset.lineId ?? 'R000000';
	}
	public setCaretInRowWithTabs(offset: number) {
		const contentSpan = this.getContentSpanWithTabs();
		if (contentSpan) setCaretInParagraph(contentSpan, offset);
	}
	public moveCaretToThisRowWithTabs(): void {
		const targetX = caretX();
		const off = this.offsetAtXWithTabs(targetX );
		this.setCaretInRowWithTabs(off);
	}
	public moveCaretToXWithTabs(targetX: number): void {
		const off = this.offsetAtXWithTabs(targetX );
		this.setCaretInRowWithTabs(off);
	}
	public get caretOffsetWithTabs() {
		const x = caretX();
		return this.offsetAtXWithTabs(x);
	}
	public get caretOffset(): number {
		const x = caretX();
		return this.offsetAtX(x);
	}
	public offsetAtX(x: number): number {
		const owt = this.offsetAtXWithTabs(x);
		const  o = owt - this.indent;
		return o < 0 ? 0 : o;
	}
	public offsetAtXWithTabs(x: number): number {
		const contentSpan = this.getContentSpanWithTabs();
		if (!contentSpan) return 0;
		
		// Get total text length (works with HTML too)
		const text = contentSpan.textContent ?? '';
		const len = text.length;
		if (len === 0) return 0;
	
		let lastdist = x;
		let dist = x;
		
		// Check each text position
		for (let i = 0; i < len; i++) {
			// Convert text offset to DOM position
			const position = getNodeAndOffsetFromTextOffset(contentSpan, i);
			if (!position) continue;
			
			const r = document.createRange();
			r.setStart(position.node, position.offset);
			r.collapse(true);
			const rect = r.getBoundingClientRect();
			dist = rect.left - x;
			if (dist >= -0.000001) return i;
		}
		return len;
	}
}

	export function findRow(id: string): Row {
		for (const row of rows()) {
			if (row.idString == id) 
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
			row = row.Next;
		}
	}
	public endRow(): Row { // [row, row+count)
		let row = this.row;
		for (let i = 0; i < this.count; i++) {
			row = row.Next;
		}
		return row;
	}
	public last() : Row {
		let row = this.row;
		for (let i = 0; i < this.count - 1; i++) {
			row = row.Next;
		}
		return row;
	}
}

// Sentinel row used to indicate insertion at the start of the container
export const endRow: Row = new Row(
	document.createElement(RowElementTag) as RowElement,
	document.createElement(RowElementTag) as RowElement
);

// Iterator that yields parallel element pairs from both editors
function* paragraphPairs(): IterableIterator<RowElementPair> {
	const editorParagraphs = lm.editor.querySelectorAll(RowElementTag);
	const newEditorParagraphs = lm.newEditor.querySelectorAll(RowElementTag);
	const count = Math.min(editorParagraphs.length, newEditorParagraphs.length);
	
	for (let i = 0; i < count; i++) {
		yield {
			el: editorParagraphs[i] as RowElement,
			newEl: newEditorParagraphs[i] as RowElement
		};
	}
}

// create an iterator that yields rows from the DOM
export function* rows(): IterableIterator<Row> {
	for (const pair of paragraphPairs()) {
		yield new Row(pair.el, pair.newEl);
	}
}

export function at(index: number): Row {
	// if index is out of range, return endRow
	if (index < 0 || index >= lm.editor.childElementCount) return endRow;
	if (index >= lm.newEditor.childElementCount) return endRow;
	return new Row(
		lm.editor.children[index] as RowElement,
		lm.newEditor.children[index] as RowElement
	);
}
// Create a new row and insert it after the given previous row.
// If previousRow is endRow, insert at the front of the container.
export function addBefore(targetRow: Row, scene: ArraySpan<SceneRow>)
	: RowSpan {
	if (lm.editor === null) 
		return new RowSpan(endRow, 0);
	
	let firstRow = endRow;
	for (const sceneRow of scene) {
		const row = createRowElementFromSceneRow(sceneRow);
		if (firstRow === endRow) firstRow = row;

		// Insert into editor
		if (targetRow === endRow) {
			lm.editor.appendChild(row.el);
		} else {
			lm.editor.insertBefore(row.el, targetRow.el);
		}
		
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
	if (lm.editor === null) return new RowSpan(endRow, 0);
	let count = 0;
	let beforeRow = referenceRow.el;
	let newBeforeRow = referenceRow.newEl;
	let first = endRow;
	
	for (const rowData of rowDataArray) {
		const row = createRowElementFromSceneRow(rowData);
		
		// Insert into editor
		if (beforeRow.nextSibling) {
			lm.editor.insertBefore(row.el, beforeRow.nextSibling);
		} else {
			lm.editor.appendChild(row.el);
		}
		
		// Insert into newEditor
		if (newBeforeRow.nextSibling) {
			lm.newEditor.insertBefore(row.newEl, newBeforeRow.nextSibling);
		} else {
			lm.newEditor.appendChild(row.newEl);
		}
		
		if (first === endRow) first = row;
		beforeRow = row.el;
		newBeforeRow = row.newEl;
		count++;
	}
	
	return new RowSpan(first, count);
}

// Helper: Walk DOM tree and find node+offset for a given text offset
function getNodeAndOffsetFromTextOffset(
	container: HTMLElement, 
	textOffset: number
): { node: Node, offset: number } | null {
	let currentOffset = 0;
	
	function walk(node: Node): { node: Node, offset: number } | null {
		if (node.nodeType === Node.TEXT_NODE) {
			const textLength = node.textContent?.length ?? 0;
			if (currentOffset + textLength >= textOffset) {
				return { node, offset: textOffset - currentOffset };
			}
			currentOffset += textLength;
		} else if (node.nodeType === Node.ELEMENT_NODE) {
			for (const child of node.childNodes) {
				const result = walk(child);
				if (result) return result;
			}
		}
		return null;
	}
	
	return walk(container);
}

// Helper: Get text offset from a DOM position
function getHtmlOffsetFromNode(container: HTMLElement, targetNode: Node, targetOffset: number): number {
	let textOffset = 0;
	
	function walk(node: Node): boolean {
		if (node === targetNode) {
			textOffset += targetOffset;
			return true;
		}
		
		if (node.nodeType === Node.TEXT_NODE) {
			textOffset += node.textContent?.length ?? 0;
		} else if (node.nodeType === Node.ELEMENT_NODE) {
			const element = node as Element;
			const tagName = element.tagName.toLowerCase();
			
			// Add opening tag: <tagname>
			textOffset += tagName.length + 2;
			
			// Check if element has children (not self-closing)
			const hasChildren = element.childNodes.length > 0;
			
			// Walk children
			for (const child of element.childNodes) {
				if (walk(child)) return true;
			}
			
			// Add closing tag if not self-closing: </tagname>
			// Self-closing tags like <br>, <img> don't have closing tags
			if (hasChildren || !isSelfClosingTag(tagName)) {
				textOffset += tagName.length + 3;
			}
		}
		return false;
	}
	
	walk(container);
	return textOffset - container.tagName.length-2;
}

// Helper: Check if a tag is self-closing
function isSelfClosingTag(tagName: string): boolean {
	const selfClosing = ['br', 'hr', 'img', 'input', 'meta', 'link', 'area', 'base', 'col', 'embed', 'param', 'source', 'track', 'wbr'];
	return selfClosing.includes(tagName.toLowerCase());
}
function getTextOffsetFromNode(container: HTMLElement, targetNode: Node, targetOffset: number): number {
	let textOffset = 0;
	let found = false;
	
	function walk(node: Node): boolean {
		if (node === targetNode) {
			textOffset += targetOffset;
			return true;
		}
		
		if (node.nodeType === Node.TEXT_NODE) {
			textOffset += node.textContent?.length ?? 0;
		} else if (node.nodeType === Node.ELEMENT_NODE) {
			for (const child of node.childNodes) {
				if (walk(child)) return true;
			}
		}
		return false;
	}
	
	walk(container);
	return textOffset;
}

function getCurrentParagraphWithOffset(): { element: RowElement, offset: number } | null {
	const selection = window.getSelection();
	if (!selection || selection.rangeCount === 0) return null;
	
	const range = selection.getRangeAt(0);
	let node = selection.anchorNode;
	
	// Navigate up to find the row div
	let currentP: RowElement | null = null;
	while (node && node !== lm.editor) {
		if (node.nodeName === RowElementTag.toUpperCase() && 
			node.parentNode === lm.editor) {
			currentP = node as RowElement;
			break;
		}
		node = node.parentNode;
	}
	
	if (!currentP) return null;

	// Calculate offset within content span
	const contentSpan = currentP.querySelector('.content') as HTMLSpanElement;
	if (!contentSpan) return { element: currentP, offset: 0 };
	
	// Use helper to calculate text offset from DOM position
	const offset = getTextOffsetFromNode(
		contentSpan, 
		range.startContainer, 
		range.startOffset
	);
	
	return { element: currentP, offset };
}

function setCaretInParagraph(contentSpan: HTMLElement, offset: number) {
	if (offset < 0) offset = 0;
	contentSpan.focus();
	const selection = window.getSelection();
	if (!selection) return;
	
	// Use helper to convert text offset to DOM position
	const position = getNodeAndOffsetFromTextOffset(contentSpan, offset);
	if (!position) {
		// Fallback: place at end of content
		const range = document.createRange();
		range.selectNodeContents(contentSpan);
		range.collapse(false);
		selection.removeAllRanges();
		selection.addRange(range);
		return;
	}
	
	const range = document.createRange();
	range.setStart(position.node, position.offset);
	range.collapse(true);
	selection.removeAllRanges();
	selection.addRange(range);
}

export function currentRow(): Row {
	let p = getCurrentParagraphWithOffset();
	if (!p) return endRow;
	
	// Find corresponding newEditor element at same index
	const index = Array.from(lm.editor.children).indexOf(p.element);
	const newEl = (index >= 0 && index < lm.newEditor.children.length) 
		? lm.newEditor.children[index] as RowElement
		: endRow.newEl;
	
	return new Row(p.element, newEl);
}
export function currentRowWithOffset(): { element: Row, offset: number } {
	let p = getCurrentParagraphWithOffset();
	if (!p) return { element: endRow, offset: 0 };
	
	// Find corresponding newEditor element at same index
	const index = Array.from(lm.editor.children).indexOf(p.element);
	const newEl = (index >= 0 && index < lm.newEditor.children.length) 
		? lm.newEditor.children[index] as RowElement
		: endRow.newEl;
	
	return { element: new Row(p.element, newEl), offset: p.offset };
}

export function moveRowAbove(toMove: Row, target: Row): void {
	if (toMove.el && target.el) {
		// Move in editor
		toMove.el.parentNode!.insertBefore(toMove.el, target.el);
		// Move in newEditor
		toMove.newEl.parentNode!.insertBefore(toMove.newEl, target.newEl);
	} else {
		if (toMove.el)
			console.error("moveBefore: target is null");
		else if (target.el)
			console.error("moveBefore: toMove is null");
	}
}
export function caretX(): number {
	const sel = window.getSelection();
	if (!sel || sel.rangeCount === 0) return 0;
	const r = sel.getRangeAt(0).cloneRange();
	r.collapse(true);
	const rect = r.getBoundingClientRect();
	return rect.left;
}

export function clear(): void {
	lm.editor.innerHTML = '';
	lm.newEditor.innerHTML = '';
}

export function getContentLines(): string[] {
	const lines: string[] = [];
	for (const row of rows()) {
		lines.push(row.contentWithTabs);
	}
	return lines;
}

export function getContent(): string {
	
	return getContentLines().join('\n');
}

export function replaceRows(oldRows: RowSpan, newRows: ArraySpan<SceneRow>): RowSpan {
	if (oldRows.count === 0 && newRows.length === 0) return new RowSpan(endRow, 0);
	
	const beforeStartRow = oldRows.row.Previous;
	
	// Collect all rows first before removing (to avoid breaking the DOM chain)
	const rowsToRemove: Row[] = Array.from(oldRows);
	
	// Now remove them
	for (const row of rowsToRemove) {
		row.el.remove();
		row.newEl.remove();
	}
	
	return addAfter(beforeStartRow, newRows);
}

// Update rows with Scene.RowData array
export function updateRows(rowDataArray: ArraySpan<SceneRow>): void {
	let idx = 0;
	for (const row of rows()) {
		const rowData = rowDataArray.at(idx);
		if (!rowData) break;
		
		row.setContent(rowData.content, rowData.indent);
		idx++;
	}
}

export function deleteRow(row: Row): void {
	row.el.remove();
	row.newEl.remove();
}


export function deleteAfter(referenceRow: Row, count: number): void {
	let current = referenceRow.Next;
	for (let i = 0; i < count && current.valid(); i++) {
		const next = current.Next;
		current.el.remove();
		current.newEl.remove();
		current = next;
	}
}
export function docName(): string {
	return lm.path.textContent ?? '';
}
export function setDocName(name: string): void {
	lm.path.textContent = name;
}
export function setContent(scene: ArraySpan<SceneRow>): RowSpan {    // Clear the Editor
	clear();

	// Create a Line element for each visible line
	let end = endRow;
	return addBefore(end, scene);
}

