import { PureCellKind } from './web/pureData.js';

// export class SiteRowId extends Id<'SceneRow'> {
    //     public constructor(value: string) {
    //         if (!/^R[0-9A-Z]{6}$/.test(value)) {
    //             throw new Error('Invalid SiteRowId');
    //         }
    //         super(value);
    //     }
    // }
    export class SceneCell {
        public constructor(public readonly type: PureCellKind, public readonly text: string,
            public readonly column:number, public readonly width: number 
         ) {    }
         public get nextColumn(): number { return this.width == -1 ? -1 : this.column + this.width; }
    }
    export class SceneRowCells {
        private _cells: SceneCell[] = [];
        public get toArray(): readonly SceneCell[] { return this._cells; }
        public constructor(public readonly source: string, public readonly indent: number) {
            for (let i = 0; i < this.indent; i++) {
                this._cells.push(new SceneCell(PureCellKind.Indent, '\t', i, 1));
            }
            let index = 0;
            const _cellText = this.source.split('\t');
            for (let i = 0; i < _cellText.length-1; i++) {
                const text = _cellText[i];
                this._cells.push(new SceneCell(PureCellKind.Text, text, index, text.length? 1: 1));
                index += 1;
            }
            this._cells.push(new SceneCell(PureCellKind.Text, _cellText[_cellText.length-1], index, -1));
        }
        public at(index: number): SceneCell 
        { 
            if (index < 0 || index >= this._cells.length)
                return this._cells[this._cells.length-1];
            return this._cells[index]; }
        public get count(): number { return this._cells.length; }
        public cell(index: number): SceneCell { return this._cells[index]; }
        public indexOf(cell: SceneCell): number { return this._cells.indexOf(cell); }
        public get text(): string { return this._cells.map(cell => cell.text).join('\t'); }
    
    }
    export type CellSelectionState = {
        cellIndex: number;
        selected: boolean;
        active: boolean;
    };
    