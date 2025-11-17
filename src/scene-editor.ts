import { SceneCell, SceneRow } from './scene.js';
import { ArraySpan } from './arrayspan.js';
import * as RowEditor from './web/row.js';
import { Cell } from './web/cell.js';
import { PureCell, PureRow, PureCellKind } from './web/pureData.js';

export type EditorRow = RowEditor.Row;
export type EditorRowSpan = RowEditor.RowSpan;

export function sceneCellToPureCell(sceneCell: SceneCell): PureCell {
	const kind = sceneCell.type === PureCellKind.Indent
		? PureCellKind.Indent
		: PureCellKind.Text;

	return new PureCell(kind, sceneCell.text, sceneCell.width);
}

export function sceneRowToPureRow(sceneRow: SceneRow): PureRow {
	const cells = sceneRow.cells.cells.map(sceneCellToPureCell);

	return new PureRow(sceneRow.id, sceneRow.indent, cells);
}

export function sceneCellMatchesEditorCell(sceneCell: SceneCell, editorCell: Cell): boolean {
	const scenePure = sceneCellToPureCell(sceneCell);
	const editorPure = editorCell.toPureCell();
	return scenePure.equals(editorPure);
}

export function sceneRowMatchesEditorRow(sceneRow: SceneRow, editorRow: RowEditor.Row): boolean {
	const scenePure = sceneRowToPureRow(sceneRow);
	const editorPure = editorRow.toPureRow();
	return scenePure.equals(editorPure);
}

// Thin Scene→Editor adapter. Converts SceneRow to PureRow before calling web layer.
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
