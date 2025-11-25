import { SceneCell } from './sitecells.js';

import { ArraySpan } from './arrayspan.js';
import * as RowEditor from './web/row.js';
import { Cell } from './web/cell.js';
import { PureCell, PureRow, PureCellKind } from './web/pureData.js';
import { RowSpan } from './web/row.js';
import { SiteRow } from './site.js';

export type EditorRow = RowEditor.Row;
export type EditorRowSpan = RowEditor.RowSpan;

export function sceneCellToPureCell(sceneCell: SceneCell): PureCell {
	const kind = sceneCell.type === PureCellKind.Indent
		? PureCellKind.Indent
		: PureCellKind.Text;

	return new PureCell(kind, sceneCell.text, sceneCell.width);
}

export function SiteRowToPureRow(SiteRow: SiteRow): PureRow {
	const cells = SiteRow.cells.toArray.map(sceneCellToPureCell);

	return new PureRow(SiteRow.id, SiteRow.indent, cells);
}
export function siteRowToPureRow(siteRow: SiteRow): PureRow {
	const cells = siteRow.cells.toArray.map(sceneCellToPureCell);

	return new PureRow(siteRow.id, siteRow.indent, cells);
}

export function sceneCellMatchesEditorCell(sceneCell: SceneCell, editorCell: Cell): boolean {
	const scenePure = sceneCellToPureCell(sceneCell);
	const editorPure = editorCell.toPureCell();
	return scenePure.equals(editorPure);
}

export function SiteRowMatchesEditorRow(SiteRow: SiteRow, editorRow: RowEditor.Row): boolean {
	const scenePure = SiteRowToPureRow(SiteRow);
	const editorPure = editorRow.toPureRow();
	return scenePure.equals(editorPure);
}

// Thin Scene→Editor adapter. Converts SiteRow to PureRow before calling web layer.
export function replaceRows(
	oldRows: RowEditor.RowSpan,
	newRows: SiteRow[]
): RowEditor.RowSpan {
	const pureRows = new ArraySpan(
		Array.from(newRows).map(SiteRowToPureRow),
		0,
		newRows.length
	);
	return RowEditor.replaceRows(oldRows, pureRows);
}

export function setEditorContent(scene: ArraySpan<SiteRow>)
	: RowSpan {
	const pureRows = new ArraySpan(
		Array.from(scene).map(SiteRowToPureRow),
		0,
		scene.length
	);
	return RowEditor.setEditorContent(pureRows);
}
