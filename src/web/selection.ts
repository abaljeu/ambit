import { SiteRowId } from '../site.js';
import { Cell } from './cell.js';
import * as Dom from './editor-dom.js';
import * as lm from './elements.js';
import { PureTextSelection } from './pureData.js';
import { Row, endRow } from './row.js';
import { rows } from './row.js';
import * as DetailView from './detailView.js';
import * as Editor from './row.js';
import * as Selection from './selection.js';

export function setSelection(cell: Cell, focus: number, anchor: number): void {
    const sel= window.getSelection();
    DetailView.setDetailView(cell.htmlContent);
	DetailView.setSelection(sel, focus, anchor);
}

function getCurrentParagraphWithOffset(): { element: Dom.RowElement, offset: number } | null {
	const selection = window.getSelection();
	if (!selection || selection.rangeCount === 0) return null;
	
	const range = selection.getRangeAt(0);
	let node = selection.anchorNode;

	// Navigate up to find the row div in newEditor container
	let currentP: Dom.RowElement | null = null;
	while (node && node !== lm.newEditor) {
		if (node.nodeName === Dom.RowElementTag.toUpperCase() &&
			node.parentNode === lm.newEditor) {
			currentP = node as Dom.RowElement;
			break;
		}
		node = node.parentNode;
	}

	if (!currentP) return null;

	// Calculate offset within content span
	const contentSpan = currentP.querySelector(`.${Dom.RowContentClass}`) as HTMLSpanElement;
	if (!contentSpan) return { element: currentP, offset: 0 };

	// Use helper to calculate text offset from DOM position
	const offset = Dom.getTextOffsetFromNode(
		contentSpan,
		range.startContainer,
		range.startOffset
	);

	return { element: currentP, offset };
}

export function currentRow(): Row {
	const p = getCurrentParagraphWithOffset();
	if (!p){
		for (const row of rows()) {
			if (row.valid()) {
				for (const cell of row.cells) {
					if (cell.active) {
						return row;
					}
				}
			}
		}
		 return endRow;
	}
	const parent = p.element.parentNode as Dom.RowElement | null;
	if (parent === lm.newEditor) {
		// p.element is from newEditor
		return new Row(p.element);
	}
	
	return endRow;
}
export function findCellContainingNode(subNode: Node) : Cell | null{
    let node = subNode;
    // Navigate up to find the cell element that contains the cursor
    let cellElement: Dom.CellElement | null = null;
    while (node && node !== lm.newEditor) {
        if (node.nodeType === Node.ELEMENT_NODE) {
            const el = node as HTMLElement;
            if (el.classList.contains(Dom.TextCellClass)) {
                cellElement = el as Dom.CellElement;
                break;
            }
        }
        if (!node.parentNode) {
            return null;
        }
        node = node.parentNode as Node;
    }
    if (!cellElement) {
        return null;
    }
    const targetCell = new Cell(cellElement);
    return targetCell;
}
export function currentSelection() : PureTextSelection | null{
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) {
        return null;
    }

    const range = selection.getRangeAt(0);
    let focusNode = selection.focusNode;
    if (!focusNode) {
        return null;
    }
    const cell = findCellContainingNode(focusNode!);
    if (!cell) {
        return null;
    }

    // Navigate up from cell to find the row div in newEditor container
    let rowElement: Dom.RowElement | null = null;
    let node: Node = cell.newEl;
    while (true) {
        if (node.parentNode === lm.newEditor) {
            rowElement = node as Dom.RowElement;
            break;
        }
        if (!node.parentNode) {
            return null;
        }
        node = node.parentNode as Node;
    }

    const row = new Row(rowElement);

    const focus = cell.caretOffset();
    const anchor = cell.getAnchorOffset();

    return new PureTextSelection(
        new SiteRowId(row.id),
        row.getCellIndex(cell),
        focus,
        anchor
    );
}
