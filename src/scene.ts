import * as Model from './model.js';

export class Row {
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
	insertBefore(lineId: string, content: string): Row {
		const idx = this.findIndexByLineId(lineId);
		const newLine = this._doc.insertBefore(lineId, content);
		const newRow = new Row(this._doc, newLine);
		// 0 <= idx <= this._rows.length
		this._rows.splice(idx, 0, newRow);
		return newRow;
	}
}
const theContent = new Content();


export function getContent(): Content { return theContent; }