import { Site, SiteRow, SiteRowId, SiteRowSubscriber } from './site.js';
import { Id, Pool } from './pool.js';
import * as Editor from './editor.js';
import * as SceneEditor from './scene-editor.js';
import { ArraySpan } from './arrayspan.js';
import * as Change from './change.js';
import { CellBlock, CellSelection, CellTextSelection } from './cellblock.js';
import { PureRow, PureCellSelection, PureCell, PureCellKind, PureTextSelection, PureSelection } from './web/pureData.js';
import { model } from './model.js';
import { SceneRowCells } from './sitecells.js';
/*    => filter, flatten
    Scene
        SceneRow
        reference SiteNode
        compute visible
*/

export class SceneRow extends SiteRowSubscriber {
    public readonly id: SiteRowId;
    public  get indent(): number { return this.siteRow.indent; }
    public treeLength: number = 1;
    // public static end = new SceneRow(NoScene,  SiteRow.end, new SiteRowId('S000000'));
    public get cells(): SceneRowCells {
        return this.siteRow.cells;
    }
    constructor(public  scene: Scene, public readonly siteRow: SiteRow, id: SiteRowId) {
        super();
        this.id = id;
        this.siteRow.subscribe(this);
    }
    public get valid(): boolean { return this.siteRow !== SiteRow.end; }
    // Check if a specific cell index in this row is selected
    public isCellSelected(cellIndex: number): boolean {
        const cellSelection = this.scene.getCellSelection();
        if (cellSelection instanceof CellBlock) {
            return cellSelection.includesCell(this.siteRow, cellIndex);
        }
        return false;
    }
    
    
    public getCellSelectionStates(): readonly PureSelection[] {
        const sceneSelection : CellSelection = this.scene.getCellSelection();
        const states: PureSelection[] = [];
            const cellCount = this.cells.count;
            
            for (let i = 0; i < cellCount; i++) {
                if (sceneSelection instanceof CellBlock) {
                    const _selected = sceneSelection.includesCell(this.siteRow, i);
                    const active = sceneSelection.isActiveCell(this.siteRow, i);
                    states.push(new PureCellSelection(this.id, i, _selected, active));
                } else if (sceneSelection instanceof CellTextSelection) {
                    const thisOne : boolean = (sceneSelection.row === this.siteRow)
                        && (sceneSelection.cellIndex === i);
                        if (thisOne) {
                            states.push(new PureTextSelection(this.id, sceneSelection.cellIndex, sceneSelection.focus, sceneSelection.anchor));
                        } else 
                            states.push(new PureCellSelection(this.id, i, false, false));
                            continue;
                    }
                }
            return states;
    }
    
    public indexInScene(): number {
        const index = this.scene.rows.indexOf(this);
        if (index === -1) return this.scene.rows.length;
        return index;
    }
    public get previous(): SceneRow {
        return this.scene.at(this.indexInScene()-1);
    }
    public get next(): SceneRow {
        return this.scene.at(this.indexInScene()+1);
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

        // todo: transfer cell selection to new cells.
        // for  range (a,b) and old length L, new length N
        // new range (c,d) will have L-b - N-d and b-a = d-c.
        // (then if c<0 then c=0.)
         if (r !== Editor.endRow) {
            // Convert SceneRow to PureRow
            const cells = this.cells.cells.map(cell => 
                new PureCell(
                    cell.type,
                    cell.text,
                    cell.width
                )
            );
            const pureRow = new PureRow(this.id, this.indent, cells);
            r.setContent(pureRow);
         }
    }
}

export class SceneRowPool extends Pool<SceneRow, SiteRowId> {
    protected override fromString(value: string): SiteRowId {
        return new SiteRowId(value);
    }
    public get end(): SceneRow {
        return this.context.end;
    }
    public readonly tag: string = 'S';

    public constructor(public context: Scene) { super(); }
}

export class Scene {
    public readonly sceneRowPool = new SceneRowPool(this);
    private _rows: SceneRow[] = [];
    public readonly end = new SceneRow(this,  SiteRow.end, SiteRow.end.id);
    
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
    
    public getCellSelection(): CellSelection {
        return this.site.cellSelection;
    }
    
    public getSelectedSceneRows(): readonly SceneRow[] {
        const cellSelection = this.getCellSelection();
        if (cellSelection instanceof CellBlock) {
            return this._rows.filter(row => cellSelection.includesSiteRow(row.siteRow));
        }
        return this._rows;
    }
    
    private isTextSelection(state: PureSelection): boolean {
        return state instanceof PureTextSelection;
    }
    // Called by controller after updating site selection
    public updatedSelection(): void {
        let cursor = false;
        for (const row of this._rows) {
            const editorRow = Editor.findRow(row.id.value);
            if (editorRow !== Editor.endRow) {
                const pureStates = row.getCellSelectionStates();
                editorRow.updateCellBlockStyling(pureStates);
                if (pureStates.some(this.isTextSelection)) {
                    cursor = true;
                }
            }
        }
        if (!cursor) {
            Editor.removeCarets();
        }
    }
    
}
