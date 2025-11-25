import { Cell } from "./cell.js";
import { activeContent, activeContentCursor } from "./elements.js";
import * as HtmlUtil from "./htmlutil.js";
import { PureTextSelection } from "./pureData.js";

let _focus: number = 0;
let _anchor: number = 0;

export function setDetailView(text: string): void {
	activeContent.textContent = text;
}

export function getDetailView(): string {
	return activeContent.textContent ?? '';
}

export function setSelection(sel: Selection | null, focus: number, anchor: number): void {
	_focus = focus;
	_anchor = anchor;

	const text = activeContent.textContent ?? '';
	if (text.length === 0)
		return;

	if (!activeContent.firstChild)
		return;

	const cursor = activeContentCursor;
	if (!cursor)
		return;

	const range = new Range();
	const start = focus < text.length ? focus : text.length;
	const end = anchor < text.length ? anchor : text.length;
	range.setStart(activeContent.firstChild, start);
	range.setEnd(activeContent.firstChild, end);

	const rect = range.getBoundingClientRect();
	const container = activeContent.parentElement;
	if (!container)
		return;
	const containerRect = container.getBoundingClientRect();

	cursor.style.left = `${rect.left - containerRect.left}px`;
	cursor.style.top = `${rect.top - containerRect.top}px`;
	cursor.style.height = `${rect.height}px`;
    if (start === end) {
        cursor.style.animation = 'detailCaretBlink 1s step-end infinite';
    } else {
        cursor.style.animation = 'none';
    }
}
export function getSelection(): { focus: number, anchor: number } {
	return { focus: _focus, anchor: _anchor };
}

