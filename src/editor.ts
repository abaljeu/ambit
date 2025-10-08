import * as lm from './elements.js';
import * as Scene from './scene.js';
import { ArraySpan } from './arrayspan.js';

const RowElementTag: string = 'div';
const RowContentTag: string = 'span';
type RowContentElement = HTMLSpanElement;
type RowElement = HTMLDivElement;
const VISIBLE_TAB = '→'; // Visible tab character

export function createRowElement(): RowElement {
	const el = document.createElement(RowElementTag) as RowElement;
	
	const foldIndicator = document.createElement(RowContentTag);
	foldIndicator.className = 'fold-indicator';
	foldIndicator.textContent = ' ';
	
	const content = document.createElement(RowContentTag);
	content.className = 'content';
	content.contentEditable = 'true';
	
	el.appendChild(foldIndicator);
	el.appendChild(content);
	
	return el;
}

export class Row {
		constructor(public readonly el: RowElement, public readonly visibleTextOffset: number) {}
		private getContentSpan(): RowContentElement {
			return this.el.querySelector('.content') as RowContentElement;
		}
		private getFoldIndicatorSpan(): RowContentElement {
			return this.el.querySelector('.fold-indicator') as RowContentElement;
		}
	public getHtmlOffset(): number {
		const contentSpan = this.getContentSpan();
		if (!contentSpan) return 0;
		
		// Find the current cursor position in the DOM
		const selection = window.getSelection();
		if (!selection || selection.rangeCount === 0) return 0;
		
		const range = selection.getRangeAt(0);
		
		// Use getHtmlOffsetFromNode to compute HTML string offset
		return getHtmlOffsetFromNode(contentSpan, range.startContainer, range.startOffset);
	}
	
	public get content(): string {
		const contentSpan = this.getContentSpan();
		if (!contentSpan) return '';
		// Extract innerHTML to preserve HTML tags, then convert visible tabs
		return contentSpan.innerHTML.replace(new RegExp(VISIBLE_TAB, 'g'), '\t');
	}
	public setContent(value: string) {
		const contentSpan = this.getContentSpan();
		if (contentSpan) {
			// Use innerHTML to allow HTML tags, and convert tabs to visible tabs
			contentSpan.innerHTML = 
				value.replace(new RegExp('\t', 'g'), VISIBLE_TAB);
		}
	}
		public setFoldIndicator(indicator: string) {
			const foldSpan = this.getFoldIndicatorSpan();
			if (foldSpan) foldSpan.textContent = indicator;
		}
		public get Previous(): Row {
			const previousSibling = this.el?.previousElementSibling;
			if (!previousSibling) return NoRow;
			return new Row(previousSibling as RowElement, 0);
		}
		public get Next(): Row {
			const nextSibling = this.el?.nextElementSibling;
			if (!nextSibling) return NoRow;
			return new Row(nextSibling as RowElement, 0);
		}
		public valid(): boolean {
			return this.el !== null;
		}
		public get id(): string {
			return this.el?.dataset.lineId ?? "000000";
		}
		public setCaretInRow(offset: number) {
			const contentSpan = this.getContentSpan();
			if (contentSpan) setCaretInParagraph(contentSpan, offset);
		}
		public moveCaretToThisRow(): void {
			const targetX = caretX();
			const off = this.offsetAtX(targetX );
			this.setCaretInRow(off);
		}
		public moveCaretToX(targetX: number): void {
			const off = this.offsetAtX(targetX );
			this.setCaretInRow(off);
		}
	public offsetAtX(x: number): number {
		const contentSpan = this.getContentSpan();
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
			dist = Math.abs(rect.left - x);
			if (dist > lastdist) 
				return i - 1 >= 0 ? i - 1 : 0;
			lastdist = dist;
		}
		return len;
	}
		public focus(): void {
			const contentSpan = this.getContentSpan();
			if (contentSpan) contentSpan.focus();
		}
	}

// RowSpan is not an ArraySpan because the rows are virtual.
// it's implemented by taking a row and finding the next row.
export class RowSpan implements Iterable<Row> {
	constructor(public readonly row: Row, public readonly count: number) {}
	
	// fix this
	*[Symbol.iterator](): Iterator<Row> {
		let row = this.row;
		for (let i = 0; i < this.count; i++) {
			yield this.row;
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
}

// Sentinel row used to indicate insertion at the start of the container
export const NoRow: Row = new Row(document.createElement(RowElementTag) as RowElement, 0);

// Paragraphs iterator
function* paragraphs(): IterableIterator<RowElement> {
	const paragraphs = lm.editor.querySelectorAll(RowElementTag);
	for (const paragraph of paragraphs) {
		yield paragraph as RowElement;
	}
}
// create an iterator that yields rows from the DOM
export function* rows(): IterableIterator<Row> {
	for (const paragraph of paragraphs()) {
		yield new Row(paragraph, 0);
	}
}
export function at(index: number): Row {
	// if index is out of range, return NoRow
	if (index < 0 || index >= lm.editor.childElementCount) return NoRow;
	return new Row(lm.editor.children[index] as RowElement, 0);
}
// Create a new row and insert it after the given previous row.
// If previousRow is NoRow, insert at the front of the container.
export function addBefore(targetRow: Row, scene: ArraySpan<Scene.RowData>)
	: RowSpan {
	if (lm.editor === null) 
		return new RowSpan(NoRow, 0);
	
	let firstRow = NoRow;
	for (const sceneRow of scene) {
		if (!sceneRow.visible) continue;
		
		// Convert regular tabs to visible tabs for display
		const visibleContent = sceneRow.content.replace(/\t/g, VISIBLE_TAB);
		const el = createRowElement();
		el.dataset.lineId = sceneRow.id;
		let row = new Row(el, 0);
		if (firstRow === NoRow) firstRow = row;

		row.setContent(visibleContent);
		if (targetRow === NoRow) {
			lm.editor.append(el); // add to front of editor
		} else {
			lm.editor.insertBefore(el, targetRow.el);
		}
	}
	return new RowSpan(firstRow, scene.length);
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

export function CurrentRow(): Row {
	let p = getCurrentParagraphWithOffset();
	return p ? new Row(p.element, p.offset) : NoRow;
}

export function moveRowAbove(toMove: Row, target: Row): void {
	if (toMove.el && target.el)
		toMove.el.parentNode!.insertBefore(toMove.el, target.el);
	else
	{	if (toMove.el)
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
}

export function getContentLines(): string[] {
	const lines: string[] = [];
	for (const row of rows()) {
		lines.push(row.content);
	}
	return lines;
}

export function getContent(): string {
	
	return getContentLines().join('\n');
}

export function replaceRows(oldRows: RowSpan, newRows: ArraySpan<Scene.RowData>): void {
	if (oldRows.count === 0 && newRows.length === 0) return;
	
	const beforeStartRow = oldRows.row.Previous;
	const endRow = oldRows.endRow();
	
	for (const row of oldRows) {
		row.el.remove();
	}
	addBefore(endRow, newRows);
}

// Update rows with Scene.RowData array
export function updateRows(rowDataArray: ArraySpan<Scene.RowData>): void {
	let idx = 0;
	for (const row of rows()) {
		if (idx < rowDataArray.length && row.id === rowDataArray.at(idx).id) {
			row.setContent(rowDataArray.at(idx).content);
			idx++;
		}
	}
}
export function deleteRow(row: Row): void {
	row.el.remove();
}

export function addAfter(
	referenceRow: Row, 
	rowDataArray: ArraySpan<Scene.RowData>
): RowSpan {
	if (lm.editor === null) return new RowSpan(NoRow, 0);
	let count = 0;
	let beforeRow = referenceRow.el;
	let first = NoRow;
	for (const rowData of rowDataArray) {
		const el = createRowElement();
		el.dataset.lineId = rowData.id;
		new Row(el, 0).setContent(rowData.content);
		
		if (beforeRow.nextSibling) {
			lm.editor.insertBefore(el, beforeRow.nextSibling);
		} else {
			lm.editor.appendChild(el);
		}
		if (first === NoRow) first = new Row(el, 0);
		beforeRow = el;
		count++;
	}
	
	return new RowSpan(first, count);
}

export function deleteAfter(referenceRow: Row, count: number): void {
	let current = referenceRow.Next;
	for (let i = 0; i < count && current.valid(); i++) {
		const next = current.Next;
		current.el.remove();
		current = next;
	}
}
export function docName(): string {
	return lm.path.textContent ?? '';
}
export function setDocName(name: string): void {
	lm.path.textContent = name;
}
export function setContent(scene: ArraySpan<Scene.RowData>): RowSpan {    // Clear the Editor
	clear();

	// Create a Line element for each visible line
	let end = NoRow;
	return addBefore(end, scene);
}
