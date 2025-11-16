import * as lm from './elements.js';

export function setMessage(message: string): void {
	lm.messageArea.textContent = message;
}

export function setPath(path: string): void {
	document.title = path + " - Ambit";
	lm.path.textContent = path;
}

