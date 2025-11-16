import { SceneCell, SceneRow } from './scene.js';
import { ArraySpan } from './arrayspan.js';
import * as RowEditor from './web/row.js';
import { Cell } from './web/cell.js';
import { PureCell, PureRow, PureCellKind } from './web/editorData.js';

export type EditorRow = RowEditor.Row;
export type EditorRowSpan = RowEditor.RowSpan;

export function sceneCellToPureCell(sceneCell: SceneCell): PureCell {
	const kind = sceneCell.type === PureCellKind.Indent
		? PureCellKind.Indent
		: PureCellKind.Text;

	return {
		kind,
		text: sceneCell.text,
		width: sceneCell.width,
	};
}

export function sceneRowToPureRow(sceneRow: SceneRow): PureRow {
	const cells = sceneRow.cells.cells.map(sceneCellToPureCell);

	return {
		id: sceneRow.id.value,
		indent: sceneRow.indent,
		cells,
	};
}

function pureCellEquals(a: PureCell, b: PureCell): boolean {
	return a.kind === b.kind &&
		a.text === b.text &&
		a.width === b.width;
}

function pureRowEquals(a: PureRow, b: PureRow): boolean {
	if (a.id !== b.id || a.indent !== b.indent || a.cells.length !== b.cells.length) {
		return false;
	}
	
	for (let i = 0; i < a.cells.length; i++) {
		if (!pureCellEquals(a.cells[i], b.cells[i])) {
			return false;
		}
	}
	
	return true;
}

export function sceneCellMatchesEditorCell(sceneCell: SceneCell, editorCell: Cell): boolean {
	const scenePure = sceneCellToPureCell(sceneCell);
	const editorPure = editorCell.toPureCell();
	return pureCellEquals(scenePure, editorPure);
}

export function sceneRowMatchesEditorRow(sceneRow: SceneRow, editorRow: RowEditor.Row): boolean {
	const scenePure = sceneRowToPureRow(sceneRow);
	const editorPure = editorRow.toPureRow();
	return pureRowEquals(scenePure, editorPure);
}

// Thin Scene→Editor adapter. Converts SceneRow to PureRow before calling web layer.

export function createRowElementFromSceneRow(sceneRow: SceneRow)
	: RowEditor.Row {
	const pureRow = sceneRowToPureRow(sceneRow);
	return RowEditor.createRowElementFromPureRow(pureRow);
}

// Create a new row and insert it after the given previous row.
// If previousRow is endRow, insert at the front of the container.
export function addBefore(
	targetRow: RowEditor.Row,
	scene: ArraySpan<SceneRow>
): RowEditor.RowSpan {
	const pureRows = new ArraySpan(
		Array.from(scene).map(sceneRowToPureRow),
		0,
		scene.length
	);
	return RowEditor.addBefore(targetRow, pureRows);
}

export function addAfter(
	referenceRow: RowEditor.Row,
	rowDataArray: ArraySpan<SceneRow>
): RowEditor.RowSpan {
	const pureRows = new ArraySpan(
		Array.from(rowDataArray).map(sceneRowToPureRow),
		0,
		rowDataArray.length
	);
	return RowEditor.addAfter(referenceRow, pureRows);
}

export function replaceRows(
	oldRows: RowEditor.RowSpan,
	newRows: ArraySpan<SceneRow>
): RowEditor.RowSpan {
	const pureRows = new ArraySpan(
		Array.from(newRows).map(sceneRowToPureRow),
		0,
		newRows.length
	);
	return RowEditor.replaceRows(oldRows, pureRows);
}

export function setEditorContent(scene: ArraySpan<SceneRow>)
	: RowEditor.RowSpan {
	const pureRows = new ArraySpan(
		Array.from(scene).map(sceneRowToPureRow),
		0,
		scene.length
	);
	return RowEditor.setEditorContent(pureRows);
}
