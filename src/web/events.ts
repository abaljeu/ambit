import * as lm from '../elements.js';

export function installEditorEvents(handlers: {
	onKeyDown: (event: KeyboardEvent) => void;
	onSave: () => void;
}): void {
	// Make newEditor focusable so it can receive keyboard events
	lm.newEditor.tabIndex = -1;

	lm.newEditor.addEventListener("keydown", handlers.onKeyDown);

	lm.newEditor.addEventListener('click', function(event: MouseEvent) {
		event.preventDefault();
	});
	
	lm.saveButton.onclick = handlers.onSave;
}

