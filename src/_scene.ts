// import { RowId, endRowId } from './rowid.js';
// import * as Model from './_model.js';
// import {  ArraySpan } from './arrayspan.js';
// import * as HtmlUtil from './htmlutil.js';

// export class RowData {
// 	public folded: boolean = false;
// 	public visible: boolean = true;

// 	// doc and line belong to Model.  Only Model may modify them.
// 	// Do not call members that modify them.
// 	constructor(
// 		public readonly refDoc: Model.Doc,
// 		public readonly refLine: Model.Line) {}
		
// 	public get id(): RowId { return this.refLine.id; }
// 	public get content(): string { return this.refLine.content; }

// 	public getIndentLevel(): number {
// 		let count = 0;
// 		for (const char of this.refLine.content) {
// 			if (char === '\t') count++;
// 			else break;
// 		}
// 		return count;
// 	}
// 	public get valid(): boolean { return this.refLine.valid; }
// }

// export class Data {
// 	private _rows: RowData[];
// 	private _doc : Model.Doc;

// 	public EndOfScene() : RowData { 
// 		return new RowData(this._doc, this._doc.End());
// 	}
// 	constructor() {
// 		this._rows = [];
// 		this._doc = Model.createNoDoc();
// 	}
// 	public loadFromDoc(doc: Model.Doc): void {
// 		this._doc = doc;
// 		this._rows = doc.lines.map(l => new RowData(doc, l));
// 	}
// 	public get rows(): readonly RowData[] { return this._rows; }
// 	get length(): number { return this._rows.length; }
// 	findByLineId(id: RowId): RowData {
// 			return this._rows.find(r => r.id.equals(id)) 
// 			?? this.EndOfScene(); 
// 	}
// 	findIndexByLineId(id: RowId): number {
// 		let i = this._rows.findIndex(r => r.id.equals(id));
// 		if (i === -1) return this._rows.length;
// 		return i;
// 	}
// 	updateRowData(id: RowId, content: string) {
// 		let row : RowData = this.findByLineId(id);
// 		Model.updateLineContent(row.refLine, content);
// 	}
// 	public at(index : number): RowData {
// 		if (index < 0 || index >= this._rows.length) return this.EndOfScene();
// 		return this._rows[index];
// 	}
// 	splitRow(rowId: RowId, offset: number): ArraySpan<RowData> {
// 		const currentRowIndex = this.findIndexByLineId(rowId);
// 		const currentRow = this.at(currentRowIndex);
// 		const nextRow = this.at(currentRowIndex + 1);

// 		const currentRowNewContent = currentRow.content.substring(0, offset);
// 		const newRowContent = currentRow.content.substring(offset);
// 		const fixedCurrentRowNewContent = HtmlUtil.fixTags(currentRowNewContent);
// 		const fixedNewRowContent = 
// 			`\t`.repeat(currentRow.getIndentLevel())
// 			 + HtmlUtil.fixTags(newRowContent);
// 		this.updateRowData(currentRow.id, fixedCurrentRowNewContent);
// 		const newSceneRow : RowData = this.insertBefore(nextRow.id, fixedNewRowContent);
// 		return new ArraySpan<RowData>(this._rows, currentRowIndex, currentRowIndex + 2);
// 	}
// 	insertBefore(rowId: RowId, content: string): RowData {
// 		const targetRow = this.findByLineId(rowId);
// 		const newLine = targetRow.refDoc.insertBefore(rowId, content);
// 		const newRow = new RowData(targetRow.refDoc, newLine);
// 		const idx = this.findIndexByLineId(rowId);
// 		// 0 <= idx <= this._rows.length
// 		this._rows.splice(idx, 0, newRow);
// 		return newRow;
// 	}
// 	public  joinRows(prevRowId: RowId, nextRowId: RowId): RowData {
// 		const prevRow = this.findByLineId(prevRowId);
// 		const nextRow = this.findByLineId(nextRowId);
// 		const nextContent = nextRow.content.substring(nextRow.getIndentLevel()); // remove tabs
// 		const newContent = prevRow.content + nextContent;
// 		this.updateRowData(prevRowId, newContent);
// 		this.deleteRow(nextRow);
// 		return prevRow;
// 	}
// 	public deleteRow(row: RowData): void {
// 		this._rows.splice(this._rows.indexOf(row), 1);
// 		this._doc.deleteLine(row.id);
// 	}
// 	public nextRow(row : RowData): RowData {
// 		const idx = this.findIndexByLineId(row.id);
// 		if (idx === this._rows.length) return this.EndOfScene();
// 		return this._rows[idx + 1];
// 	}
// 	public hasChildren(row : RowData): boolean {
// 		return this.nextRow(row).getIndentLevel() > row.getIndentLevel();
// 	}
// 	public indentRowAndChildren(row : RowData): ArraySpan<RowData> {
// 		const rowAndChildren = this.descendents(row).createSubspan(-1); // grow left
// 		for (const row of rowAndChildren) {
// 			const newContent = '\t' + row.content;
// 			Model.updateLineContent(row.refLine, newContent);
// 		}
// 		return rowAndChildren;
// 	}
// 	public deindentRowAndChildren(row : RowData): ArraySpan<RowData> {
// 		if (row.getIndentLevel() === 0) return ArraySpan.NoSpan;
		
// 		const rowAndChildren = this.descendents(row).createSubspan(-1); // grow left
// 		for (const row of rowAndChildren) {
// 			const newContent = row.content.substring(1);
// 			Model.updateLineContent(row.refLine, newContent);
// 		}
// 		return rowAndChildren;
// 	}
// 	public descendents(row : RowData): ArraySpan<RowData> {
// 		const baseIndent = row.getIndentLevel();
// 		const idx = this.findIndexByLineId(row.id);
// 		let i = idx + 1; 
// 		for (;i < this._rows.length; i++) {
// 			const currentRow = this._rows[i];
// 			const currentIndent = currentRow.getIndentLevel();
// 			if (currentIndent <= baseIndent) break;
// 		}
// 		return new ArraySpan(this._rows, idx + 1, i);
// 	}
// 	public toggleFold(rowId: RowId): ArraySpan<RowData> {
// 		const row = this.findByLineId(rowId);
// 		const idx = this.findIndexByLineId(rowId);
		
// 		// Check if this row has any more-indented children
// 		const affectedRows = this.descendents(row);
		
// 		// If no children, do nothing
// 		if (affectedRows.length === 0) return affectedRows;
		
// 		// Toggle fold state
// 		row.folded = !row.folded;
		
// 		// Update visibility of affected rows
// 		for (const affectedRow of affectedRows) {
// 			affectedRow.visible = !row.folded;
// 		}
		
// 		return affectedRows;
// 	}
// }
// export const data = new Data();
