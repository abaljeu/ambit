import { Site, SiteRow, SiteRowId, SiteRowSubscriber } from './site.js';
import { Id, Pool } from './pool.js';
import * as Editor from './editor.js';
import { ArraySpan } from './arrayspan.js';

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
export abstract class SceneRowSubscriber {
    abstract sceneRowUpdated(sceneRow: SceneRow): void;
}
export class SceneRow extends SiteRowSubscriber {
    public readonly id: SceneRowId;
    public length: number = 1;
    public _deferNotifications = false;
    // public static end = new SceneRow(NoScene,  SiteRow.end, new SceneRowId('R000000'));
    
    
    constructor(public  context: Scene, public readonly site: SiteRow, id: SceneRowId) {
        super();
        this.id = id;
        this.site.subscribe(this);
    }
    
    public siteRowUpdated(siteRow: SiteRow): void {
        // When our SiteRow changes, we need to rebuild this section of the scene
        if (siteRow !== this.site) return;
        if (this._deferNotifications) return;
        
        const oldLength = this.length;
        const newRows: SceneRow[] = [];
        
        // Flatten only the CHILDREN, not the node itself
        // This node stays in place, we just update its descendants
        for (const child of siteRow.children) {
            this.context._flattenRecursive(child, newRows);
        }
        
        // Update this row's length
        this.length = 1 + newRows.reduce((sum, row) => sum + row.length, 0);
        
        // Replace the OLD children (oldLength - 1 because oldLength includes this row)
        // with the NEW children (newRows)
        this.context.replaceRows(this, oldLength, [this, ...newRows]);
    }
    private _subscribers: SceneRowSubscriber[] = [];
    public subscribe(subscriber: SceneRowSubscriber): void {
        this._subscribers.push(subscriber);
    }
    public unsubscribe(subscriber: SceneRowSubscriber): void {
        this._subscribers = this._subscribers.filter(s => s !== subscriber);
    }
    private _notifySubscribers(): void {
        if (!this._deferNotifications) {
            this._subscribers.forEach(subscriber => subscriber.sceneRowUpdated(this));
        }
    }
    public get content(): string { return this.site.docLine.content; }
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
    
    public replaceRows(sceneRow: SceneRow, oldLength: number, newRows: SceneRow[]): void {
        // Prevent overlapping updates
        if (this._updating) return;
        this._updating = true;
        
        try {
            const index = this._rows.indexOf(sceneRow);
            if (index === -1) return; // Not found in the array
            
            // Defer notifications on all rows that will be replaced
            for (let i = index; i < index + oldLength && i < this._rows.length; i++) {
                this._rows[i]._deferNotifications = true;
            }
            
            // Construct RowSpan for the old rows
            const startRow = Editor.at(index);
            const oldRowSpan = new Editor.RowSpan(startRow, oldLength);
            
            // Construct ArraySpan for the new SceneRows
            const newRowSpan = new ArraySpan<SceneRow>(newRows, 0, newRows.length);
            
            // Replace in Editor
            Editor.replaceRows(oldRowSpan, newRowSpan);
            
            // Replace oldLength rows starting at index with newRows
            this._rows.splice(index, oldLength, ...newRows);
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
            let row = this.sceneRowPool.search((row: SceneRow) => row.site === siteRow);
            if (row === this.sceneRowPool.end) {
                row = this.sceneRowPool.create(id => new SceneRow(this, siteRow, id));
            } else {
                // Reuse existing row - don't re-subscribe
                row._deferNotifications = true;
            }
            row.length = 1;
            result.push(row);
            
            // Recursively add all children
            for (const child of siteRow.children) {
                this._flattenRecursive(child, result);
                row.length += child.length;
            }
            
            row._deferNotifications = false;
        }
    }


    
    public get rows(): readonly SceneRow[] { return this._rows; }
}
