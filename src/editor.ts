import * as lm from './elements.js';
export namespace Editor {
	export const RowElementTag: string = 'div';
	type RowElement = HTMLDivElement;
	export function createRowElement(): RowElement {
		const el = document.createElement(RowElementTag) as RowElement;
		el.contentEditable = 'true';
		return el;
	}

	export class Row {
		constructor(public readonly el: RowElement) {}
		public get Previous(): Row {
			const previousSibling = this.el?.previousElementSibling;
			if (!previousSibling) return NoRow;
			return new Row(previousSibling as RowElement);
		}
		public get Next(): Row {
			const nextSibling = this.el?.nextElementSibling;
			if (!nextSibling) return NoRow;
			return new Row(nextSibling as RowElement);
		}
		public valid(): boolean {
			return this.el !== null;
		}
		public get Id(): string {
			return this.el?.dataset.lineId ?? "000000";
		}
		public setCaretInRow(offset: number) {
			setCaretInParagraph(this.el, offset);
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
		public offset(): number {
			// should be more direct; using range.startOffset
			return this.offsetAtX(caretX());
		}
		public offsetAtX(x: number): number {
			const tn = this.el.firstChild as Text | null;
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
			this.el.focus();
		}
	}

	// Sentinel row used to indicate insertion at the start of the container
	export const NoRow: Row = new Row(document.createElement(RowElementTag) as RowElement);

	// Create a new row and insert it after the given previous row.
	// If previousRow is NoRow, insert at the front of the container.
	export function addBefore(previousRow: Row, id: string, content: string): Row {
		if (lm.editor === null) return NoRow;
		const el = createRowElement();
		el.dataset.lineId = id;
		el.textContent = content;
		if (previousRow === NoRow) {
				// add to front of editor
				lm.editor.append(el);
			} else {
				lm.editor.insertBefore(el, previousRow.el);
			}
			return new Row(el);
		}
	
	function getCurrentParagraph(): RowElement | null {
		const selection = window.getSelection();
		if (!selection || selection.rangeCount === 0) return null;
		
		let currentP = selection.anchorNode;
		while (currentP && currentP.nodeName !== RowElementTag.toUpperCase()){
			currentP = currentP.parentNode;
		}
		
		if (!currentP || currentP.parentNode !== lm.editor) 
			return null;
		return currentP as RowElement;
	}

	function setCaretInParagraph(p: RowElement, offset: number) {
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

	export function CurrentRow(): Row {
		let p = getCurrentParagraph() ;
		return p ? new Row(p as RowElement) : NoRow;
	}

	// In `Editor` namespace
export function moveBefore(toMove: Row, target: Row): void {
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
		// Extract text from all Line elements
		const paragraphs = lm.editor.querySelectorAll(RowElementTag);
		const lines: string[] = [];
		
		paragraphs.forEach(p => {
			lines.push(p.textContent || '');
		});
		
		return lines.join('\n');

	}

}