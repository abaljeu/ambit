import { Doc, DocLine, noDoc, Subscriber } from './doc.js';
import * as Change from './change.js';
import { Id, Pool } from './pool.js';

// SiteRow ID - uses 'Sxxxxxx' format  
export class SiteRowId extends Id<'SiteRow'> {
    public constructor(value: string) {
        if (!/^S[0-9A-Z]{6}$/.test(value)) {
            throw new Error('Invalid DocLineId');
        }
         super(value); 
    }
}

export abstract class SiteRowSubscriber {
    abstract siteRowUpdated(siteRow: SiteRow): void;
    abstract siteRowsInserted(siteRow: SiteRow, offset: number, newRows: SiteRow[]): void;
    abstract siteRowsDeleting(siteRow: SiteRow, offset: number, count: number): void;
}

export class SiteRow extends Subscriber {
    public readonly id: SiteRowId;
    private _folded : boolean = false;
    public readonly docLine: DocLine;
    
    public static end = new SiteRow(DocLine.end, new SiteRowId('S000000'));
    // tree structure
    public readonly children: SiteRow[] = [];
    public parent: SiteRow = SiteRow.end;
    public subTreeLength: number = 1;
    
    public constructor(DocLine: DocLine, id: SiteRowId) {
        super();
        this.id = id;
        this.docLine = DocLine;
        this.docLine.subscribe(this);
    }
    public get hasChildren(): boolean { return this.children.length > 0; }
    
    // these lines could pre-exist or be trees
    public docLinesInserted(owner : DocLine, offset: number, lines: DocLine[]): void {
        if (owner !== this.docLine) return;

        const newRows: SiteRow[] = [];
        for (let i = offset; i < offset + lines.length; i++) {
            const docLine = owner.children[i];
            let found = siteRowPool.search((row: SiteRow) => row.docLine === docLine);
            if (found !== siteRowPool.end) {
                newRows.push(found);
            } else {
                let newRow = SiteRow._buildTreeRecursive(docLine);
                newRow.parent = this;
                newRows.push(newRow);
            }
        }
        this.children.splice(offset, 0, ...newRows);
        this.subTreeLength += newRows.reduce((sum, row) => sum + row.subTreeLength, 0);
        this._subscribers.forEach(subscriber => subscriber.siteRowsInserted(this, offset, newRows));
    }
    public docLinesDeleting(owner : DocLine, offset: number, count: number): void {
        if (owner !== this.docLine) return;
        
        // Calculate the total subtree length of children being deleted
        const deletedSubtreeLength = this.children
            .slice(offset, offset + count)
            .reduce((sum, child) => sum + child.subTreeLength, 0);
            
        this.children.splice(offset, count);
        this.subTreeLength -= deletedSubtreeLength;
        this._subscribers.forEach(subscriber => subscriber.siteRowsDeleting(this, offset, count));
    }
    public docLineSplitted(docLine: DocLine): void {
        // Verify this is the DocLine we're tracking
        if (docLine !== this.docLine) return;
        
        // Synchronize children: rebuild to match DocLine's children
        this._synchronizeChildren();
        
        // Update length propagates up the tree
        this._updateLength();
        
        this._subscribers.forEach(subscriber => subscriber.siteRowUpdated(this));
    }
    

    public get valid(): boolean { return this !== SiteRow.end; }
    public get folded(): boolean { return this._folded; }
    public addChild(child: SiteRow): void {
        child.parent = this;
        this.children.push(child);
        this._updateLength();
    }
    
    public removeChild(child: SiteRow): boolean {
        const index = this.children.indexOf(child);
        if (index === -1) return false;
        
        child.parent = SiteRow.end;
        this.children.splice(index, 1);
        this._updateLength();
        return true;
    }
    
    private _updateLength(): void {
        this.subTreeLength = 1 + this.children.reduce((sum, child) => sum + child.subTreeLength, 0);
    }
    
    private _synchronizeChildren(): void {
        // Defer notifications during synchronization
        
        this.children.length = 0;

        for (const line of this.docLine.children) {
            let found : SiteRow = siteRowPool.search((row: SiteRow) => row.docLine === line);
            if (found === SiteRow.end) {
                found = siteRowPool.create((id) => new SiteRow(line, id));
            }
            // Set parent and add directly without triggering notifications
            found.parent = this;
            this.children.push(found);
        }
        
        this._updateLength();
        
        // Only ONE notification after all children are synchronized
        this._subscribers.forEach(subscriber => subscriber.siteRowUpdated(this));
    }
    
    public _subscribers: SiteRowSubscriber[] = [];
    
    public subscribe(subscriber: SiteRowSubscriber): void {
        this._subscribers.push(subscriber);
    }
    
    public unsubscribe(subscriber: SiteRowSubscriber): void {
        this._subscribers = this._subscribers.filter(s => s !== subscriber);
    }
    
    
    public static _buildTreeRecursive(docLine: DocLine): SiteRow {
        // Create SiteRow with reference to this DocLine
        const siteRow = siteRowPool.create((id) => new SiteRow(docLine, id));
        // Recursively create children
        for (const child of docLine.children) {
            const childSiteRow = this._buildTreeRecursive(child);
            childSiteRow.parent = siteRow;
            siteRow.children.push(childSiteRow);
        }
        
        siteRow._updateLength();
        
        return siteRow;
    }
    

    
    
    public getDepth(): number {
        return this.parent !== SiteRow.end ? this.parent.getDepth() + 1 : 0;
    }
    
    public isLeaf(): boolean {
        return this.children.length === 0;
    }
    
    public getSiblings(): SiteRow[] {
        if (this.parent === SiteRow.end) return [this];
        return this.parent.children;
    }
}
export class SiteRowPool extends Pool<SiteRow, SiteRowId> {
    protected override fromString(value: string): SiteRowId {
        return new SiteRowId(value);
    }
    public get end(): SiteRow {
        return SiteRow.end;
    }
    public readonly tag: string = 'S';
}
const siteRowPool = new SiteRowPool();
export class Site {
    private _doc: Doc = noDoc; // The root doc
    private _root: SiteRow = SiteRow.end;
    
    public setDoc(doc: Doc): void {
        this._doc = doc;
        this.buildTree(doc.getRoot());
    }
    
    private buildTree(line: DocLine ): void {
        // Create SiteRow for each DocLine, matching the tree structure
        this._root = SiteRow._buildTreeRecursive(line);
    }
    public findRowByDocLine(docLine: DocLine): SiteRow {
        return siteRowPool.search((row: SiteRow) => row.docLine === docLine);
    }
    public getRoot(): SiteRow {
        return this._root;
    }
    
    public get doc(): Doc { return this._doc; }
    
    constructor() {}
}

