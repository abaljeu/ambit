import { Model } from './model.js';

export namespace Scene {
	export class Row {
		constructor(
			public readonly doc: Model.Doc,
			public readonly line: Model.Line
		) {}
	}

	export class Rows {
		private readonly _rows: Row[];
		constructor(public readonly doc: Model.Doc) {
			this._rows = doc.lines.map(l => new Row(doc, l));
		}
		get rows(): readonly Row[] { return this._rows; }
		get length(): number { return this._rows.length; }
		at(index: number): Row | undefined { return this._rows[index]; }
		findByLineId(id: string): Row | undefined  {
             return this._rows.find(r => r.line.id === id); 
        }
	}

	let currentRows: Rows | null = null;
	export function setFromDoc(doc: Model.Doc): Rows {
		const rows = new Rows(doc);
		currentRows = rows;
		return rows;
	}
	export function getCurrent(): Rows | null { return currentRows; }
}