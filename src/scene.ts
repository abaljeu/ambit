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
    public  get indent(): number { return this.siteRow.indent; }
    public treeLength: number = 1;
    // public static end = new SceneRow(NoScene,  SiteRow.end, new SceneRowId('R000000'));
    
    public print() : void {
        console.log(`${this.id.toString()} (${this.indent}) ${this.siteRow.docLine.content}`);
    }
    constructor(public  scene: Scene, public readonly siteRow: SiteRow, id: SceneRowId) {
        super();
        this.id = id;
        this.siteRow.subscribe(this);
    }
    
    public indexInScene(): number {
        const index = this.scene.rows.indexOf(this);
        if (index === -1) return this.scene.rows.length;
        return index;
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
        this.scene.deleteRows(this.indexInScene(), this.treeLength);
    }
    public get content(): string { return this.siteRow.docLine.content; }
    public siteRowTextChanged(siteRow: SiteRow): void {
        if (siteRow !== this.siteRow) return;
         const r : Editor.Row = Editor.findRow(this.id.value);
         if (r !== Editor.endRow) 
            r.setContent(this.content);
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

    
    constructor() {}
    
    public deleteRows(start: number, length: number): void {
        // Construct RowSpan for the old rows
        const startRow = Editor.at(start);
        const oldRowSpan = new Editor.RowSpan(startRow, length);
        
        // Remove from Editor (replace with empty)
        const emptyRowSpan = new ArraySpan<SceneRow>([], 0, 0);
        Editor.replaceRows(oldRowSpan, emptyRowSpan);
        
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
        Editor.replaceRows(insertionRowSpan, new ArraySpan<SceneRow>(newSceneRows, 0, newSceneRows.length));
        
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

    public print(): void {
        console.log("Scene");
        for (const row of this._rows) {
            row.print();
        }
    }
    public findOrCreateSceneRow(siteRow: SiteRow): SceneRow {
        const row = this.sceneRowPool.search((row: SceneRow) => row.siteRow === siteRow);
        if (row !== this.sceneRowPool.end) return row;
        return this.sceneRowPool.create(id => new SceneRow(this, siteRow, id));
    }
    public get rows(): readonly SceneRow[] { return this._rows; }
}
