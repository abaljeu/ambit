import { Site, SiteRow, SiteRowSubscriber } from './site.js';
import { Id, Pool } from './pool.js';
import * as Editor from './editor.js';
import * as SceneEditor from './scene-editor.js';
import { ArraySpan } from './arrayspan.js';
import * as Change from './change.js';
import { CellBlock } from './cellblock.js';
import { PureRow, PureCellSelection, PureCell, PureCellKind, PureTextSelection, PureSelection } from './web/pureData.js';
/*    => filter, flatten
    Scene
        SceneRow
        reference SiteNode
        compute visible
*/

export class SceneRowId extends Id<'SceneRow'> {
    public constructor(value: string) {
        if (!/^R[0-9A-Z]{6}$/.test(value)) {
            throw new Error('Invalid SceneRowId');
        }
        super(value);
    }
}
export class SceneCell {
    public constructor(public readonly type: string, public readonly text: string,
        public readonly column:number, public readonly width: number 
     ) {    }
     public get nextColumn(): number { return this.width == -1 ? -1 : this.column + this.width; }
}
export class SceneRowCells {
    private _cells: SceneCell[] = [];
    public get cells(): readonly SceneCell[] { return this._cells; }
    public constructor(public readonly source: string, public readonly indent: number) {
        for (let i = 0; i < this.indent; i++) {
            this._cells.push(new SceneCell('indent', '\t', i, 1));
        }
        let index = 0;
        const _cellText = this.source.split('\t');
        for (let i = 0; i < _cellText.length-1; i++) {
            const text = _cellText[i];
            this._cells.push(new SceneCell('text', text, index, text.length? 1: 1));
            index += 1;
        }
        this._cells.push(new SceneCell('text', _cellText[_cellText.length-1], index, -1));
    }
    public get count(): number { return this._cells.length; }
    public cell(index: number): SceneCell { return this._cells[index]; }
    public get text(): string { return this._cells.map(cell => cell.text).join('\t'); }

}
export type CellSelectionState = {
    cellIndex: number;
    selected: boolean;
    active: boolean;
};

export class SceneRow extends SiteRowSubscriber {
    public readonly id: SceneRowId;
    public  get indent(): number { return this.siteRow.indent; }
    public treeLength: number = 1;
    // public static end = new SceneRow(NoScene,  SiteRow.end, new SceneRowId('R000000'));
    private _cells: SceneRowCells | undefined;
    public get cells(): SceneRowCells {
        if (this._cells === undefined) {
            this._cells = new SceneRowCells(this.content, this.indent);
        }
        return this._cells;
    }
    constructor(public  scene: Scene, public readonly siteRow: SiteRow, id: SceneRowId) {
        super();
        this.id = id;
        this.siteRow.subscribe(this);
    }
    
    // Check if this SceneRow is part of the current CellBlock
    public isInCellBlock(): boolean {
        const cellBlock = this.scene.getCellBlock();
        return cellBlock.includesSiteRow(this.siteRow);
    }
    
    // Check if a specific cell index in this row is selected
    public isCellSelected(cellIndex: number): boolean {
        const cellBlock = this.scene.getCellBlock();
        return cellBlock.includesCell(this.siteRow, cellIndex);
    }
    
    // Check if a specific cell index is the active cell
    public isCellActive(cellIndex: number): boolean {
        const cellBlock = this.scene.getCellBlock();
        return cellBlock.isActiveCell(this.siteRow, cellIndex);
    }
    
    public getMaxColumnCount(sceneRows: readonly SceneRow[]): number {
        let maxColumns = 0;
        for (const sceneRow of sceneRows) {
            if (this.scene.getCellBlock().includesSiteRow(sceneRow.siteRow)) {
                const cellCount = sceneRow.cells.count;
                if (cellCount > maxColumns) {
                    maxColumns = cellCount;
                }
            }
        }
        return maxColumns;
    }

    public getCellSelectionStates(): readonly PureSelection[] {
        const cellBlock = this.scene.getCellBlock();
        const cellCount = this.cells.count;
        const states: PureSelection[] = [];
        
        let maxColumns = cellCount;
        if (cellBlock.endColumnIndex === -1) {
            const selectedRows = this.scene.getSelectedSceneRows();
            maxColumns = this.getMaxColumnCount(selectedRows);
        }
        
        for (let i = 0; i < cellCount; i++) {
            const selected = cellBlock.includesCell(this.siteRow, i);
            const active = cellBlock.isActiveCell(this.siteRow, i);
            states.push(new PureCellSelection(this.id, i, selected, active));
        }
        
        return states;
    }
    
    public indexInScene(): number {
        const index = this.scene.rows.indexOf(this);
        if (index === -1) return this.scene.rows.length;
        return index;
    }

    public siteRowFolded(): void {
        this.deleteChildren();
    }
    public siteRowUnfolded(): void {
        this.scene.addRows(this.indexInScene()+1, this.siteRow.children);
    }
    public findParent(): SceneRow {
        return this.scene.sceneRowPool.search((row: SceneRow) => row.siteRow === this.siteRow.parent);
    }
    public siteRowsInsertedBefore(newSiteRows: SiteRow[]): void {
        const sceneParent = this.scene.search(row => row.siteRow === this.siteRow.parent)
        if (sceneParent === this.scene.end) return;
        
        const selfIndex = this.indexInScene();
        this.scene.addRows(selfIndex, newSiteRows);

    }
    public siteRowsInsertedBelow(newSiteRows: SiteRow[]): void {
        if (this.siteRow.folded) return;
        if (this  === this.scene.end) return;
        
        const selfIndex = this.indexInScene();
        this.scene.addRows(selfIndex+this.treeLength, newSiteRows);
    }
    public siteRowRemoving(): void {
        this.deleteSelfAndChildren();
    }
    private deleteSelfAndChildren(): void {
        this.scene.deleteRows(this.indexInScene(), this.treeLength);
    }
    private deleteChildren(): void {
        this.scene.deleteRows(this.indexInScene()+1, this.treeLength-1);
    }

    public get content(): string { return this.siteRow.docLine.content; }
    public siteRowTextChanged(siteRow: SiteRow): void {
        if (siteRow !== this.siteRow)
            return;

        const r : Editor.Row = Editor.findRow(this.id.value);

        const oldCells = this.cells;
        this._cells = undefined;
        const newCells = this.cells;

        // todo: transfer cell selection to new cells.
        // for  range (a,b) and old length L, new length N
        // new range (c,d) will have L-b - N-d and b-a = d-c.
        // (then if c<0 then c=0.)
         if (r !== Editor.endRow) {
            // Convert SceneRow to PureRow
            const cells = newCells.cells.map(cell => 
                new PureCell(
                    cell.type === PureCellKind.Indent ? PureCellKind.Indent : PureCellKind.Text,
                    cell.text,
                    cell.width
                )
            );
            const pureRow = new PureRow(this.id, this.indent, cells);
            r.setContent(pureRow);
         }
    }
}

export class SceneRowPool extends Pool<SceneRow, SceneRowId> {
    protected override fromString(value: string): SceneRowId {
        return new SceneRowId(value);
    }
    public get end(): SceneRow {
        return this.context.end;
    }
    public readonly tag: string = 'R';

    public constructor(public context: Scene) { super(); }
}

export class Scene {
    public readonly sceneRowPool = new SceneRowPool(this);
    private _rows: SceneRow[] = [];
    public readonly end = new SceneRow(this,  SiteRow.end, new SceneRowId('R000000'));
    
    constructor(private readonly site: Site) {}
    
    public deleteRows(start: number, length: number): void {
        // Construct RowSpan for the old rows
        const startRow = Editor.at(start);
        const oldRowSpan = new Editor.RowSpan(startRow, length);
        
        // Remove from Editor (replace with empty)
        const emptyRowSpan = new ArraySpan<SceneRow>([], 0, 0);
        SceneEditor.replaceRows(oldRowSpan, emptyRowSpan);
        
        let self = this.at(start);
        let parent = self.findParent();
        while (parent !== this.end) {
            parent.treeLength -= length;
            parent = parent.findParent();
        }
        this._rows.splice(start, length);

    }

    public addRows(start: number, newSiteRows: SiteRow[]): void {
        let newSceneRows: SceneRow[] = [];           
        for (const child of newSiteRows) {
            this._flattenRecursive(child, newSceneRows);
        }
        
        this._rows.splice(start, 0, ...newSceneRows);
        
        const startRow = Editor.at(start);
        const insertionRowSpan = new Editor.RowSpan(startRow, 0);
        SceneEditor.replaceRows(insertionRowSpan, new ArraySpan<SceneRow>(newSceneRows, 0, newSceneRows.length));
        
        // Update parent lengths (increase by newRows.length)
        let newRow = this.at(start);
        let parent = newRow.findParent();
        while (parent !== this.end) {
            parent.treeLength += newSceneRows.length;
            parent = parent.findParent();
        }
    }
    
    public at(index : number) : SceneRow {
        if (index < 0 || index >= this._rows.length)
            return this.end;
        return this._rows[index];
    }
    
    public loadFromSite(site: SiteRow): void {
        this._rows = this._flattenTree(site);
    }
    public findRow(id : string): SceneRow {
        return this.sceneRowPool.find(this.sceneRowPool.makeIdFromString(id));
    }
    private _flattenTree(siteRow: SiteRow): SceneRow[] {
        const result: SceneRow[] = [];
        this._flattenRecursive(siteRow, result);
        return result;
    }
    public search(predicate: (row: SceneRow) => boolean): SceneRow {
        return this.rows.find((row: SceneRow) => predicate(row)) ?? this.end;
    }
    public _flattenRecursive(siteRow: SiteRow, result: SceneRow[]): void {
        // A SceneRow is visible if its parent is visible and not folded
        if (!siteRow.folded) {
            // Add the current node
            let row = this.sceneRowPool.search((row: SceneRow) => row.siteRow === siteRow);
            if (row === this.sceneRowPool.end) {
                row = this.findOrCreateSceneRow(siteRow);
            } else {
                row.siteRowTextChanged(siteRow);
                // Reuse existing row - don't re-subscribe
            }
            row.treeLength = 1;
            result.push(row);
            
            // Recursively add all children
            for (const child of siteRow.children) {
                this._flattenRecursive(child, result);
                row.treeLength += child.treeLength;
            }
            
        }
    }
    public findSceneRow(siteRow: SiteRow): SceneRow {
        const row = this.sceneRowPool.search((row: SceneRow) => row.siteRow === siteRow);
        return row;
    }

    public findOrCreateSceneRow(siteRow: SiteRow): SceneRow {
        const row = this.sceneRowPool.search((row: SceneRow) => row.siteRow === siteRow);
        if (row !== this.sceneRowPool.end) return row;
        return this.sceneRowPool.create(id => new SceneRow(this, siteRow, id));
    }
    public get rows(): readonly SceneRow[] { return this._rows; }
    
    public getCellBlock(): CellBlock {
        return this.site.cellBlock;
    }
    
    public getSelectedSceneRows(): readonly SceneRow[] {
        const cellBlock = this.getCellBlock();
        return this._rows.filter(row => cellBlock.includesSiteRow(row.siteRow));
    }
    
    // Called by controller after updating site selection
    public updatedSelection(): void {
        for (const row of this._rows) {
            const editorRow = Editor.findRow(row.id.toString());
            if (editorRow !== Editor.endRow) {
                const pureStates = row.getCellSelectionStates();
                editorRow.updateCellBlockStyling(pureStates);
            }
        }
        if (this.getCellBlock() !== CellBlock.empty) {
            Editor.removeCarets();
        }
    }
}
