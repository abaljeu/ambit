import * as lm from './elements.js';
import { Scene, SceneRow } from './scene.js';
import { ArraySpan } from './arrayspan.js';
import { Id, Pool } from './pool.js';
import { visibleOffsetToHtmlOffset } from './htmlutil.js';
import * as HtmlUtil from './htmlutil.js';
const RowElementTag: string = 'div';
const RowContentTag: string = 'span';
type RowContentElement = HTMLSpanElement;
type RowElement = HTMLDivElement;
const VISIBLE_TAB = '→'; // Visible tab character, used for internal tabs.

const NOROWID = 'R000000';
function createRowElement(): RowElement {
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
	
	return newEl;
}
export function createRowElementFromSceneRow(sceneRow: SceneRow): Row {
	const newEl = createRowElement();
	const row = new Row(newEl);

	newEl.dataset.lineId = sceneRow.id.value;
	const indent = sceneRow.indent;
	row.setContent(sceneRow.content, indent);
	return row;
}


export class Row {
	public equals(other: Row): boolean {
		return this.newEl === other.newEl;
	}
	constructor(
		public readonly newEl: RowElement
	) {}
	private getContentSpan(): RowContentElement {
		return this.newEl.querySelector('.rowContent') as RowContentElement;
	}
	private getFoldIndicatorSpan(): HTMLSpanElement {
		return this.newEl.querySelector('.fold-indicator') as HTMLSpanElement;
	}

	public getHtmlOffset(): number {
		// Get visible offset from DOM
		const visibleOffset = this.caretOffset;
		
		// Convert visible offset to HTML offset using stored HTML content
		return visibleOffsetToHtmlOffset(this.htmlContent, visibleOffset);
	}
	
	public get visibleText() : string {
		const contentSpan = this.getContentSpan();
		if (!contentSpan) return '';
		return contentSpan.textContent ?? '';
	}
	public get visibleTextLength() : number {
		return this.visibleText.length;
	}
	public get htmlContent(): string {
		const contentSpan = this.getContentSpan();
		if (!contentSpan) return '';
		// Extract innerHTML to preserve HTML tags, then convert visible tabs
		return contentSpan.innerHTML.replace(new RegExp(VISIBLE_TAB, 'g'), '\t');
	}
	public setContent(value: string, sceneIndent: number) {
		const indent = sceneIndent < 0 ? 0 : sceneIndent;
		
		// Set content in newEditor (separate indentation + rowContent)
		const newContentSpan = this.getContentSpan();
		const indentSpan = this.newEl.querySelector('.indentation') as HTMLSpanElement;
		if (indentSpan && newContentSpan) {
			// Clear and rebuild indentation units
			indentSpan.innerHTML = '';
			for (let i = 0; i < indent; i++) {
				const unit = document.createElement('span');
				unit.className = 'indent-unit';
				unit.textContent = '\u00A0'; // Non-breaking space to ensure element renders
				indentSpan.appendChild(unit);
			}
			newContentSpan.innerHTML = value;
		}
	}
	public get indent(): number {
		// count the number of indent-units in the indentation span
		const indentSpan = this.newEl.querySelector('.indentation') as HTMLSpanElement;
		if (!indentSpan) return 0;
		return indentSpan.querySelectorAll('.indent-unit').length;
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
		const off = this.offsetAtX(targetX );
		this.setCaretInRow(off);
	}
	public get caretOffset(): number {
		const contentSpan = this.getContentSpan();
		if (!contentSpan) return 0;
		
		const selection = window.getSelection();
		if (!selection || selection.rangeCount === 0) return 0;
		
		const range = selection.getRangeAt(0);
		const offset = getTextOffsetFromNode(contentSpan, range.startContainer, range.startOffset);
		
		return offset;
	}
	private setCaretInParagraph(contentSpan: RowContentElement, offset: number) {
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
	
	
	public setCaretInRow(visibleOffset: number) {
		const contentSpan = this.getContentSpan();
		if (!contentSpan) {
			 console.error("setCaretInRow: contentSpan is null");
			 return;
		}
		this.setCaretInParagraph(contentSpan, visibleOffset);
	}
	public setSelectionInRow(visibleStart: number, visibleEnd: number): void {
		const contentSpan = this.getContentSpan();
		
		if (!contentSpan) return;
		
		contentSpan.focus();
		const selection = window.getSelection();
		if (!selection) return;
		
		// Convert to node positions
		const startPos = HtmlUtil.getNodeAndOffsetFromTextOffset(contentSpan, visibleStart);
		const endPos = HtmlUtil.getNodeAndOffsetFromTextOffset(contentSpan, visibleEnd);
		
		if (!startPos || !endPos) return;
		
		const range = document.createRange();
		range.setStart(startPos.node, startPos.offset);
		range.setEnd(endPos.node, endPos.offset);
		selection.removeAllRanges();
		selection.addRange(range);
	}
	
	public getSelectionRange(): { start: number, end: number } {
		const contentSpan = this.getContentSpan();
		if (!contentSpan) 
			return {start: 0, end: 0};
		
		const selection = window.getSelection();
		if (!selection || selection.rangeCount === 0) 
			return {start: 0, end: 0};
		
		const range = selection.getRangeAt(0);
		const startOffset = getTextOffsetFromNode(
			contentSpan, 
			range.startContainer, 
			range.startOffset
		);
		const endOffset = getTextOffsetFromNode(
			contentSpan, 
			range.endContainer, 
			range.endOffset
		);
		
		return { start: startOffset, end: endOffset };
	}
	private offsetAtX(x: number): number {
		return this._offsetAtX(x);
	}
	private _offsetAtX(x: number): number {
		const contentSpan = this.getContentSpan();
		if (!contentSpan) return 0;
		
		// Get total text length (works with HTML too)
		const text = contentSpan.textContent ?? '';
		const len = text.length;
		if (len === 0) return 0;
		
		let closestIndex = 0;
		let closestDistance = Infinity;
		
		// Check each text position to find closest to target X
		for (let i = 0; i <= len; i++) {
			// Convert text offset to DOM position
			const position = getNodeAndOffsetFromTextOffset(contentSpan, i);
			if (!position) continue;
			
			const r = document.createRange();
			r.setStart(position.node, position.offset);
			r.collapse(true);
			const rect = r.getBoundingClientRect();
			const distance = Math.abs(rect.left - x);
			
			if (distance < closestDistance) {
				closestDistance = distance;
				closestIndex = i;
			}
		}
		return closestIndex;
	}
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

// Helper: Walk DOM tree and find node+offset for a given text offset
function getNodeAndOffsetFromTextOffset(
	container: RowContentElement, 
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

// Helper: Get text offset from a DOM position (visible text, ignoring tags)
function getTextOffsetFromNode(container: RowContentElement, targetNode: Node, targetOffset: number): number {
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
	const contentSpan = currentP.querySelector('.rowContent') as HTMLSpanElement;
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
	let p = getCurrentParagraphWithOffset();
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
	let end = endRow;
	return addBefore(end, scene);
}

