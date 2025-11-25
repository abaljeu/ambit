import * as lm from '../web/elements.js';
import * as Controller from './controller.js';
import * as Cell from '../web/cell.js';

export function installEditorEvents(handlers: {
	onKeyDown: (event: KeyboardEvent) => void;
	onSave: () => void;
}): void {
	// Make newEditor focusable so it can receive keyboard events
	lm.newEditor.tabIndex = -1;

	lm.newEditor.addEventListener("keydown", handlers.onKeyDown);

	lm.newEditor.addEventListener('click', editorClick);
	lm.activeContent.addEventListener('click', activecontentClick);
	lm.saveButton.onclick = handlers.onSave;
}

export function editorClick(event: MouseEvent) {
	const target = event.target as HTMLElement;
	const x = event.clientX;
	const y = event.clientY;
	const result = Cell.Cell.fromXY(x, y);
	if (result) {
		Controller.moveCursorToCell(result.cell, result.offset, result.offset);
	}
}
export function activecontentClick(event: MouseEvent) {
	const target = event.target as HTMLElement;
	const x = event.clientX;
	const y = event.clientY;

	const pos = document.caretPositionFromPoint(x, y);
	if (pos === null)
		return;
	event.preventDefault();
	Controller.moveCursor(pos.offset, pos.offset);
}
