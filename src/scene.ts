import * as Model from './model.js';

export class Row {
	public fold: boolean = false;
	public visible: boolean = true;

	constructor(
		public readonly doc: Model.Doc,
		public readonly line: Model.Line	)		{}
		public get Id(): string {
			return this.line.id;
		}
		public get Content(): string {
			return this.line.content;
		}
	public updateLineContent(content: string) {
		this.line.updateContent(content);
	}
	public getIndentLevel(): number {
		let count = 0;
		for (const char of this.line.content) {
			if (char === '\t') count++;
			else break;
		}
		return count;
	}
}
export class Content {
	private _rows: Row[];
	private _doc : Model.Doc;

	public EndOfScene() : Row { 
		return new Row(this._doc, this._doc.End());
	}
	constructor() {
		this._rows = [];
		this._doc = Model.createNoDoc();
	}
	public loadFromDoc(doc: Model.Doc): void {
		this._doc = doc;
		this._rows = doc.lines.map(l => new Row(doc, l));
	}
	get rows(): readonly Row[] { return this._rows; }
	get length(): number { return this._rows.length; }
	findByLineId(id: string): Row {
			return this._rows.find(r => r.line.id === id) 
			?? this.EndOfScene(); 
	}
	findIndexByLineId(id: string): number {
		let i = this._rows.findIndex(r => r.line.id === id);
		if (i === -1) return this._rows.length;
		return i;
	}
	updateLineContent(id: string, content: string) {
		this.findByLineId(id)
			.updateLineContent(content);
	}
	insertBefore(rowId: string, content: string): Row {
		const targetRow = this.findByLineId(rowId);
		const newLine = targetRow.doc.insertBefore(rowId, content);
		const newRow = new Row(targetRow.doc, newLine);
		const idx = this.findIndexByLineId(rowId);
		// 0 <= idx <= this._rows.length
		this._rows.splice(idx, 0, newRow);
		return newRow;
	}
	public deleteRow(row: Row): void {
		this._rows.splice(this._rows.indexOf(row), 1);
		this._doc.deleteLine(row.Id);
	}
	public toggleFold(rowId: string): Row[] {
		const row = this.findByLineId(rowId);
		const idx = this.findIndexByLineId(rowId);
		
		// Check if this row has any more-indented children
		const baseIndent = row.getIndentLevel();
		const affectedRows: Row[] = [];
		
		// Find all rows that should be affected
		for (let i = idx + 1; i < this._rows.length; i++) {
			const currentRow = this._rows[i];
			const currentIndent = currentRow.getIndentLevel();
			
			// Stop when we hit a row at same or less indentation
			if (currentIndent <= baseIndent) break;
			
			affectedRows.push(currentRow);
		}
		
		// If no children, do nothing
		if (affectedRows.length === 0) return [];
		
		// Toggle fold state
		row.fold = !row.fold;
		
		// Update visibility of affected rows
		for (const affectedRow of affectedRows) {
			affectedRow.visible = !row.fold;
		}
		
		return affectedRows;
	}
}
const theContent = new Content();


export function getContent(): Content { return theContent; }