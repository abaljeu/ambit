import * as lm from './elements.js';

export const RowElementTag: string = 'div';
type RowElement = HTMLDivElement;

export function createRowElement(): RowElement {
	const el = document.createElement(RowElementTag) as RowElement;
	
	const foldIndicator = document.createElement('span');
	foldIndicator.className = 'fold-indicator';
	foldIndicator.textContent = ' ';
	
	const content = document.createElement('span');
	content.className = 'content';
	content.contentEditable = 'true';
	
	el.appendChild(foldIndicator);
	el.appendChild(content);
	
	return el;
}

export class Row {
		constructor(public readonly el: RowElement, public readonly offset: number) {}
		private getContentSpan(): HTMLSpanElement {
			return this.el.querySelector('.content') as HTMLSpanElement;
		}
		private getFoldIndicatorSpan(): HTMLSpanElement {
			return this.el.querySelector('.fold-indicator') as HTMLSpanElement;
		}
		public get content(): string {
			const contentSpan = this.getContentSpan();
			return contentSpan?.textContent || '';
		}
		public setContent(value: string) {
			const contentSpan = this.getContentSpan();
			if (contentSpan) contentSpan.textContent = value;
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
		public get Id(): string {
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
			
			const tn = contentSpan.firstChild as Text | null;
			const text = tn?.textContent ?? '';
			const len = text.length;
			if (!tn || len === 0) return 0;
		
			let lastdist = x;
			let dist = x;
			for (let i = 1; i < len; i++) {
				const r = document.createRange();
				r.setStart(tn, i);
				r.collapse(true);
				const rect = r.getBoundingClientRect();
				dist = Math.abs(rect.left - x);
				if (dist > lastdist) 
					return i - 1;
				lastdist = dist;
			}
			return len;
		}
		public focus(): void {
			const contentSpan = this.getContentSpan();
			if (contentSpan) contentSpan.focus();
		}
	}

// Sentinel row used to indicate insertion at the start of the container
export const NoRow: Row = new Row(document.createElement(RowElementTag) as RowElement, 0);

// Create a new row and insert it after the given previous row.
// If previousRow is NoRow, insert at the front of the container.
export function addBefore(previousRow: Row, id: string, content: string): Row {
		if (lm.editor === null) return NoRow;
		const el = createRowElement();
		el.dataset.lineId = id;
		const contentSpan = el.querySelector('.content') as HTMLSpanElement;
		if (contentSpan) contentSpan.textContent = content;
		if (previousRow === NoRow) {
				// add to front of editor
				lm.editor.append(el);
			} else {
				lm.editor.insertBefore(el, previousRow.el);
			}
		return new Row(el, 0);
	}

function getCurrentParagraph(): RowElement | null {
		const selection = window.getSelection();
		if (!selection || selection.rangeCount === 0) return null;
		
		let node = selection.anchorNode;
		
		// Navigate up to find the row div
		while (node && node !== lm.editor) {
			if (node.nodeName === RowElementTag.toUpperCase() && 
				node.parentNode === lm.editor) {
				return node as RowElement;
			}
			node = node.parentNode;
		}
		
		return null;
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
		
		const textNode = contentSpan.firstChild as Text | null;
		if (!textNode) return { element: currentP, offset: 0 };
		
		// Create a range from start of content span to cursor position
		const offsetRange = document.createRange();
		offsetRange.setStart(textNode, 0);
		offsetRange.setEnd(range.startContainer, range.startOffset);
		
		// Count characters in the range
		const offset = offsetRange.toString().length;
		return { element: currentP, offset };
}

function setCaretInParagraph(contentSpan: HTMLElement, offset: number) {
		contentSpan.focus();
		const selection = window.getSelection();
		if (!selection) return;
		
		const range = document.createRange();
		const textNode = contentSpan.firstChild || contentSpan;
		range.setStart(textNode, offset);
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

	export function getContent(): string {
		// Extract text from all content spans
		const paragraphs = lm.editor.querySelectorAll(RowElementTag);
		const lines: string[] = [];
		
		paragraphs.forEach(p => {
			const contentSpan = p.querySelector('.content');
			lines.push(contentSpan?.textContent || '');
		});
		
		return lines.join('\n');
}

export function deleteRow(row: Row): void {
	row.el.remove();
}

export function addAfter(
	referenceRow: Row, 
	rows: Array<{id: string, content: string}>
): Row[] {
	if (lm.editor === null) return [];
	const addedRows: Row[] = [];
	let insertAfter = referenceRow.el;
	
	for (const rowData of rows) {
		const el = createRowElement();
		el.dataset.lineId = rowData.id;
		const contentSpan = el.querySelector('.content') as HTMLSpanElement;
		if (contentSpan) contentSpan.textContent = rowData.content;
		
		if (insertAfter.nextSibling) {
			lm.editor.insertBefore(el, insertAfter.nextSibling);
		} else {
			lm.editor.appendChild(el);
		}
		
		addedRows.push(new Row(el, 0));
		insertAfter = el;
	}
	
	return addedRows;
}

export function deleteAfter(referenceRow: Row, count: number): void {
	let current = referenceRow.Next;
	for (let i = 0; i < count && current.valid(); i++) {
		const next = current.Next;
		current.el.remove();
		current = next;
	}
}
