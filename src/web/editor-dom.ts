export const RowElementTag: string = 'div';
export const RowContentTag: string = 'span';
export const RowContentClass: string = 'rowContent';
export const RowIndentClass: string = 'indentation';
export const TextCellClass: string = 'editableCell';
export const CellBlockSelectedClass: string = 'cellBlock-selected';
export const CellBlockActiveClass: string = 'cellBlock-active';
export const CellFlexClass: string = 'cell-flex';
export const CellFixedClass: string = 'cell-fixed';
export const VISIBLE_TAB = '→'; // Visible tab character, used for internal tabs.
export const NOROWID = 'S000000';

export type CellElement = HTMLSpanElement;
export type RowElement = HTMLDivElement;

// Helper: Walk DOM tree and find node+offset for a given text offset
export function getNodeAndOffsetFromTextOffset(
	container: CellElement, 
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
export function getTextOffsetFromNode(
	container: CellElement, 
	targetNode: Node, 
	targetOffset: number
): number {
	let textOffset = 0;
	
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
export function getHtmlOffsetFromNode(
	container: CellElement, 
	targetNode: Node, 
	targetOffset: number
): number {
	const range = document.createRange();
	range.setStart(container, 0);
	range.setEnd(targetNode, targetOffset);

	const tmp = document.createElement('span');
	tmp.appendChild(range.cloneContents());

	return tmp.innerHTML.length;
}