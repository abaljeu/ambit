import { Site, SiteRow, SiteRowId, SiteRowSubscriber } from './site.js';
import { Id, Pool } from './pool.js';
import * as Editor from './editor.js';
import { ArraySpan } from './arrayspan.js';
import * as Change from './change.js';
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
export class SceneRow extends SiteRowSubscriber {
    public readonly id: SceneRowId;
    public  get indent(): number { return this.siteRow.docLine.indent; }
    public subTreeLength: number = 1;
    // public static end = new SceneRow(NoScene,  SiteRow.end, new SceneRowId('R000000'));
    
    
    constructor(public  scene: Scene, public readonly siteRow: SiteRow, id: SceneRowId) {
        super();
        this.id = id;
        this.siteRow.subscribe(this);
    }
    
    public indexInScene(): number {
        return this.scene.rows.indexOf(this);
    }
    public siteRowsInserted(siteRow: SiteRow, offset: number, newSiteRows: SiteRow[]): void {
        // When our SiteRow changes, we need to rebuild this section of the scene
        if (siteRow !== this.siteRow) return;
        if (siteRow.folded) return;

        const newSceneRows: SceneRow[] = 
            newSiteRows.map(row => this.scene.createSceneRow(row));
        const start = this.indexInScene() + offset;
        this.scene.replaceRowsAndUpdateEditor(start, length, newSceneRows);
        this.subTreeLength += newSceneRows.length;
    }
    public siteRowUpdated(siteRow: SiteRow): void {
        // When our SiteRow changes, we need to rebuild this section of the scene
        if (siteRow !== this.siteRow) return;
        
        const oldLength = this.subTreeLength;
        const newRows: SceneRow[] = [];
        
        // Flatten only the CHILDREN, not the node itself
        // This node stays in place, we just update its descendants
        for (const child of siteRow.children) {
            this.scene._flattenRecursive(child, newRows);
        }
        
        // Update this row's length
        this.subTreeLength = 1 + newRows.reduce((sum, row) => sum + row.subTreeLength, 0);
        
        // Replace the OLD children (oldLength - 1 because oldLength includes this row)
        // with the NEW children (newRows)
        this.scene.replaceRowsAndUpdateEditor(this.indexInScene(), oldLength, [this, ...newRows]);
    }
    public siteRowsDeleting(siteRow: SiteRow, offset: number, count: number): void {
        if (siteRow !== this.siteRow) return;
        if (siteRow.folded) return;

        let deletedSubtreeLength = 0;
        for (let i = offset; i < offset + count; i++) {
            const child = siteRow.children[i];
            const row = this.scene.sceneRowPool.search((row: SceneRow) => row.siteRow === child);
            const index = row.indexInScene();
            if (index === -1) {
                // nothing
            } else {
                deletedSubtreeLength += row.subTreeLength;
                this.scene.replaceRowsAndUpdateEditor(index, row.subTreeLength, []);
            }
        }
        this.subTreeLength -= deletedSubtreeLength;
    }
    public get content(): string { return this.siteRow.docLine.content; }
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
    public  end = new SceneRow(this,  SiteRow.end, new SceneRowId('R000000'));
    private _updating = false;
    
    constructor() {}
    
    public replaceRowsAndUpdateEditor(start: number, length: number, newRows: SceneRow[]): void {
        // Prevent overlapping updates
        if (this._updating) return;
        this._updating = true;
        
        try {
            // Construct RowSpan for the old rows
            const startRow = Editor.at(start);
            const oldRowSpan = new Editor.RowSpan(startRow, length);
            
            // Construct ArraySpan for the new SceneRows
            const newRowSpan = new ArraySpan<SceneRow>(newRows, 0, newRows.length);
            
            // Replace in Editor
            Editor.replaceRows(oldRowSpan, newRowSpan);
            
            // Replace oldLength rows starting at index with newRows
            this._rows.splice(start, length, ...newRows);
        } finally {
            this._updating = false;
        }
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
    public _flattenRecursive(siteRow: SiteRow, result: SceneRow[]): void {
        // A SceneRow is visible if its parent is visible and not folded
        if (!siteRow.folded) {
            // Add the current node
            let row = this.sceneRowPool.search((row: SceneRow) => row.siteRow === siteRow);
            if (row === this.sceneRowPool.end) {
                row = this.createSceneRow(siteRow);
            } else {
                // Reuse existing row - don't re-subscribe
            }
            row.subTreeLength = 1;
            result.push(row);
            
            // Recursively add all children
            for (const child of siteRow.children) {
                this._flattenRecursive(child, result);
                row.subTreeLength += child.subTreeLength;
            }
            
        }
    }

    public createSceneRow(siteRow: SiteRow): SceneRow {
        return this.sceneRowPool.create(id => new SceneRow(this, siteRow, id));
    }
    public get rows(): readonly SceneRow[] { return this._rows; }
}
