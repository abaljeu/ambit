import { Site, SiteRow, SiteRowId, SiteRowSubscriber } from './site.js';
import { Id, Pool } from './pool.js';
import * as Editor from './editor.js';
import * as SceneEditor from './scene-editor.js';
import { ArraySpan } from './arrayspan.js';
import * as Change from './change.js';
import { CellBlock, CellSelection, CellTextSelection, NoSelection } from './cellblock.js';
import { PureRow, PureCellSelection, PureCell, PureCellKind, PureTextSelection, PureSelection } from './web/pureData.js';
import { model } from './model.js';
import { SceneCell, SceneRowCells } from './sitecells.js';
/*    => filter, flatten
    Scene
        SceneRow
        reference SiteNode
        compute visible
*/


export class Scene {
    private _rows: SiteRow[] = [];
    public readonly end = SiteRow.end;
    
    constructor(private readonly site: Site) {}
    
    public indexOf(row: SiteRow): number {
        const index = this._rows.indexOf(row);
        if (index < 0) {
            return this._rows.length;
        }
        return index;
    }
    public deleteRows(start: number, length: number): void {
        // Construct RowSpan for the old rows
        const startRow = Editor.at(start);
        const oldRowSpan = new Editor.RowSpan(startRow, length);
        
        // Remove from Editor (replace with empty)
        const emptyRowSpan = new ArraySpan<PureRow>([], 0, 0);
        Editor.replaceRows(oldRowSpan, emptyRowSpan);
        
        this._rows.splice(start, length);
    }

    public rowToPureRow(row: SiteRow): PureRow {
        return new PureRow(row.id, row.indent, 
            row.cells.toArray.map(cell => new PureCell(cell.type, cell.text, cell.width)));
        }

    public addRows(start: number, newSiteRows: SiteRow[]): void {
        let newSceneRows: SiteRow[] = [];           
        for (const child of newSiteRows) {
            this._flattenRecursive(child, newSceneRows);
        }
        
        this._rows.splice(start, 0, ...newSceneRows);
        
        const startRow = Editor.at(start);
        const editorRowSpan = new Editor.RowSpan(startRow, 0);
        SceneEditor.replaceRows(editorRowSpan, newSceneRows);
        
    }
    
    public at(index : number) : SiteRow {
        if (index < 0 || index >= this._rows.length)
            return this.end;
        return this._rows[index];
    }
    public loadFromSite(site: SiteRow): void {
        this._rows = this._flattenTree(site);
    }
    public findRow(id : string): SiteRow {
        return this.site.findRow(new SiteRowId(id));
    }

    private _flattenTree(siteRow: SiteRow): SiteRow[] {
        const result: SiteRow[] = [];
        this._flattenRecursive(siteRow, result);
        return result;
    }
    public search(predicate: (row: SiteRow) => boolean): SiteRow {
        return this.rows.find((row: SiteRow) => predicate(row)) ?? SiteRow.end;
    }

    public _flattenRecursive(siteRow: SiteRow, result: SiteRow[]): void {
        // A SiteRow is visible if its parent is visible and not folded
            // Add the current node
        result.push(siteRow);
        
        if (!siteRow.folded) {
            // Recursively add all children
            for (const child of siteRow.children) {
                this._flattenRecursive(child, result);
            }
            
        }
    }
    public get rows(): readonly SiteRow[] { return this._rows; }
    
    public getCellSelection(): CellSelection {
        return this.site.cellSelection;
    }
    
    public getSelectedSiteRows(): readonly SiteRow[] {
        const cellSelection = this.getCellSelection();
        if (cellSelection instanceof CellBlock) {
            return this._rows.filter(row => cellSelection.includesSiteRow(row));
        }
        return this._rows;
    }
    public rowUp(row: SiteRow): SiteRow {
        const index = this.indexOf(row);
        return this.at(index - 1);
    }
    public rowDown(row: SiteRow): SiteRow {
        const index = this.indexOf(row);
        return this.at(index + 1);
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
