import * as Model from './model.js';

export class RowData {
	public folded: boolean = false;
	public visible: boolean = true;

	// doc and line belong to Model.  Only Model may modify them.
	// Do not call members that modify them.
	constructor(
		public readonly refDoc: Model.Doc,
		public readonly refLine: Model.Line) {}
		
	public get id(): string { return this.refLine.id; }
	public get content(): string { return this.refLine.content; }

	public getIndentLevel(): number {
		let count = 0;
		for (const char of this.refLine.content) {
			if (char === '\t') count++;
			else break;
		}
		return count;
	}
}
export class Data {
	private _rows: RowData[];
	private _doc : Model.Doc;

	public EndOfScene() : RowData { 
		return new RowData(this._doc, this._doc.End());
	}
	constructor() {
		this._rows = [];
		this._doc = Model.createNoDoc();
	}
	public loadFromDoc(doc: Model.Doc): void {
		this._doc = doc;
		this._rows = doc.lines.map(l => new RowData(doc, l));
	}
	public get rows(): readonly RowData[] { return this._rows; }
	get length(): number { return this._rows.length; }
	findByLineId(id: string): RowData {
			return this._rows.find(r => r.id === id) 
			?? this.EndOfScene(); 
	}
	findIndexByLineId(id: string): number {
		let i = this._rows.findIndex(r => r.id === id);
		if (i === -1) return this._rows.length;
		return i;
	}
	updateRowData(id: string, content: string) {
		let row : RowData = this.findByLineId(id);
		Model.updateLineContent(row.refLine, content);
	}
	insertBefore(rowId: string, content: string): RowData {
		const targetRow = this.findByLineId(rowId);
		const newLine = targetRow.refDoc.insertBefore(rowId, content);
		const newRow = new RowData(targetRow.refDoc, newLine);
		const idx = this.findIndexByLineId(rowId);
		// 0 <= idx <= this._rows.length
		this._rows.splice(idx, 0, newRow);
		return newRow;
	}
	public deleteRow(row: RowData): void {
		this._rows.splice(this._rows.indexOf(row), 1);
		this._doc.deleteLine(row.id);
	}
	public nextRow(row : RowData): RowData {
		const idx = this.findIndexByLineId(row.id);
		if (idx === this._rows.length) return this.EndOfScene();
		return this._rows[idx + 1];
	}
	public hasChildren(row : RowData): boolean {
		return this.nextRow(row).getIndentLevel() > row.getIndentLevel();
	}
	public indentRowAndChildren(row : RowData): RowData[] {
		const rowAndChildren = [row, ...this.descendents(row)];
		for (const row of rowAndChildren) {
			const newContent = '\t' + row.content;
			Model.updateLineContent(row.refLine, newContent);
		}
		return rowAndChildren;
	}
	public deindentRowAndChildren(row : RowData): RowData[] {
		if (row.getIndentLevel() === 0) return [];
		
		const rowAndChildren = [row, ...this.descendents(row)];
		for (const row of rowAndChildren) {
			const newContent = row.content.substring(1);
			Model.updateLineContent(row.refLine, newContent);
		}
		return rowAndChildren;
	}
	public descendents(row : RowData): RowData[] {
		const baseIndent = row.getIndentLevel();
		const idx = this.findIndexByLineId(row.id);
		const affectedRows: RowData[] = [];
		for (let i = idx + 1; i < this._rows.length; i++) {
			const currentRow = this._rows[i];
			const currentIndent = currentRow.getIndentLevel();
			if (currentIndent <= baseIndent) break;
			affectedRows.push(currentRow);
		}
		return affectedRows;
	}
	public toggleFold(rowId: string): RowData[] {
		const row = this.findByLineId(rowId);
		const idx = this.findIndexByLineId(rowId);
		
		// Check if this row has any more-indented children
		const affectedRows = this.descendents(row);
		
		// If no children, do nothing
		if (affectedRows.length === 0) return [];
		
		// Toggle fold state
		row.folded = !row.folded;
		
		// Update visibility of affected rows
		for (const affectedRow of affectedRows) {
			affectedRow.visible = !row.folded;
		}
		
		return affectedRows;
	}
}
export const data = new Data();
