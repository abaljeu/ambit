import { SceneRow } from './scene.js';
import { ArraySpan } from './arrayspan.js';
import * as RowEditor from './web/row.js';

// Thin Scene→Editor adapter. For now these all delegate to the row-level editor.

export type EditorRow = RowEditor.Row;
export type EditorRowSpan = RowEditor.RowSpan;

export function createRowElementFromSceneRow(sceneRow: SceneRow)
	: RowEditor.Row {
	return RowEditor.createRowElementFromSceneRow(sceneRow);
}

// Create a new row and insert it after the given previous row.
// If previousRow is endRow, insert at the front of the container.
export function addBefore(
	targetRow: RowEditor.Row,
	scene: ArraySpan<SceneRow>
): RowEditor.RowSpan {
	return RowEditor.addBefore(targetRow, scene);
}

export function addAfter(
	referenceRow: RowEditor.Row,
	rowDataArray: ArraySpan<SceneRow>
): RowEditor.RowSpan {
	return RowEditor.addAfter(referenceRow, rowDataArray);
}

export function replaceRows(
	oldRows: RowEditor.RowSpan,
	newRows: ArraySpan<SceneRow>
): RowEditor.RowSpan {
	return RowEditor.replaceRows(oldRows, newRows);
}

export function setEditorContent(scene: ArraySpan<SceneRow>)
	: RowEditor.RowSpan {
	return RowEditor.setEditorContent(scene);
}
