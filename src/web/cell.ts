import { 
	visibleOffsetToHtmlOffset, 
	htmlOffsetToVisibleOffset 
} from '../htmlutil.js';
import * as Dom from './editor-dom.js';
import { Row } from './row.js';
import * as Selection from './selection.js';
import { PureCell, PureCellKind } from './pureData.js';

export class Cell {
	constructor(public readonly newEl: Dom.CellElement) {

	}
	public get Row() : Row {
		return new Row(this.newEl.parentNode as Dom.RowElement);
	}
	public get isIndent(): boolean {
		return this.newEl.classList.contains(Dom.RowIndentClass);
	}
	public get isText(): boolean {
		return this.newEl.classList.contains(Dom.TextCellClass);
	}
	public get visibleText() : string {
		if (this.isIndent) {
			// Indent cells display a tab character
			return '	';
		}
		// Text cells return their actual text content
		return this.newEl.textContent ?? '';
	}
	public get visibleTextLength() : number {
		return this.visibleText.length;
	}
	public get htmlContent(): string {
		if (this.isIndent) {
			// Indent cells have a tab as their content
			return '	';
		}
		// Text cells return their innerHTML to preserve HTML structure
		return this.newEl.innerHTML;
	}

	public getHtmlOffset(visibleOffset: number): number {
		// Convert visible offset to HTML offset using stored HTML content
		return visibleOffsetToHtmlOffset(this.htmlContent, visibleOffset);
	}

	public caretOffset(): number {
		if (!this.isText) return 0;
		
		const selection = window.getSelection();
		if (!selection || selection.rangeCount === 0) return 0;
		
		const range = selection.getRangeAt(0);
		const focusNode = selection.focusNode;
		const focusOffset = selection.focusOffset;
		if (focusNode && this.containsNode(focusNode)) {
			return Dom.getHtmlOffsetFromNode(this.newEl, focusNode, focusOffset);
		}
		// Fallback to start if focusNode is not available or not in this cell
		if (this.containsNode(range.startContainer)) {
			return Dom.getHtmlOffsetFromNode(this.newEl, range.startContainer, range.startOffset);
		}
		return 0;
	}

	public setCaret(offset: number): void {
		this.setSelection(offset, offset);
	}

	public getHtmlSelectionRange(): { start: number, end: number } {
		if (!this.isText) return {start: 0, end: 0};
		
		const selection = window.getSelection();
		if (!selection || selection.rangeCount === 0) 
			return {start: 0, end: 0};
		
		const range = selection.getRangeAt(0);
		// Check if selection is in this cell
		if (!this.containsNode(range.startContainer) && !this.containsNode(range.endContainer)) {
			return {start: 0, end: 0};
		}
		
		const startOffset = this.containsNode(range.startContainer) 
			? Dom.getTextOffsetFromNode(this.newEl, range.startContainer, range.startOffset)
			: 0;
		const endOffset = this.containsNode(range.endContainer)
			? Dom.getTextOffsetFromNode(this.newEl, range.endContainer, range.endOffset)
			: this.visibleTextLength;
		
		return { start: startOffset, end: endOffset };
	}

	public setSelection(start: number, end: number): void {
		if (!this.isText) return;
		this.newEl.focus();
		Selection.setSelection(this, start, end);
		const selection = window.getSelection();
		if (!selection) return;
		
		// start and end are HTML offsets (including tag markup) in this.htmlContent.
		// Convert HTML offsets to visible-text offsets, then to node positions.
		const html = this.htmlContent;
		const startVisible = htmlOffsetToVisibleOffset(html, start);
		const endVisible = htmlOffsetToVisibleOffset(html, end);

		const startPos = Dom.getNodeAndOffsetFromTextOffset(
			this.newEl, 
			startVisible
		);
		const endPos = Dom.getNodeAndOffsetFromTextOffset(
			this.newEl, 
			endVisible
		);
		
		if (!startPos || !endPos) return;
		
		const range = document.createRange();
		// Ensure start <= end for the range
		// if (start <= end) {
			range.setStart(startPos.node, startPos.offset);
			range.setEnd(endPos.node, endPos.offset);
		// } else {
		// 	range.setStart(endPos.node, endPos.offset);
		// 	range.setEnd(startPos.node, startPos.offset);
		// }
		selection.removeAllRanges();
		selection.addRange(range);
	}

	public getAnchorOffset(): number {
		if (!this.isText) return 0;
		
		const selection = window.getSelection();
		if (!selection || selection.rangeCount === 0) {
			// No selection, anchor is same as caret
			return this.caretOffset();
		}
		
		const anchorNode = selection.anchorNode;
		const anchorOffset = selection.anchorOffset;
		if (anchorNode && this.containsNode(anchorNode)) {
			return Dom.getTextOffsetFromNode(this.newEl, anchorNode, anchorOffset);
		}
		
		// Fallback: check if selection is backwards using Selection.direction
		// If selection is backwards, return 0; otherwise return innerHTML length
		if (selection.direction === "backward") {
			return 0;
		}
		return this.htmlContent.length;
	}

	public extendSelectionInCell(cellLocalOffset: number): void {
		if (!this.isText) return;
		
		// Clamp offset to valid range within this cell
		const clampedOffset = Math.max(0, 
			Math.min(cellLocalOffset, this.visibleTextLength));
		
		// Get anchor offset within this cell
		const anchorOffset = this.getAnchorOffset();
		
		// Set selection from anchor to new offset within this cell
		this.setSelection(anchorOffset, clampedOffset);
	}

	public offsetAtX(x: number): number {
		const contentSpan = this.newEl;
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
			const position = Dom.getNodeAndOffsetFromTextOffset(contentSpan, i);
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

	private containsNode(node: Node): boolean {
		return this.newEl.contains(node);
	}
	public get active(): boolean {
		return this.newEl.classList.contains(Dom.CellBlockActiveClass);
		
	}


	public moveCaretToThisCell(x: number): void {
		if (!this.isText) return;
		// Find offset at X coordinate within this cell
		const offset = this.offsetAtX(x);
		// Set caret at that offset
		this.setCaret(offset);
	}

	// Update CSS classes based on CellBlock state
	// selectionState: { selected: boolean, active: boolean } for this cell
	public updateCellBlockStyling(selectionState: { selected: boolean, active: boolean }): void {
		if (selectionState.selected) {
			this.newEl.classList.add(Dom.CellBlockSelectedClass);
		} else {
			this.newEl.classList.remove(Dom.CellBlockSelectedClass);
		}
		
		if (selectionState.active) {
			this.newEl.classList.add(Dom.CellBlockActiveClass);
		} else {
			this.newEl.classList.remove(Dom.CellBlockActiveClass);
		}
	}

	// Check if this cell has the CellBlock selected CSS class
	public hasCellBlockSelected(): boolean {
		return this.newEl.classList.contains(Dom.CellBlockSelectedClass);
	}

	// Check if this cell has the CellBlock active CSS class
	public hasCellBlockActive(): boolean {
		return this.newEl.classList.contains(Dom.CellBlockActiveClass);
	}

	public toPureCell(): PureCell {
		const kind = this.isIndent ? PureCellKind.Indent : PureCellKind.Text;
		const text = this.visibleText;
		
		let width: number;
		if (this.isIndent) {
			width = 1;
		} else if (this.newEl.classList.contains(Dom.CellFlexClass)) {
			width = -1;
		} else {
			// Parse width from style.width (format: "Nem")
			const widthStyle = this.newEl.style.width;
			if (widthStyle && widthStyle.endsWith('em')) {
				const widthValue = parseFloat(widthStyle.slice(0, -2));
				width = isNaN(widthValue) ? 1 : widthValue;
			} else {
				width = 1;
			}
		}

		return new PureCell(kind, text, width);
	}
}


