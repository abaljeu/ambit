import { Id, Pool, Poolable } from './pool.js';
import * as Change from './change.js';

// DocLine object
// DocLine ID - uses 'Dxxxxxx' format
export class DocLineId extends Id<'DocLine'> {
    public constructor(value: string) {
        if (!/^D[0-9A-Z]{6}$/.test(value)) {
            throw new Error('Invalid DocLineId');
        }
         super(value); 
    }
}

export class DocLine {
    public static end = new DocLine('', new DocLineId('D000000'));

    public readonly id: DocLineId;
    private _content: string;
    private _indent: number = 0;

    // DocTree structure
    private readonly _children: DocLine[] = [];
    private _parent: DocLine  = DocLine.end;
    private _subTreeLength: number = 1;

    // update infrastructure

    public constructor(content: string, id: DocLineId) {
        this.id = id;
        const { stripped, indent } = this._stripIndent(content);
        this._content = stripped;
        this._indent = indent;
    }

    public get indent(): number { return this._indent; }
    public setIndent(indent: number): void {
        this._indent = indent;
    }
    public get subTreeLength(): number { return this._subTreeLength; }
    public get parent(): DocLine { return this._parent; }
    public setParent(parent: DocLine): void {
        this._parent = parent;
    }
    public get children(): DocLine[] { return this._children; }

    public get content(): string { return this._content; }
    public static makeRoot(id: DocLineId, name: string): DocLine {
        const root = new DocLine('', id);
        root._indent = -1;
        root._content = name;
        return root;
    }
    private setContent(content: string): DocLine {
        const { stripped, indent } = this._stripIndent(content);
        this._content = stripped;
        this._indent = indent;
        return this;
    }
    private _stripIndent(content: string): { stripped: string; indent: number } {
        const match = content.match(/^(\t*)/);
        const indent = match ? match[1].length : 0;
        const stripped = content.substring(indent);
        return { stripped, indent };
    }
    
    private addChild(child: DocLine): void {
        child._parent = this;
        this._children.push(child);
        this._updateLength();
    }
    private addChildAfter(newChild: DocLine, after: DocLine): void {
        const index = this.children.indexOf(after);
        if (index === -1) return;
        newChild._parent = this;
        this._children.splice(index + 1, 0, newChild);
        this._updateLength();
    }
    public indexOf(line: DocLine): number {
        return this.children.indexOf(line);
    }
    public indexOrLast(line: DocLine): number {
        return this.indexOf(line) !== -1 ? this.indexOf(line) : this.children.length;
    }
    private removeChild(child: DocLine): boolean {
        const index = this.children.indexOf(child);
        if (index === -1) return false;
        
        child._parent = DocLine.end;
        this._children.splice(index, 1);
        this._updateLength();
        return true;
    }
    
    private _updateLength(): void {
        this._subTreeLength = 1 + this._children.reduce((sum, child) => sum + child._subTreeLength, 0);
        this.parent !== DocLine.end && this.parent._updateLength();
    }
    
    public getDepth(): number {
        return this.parent !== DocLine.end ? this.parent.getDepth() + 1 : 0;
    }
    
    public isLeaf(): boolean {
        return this.children.length === 0;
    }
    
    public getSiblings(): DocLine[] {
        if (this.parent === DocLine.end) return [this];
        return this.parent.children;
    }
    public isDocReference(): boolean { return false; }

    public split(offset: number): void {
        // split the text at the offset, creating a new DocLine after this.
        // Defer ALL notifications during split to batch the operation
        const parent = this.parent;
        
        const newContent = this.content.substring(offset);
        const newDocLine = docLinePool.create((id) => new DocLine(newContent, id));
        
        this.setContent(this.content.substring(0, offset));
        parent.addChildAfter(newDocLine, this);
        
       
        // Notify parent's subscribers ONCE - covers all changes
        parent._subscribers.forEach(subscriber => subscriber.docLineSplitted(parent));
    }

    public _subscribers: Subscriber[] = [];
    
    public subscribe(subscriber: Subscriber): void {
        this._subscribers.push(subscriber);
    }
    
    public unsubscribe(subscriber: Subscriber): void {
        this._subscribers = this._subscribers.filter(s => s !== subscriber);
    }
    public _buildSubTree(startIndex: number,  lines: DocLine[]): number {
        const line = lines[startIndex];
        line.children.length = 0;
        let nextIndex = startIndex + 1;
        
        while (nextIndex < lines.length) {
            const nextLine = lines[nextIndex];
            
            if (nextLine.indent > line.indent) {
                line.addChild(nextLine);
                nextIndex = this._buildSubTree(nextIndex, lines);
            } else {
                break;
            }
        }
        return nextIndex;
    }

}
export abstract class Subscriber { 
    abstract docLineSplitted(docLine: DocLine): void;
    abstract docLinesInserted(owner: DocLine, offset: number, lines: DocLine[]): void;
    abstract docLinesDeleting(owner: DocLine, offset: number, count: number): void;
}
export class DocLinePool extends Pool<DocLine, DocLineId> {
    protected override fromString(value: string): DocLineId {
        return new DocLineId(value);
    }
    public get end(): DocLine {
        return DocLine.end;
    }
    public readonly tag: string = 'D';

    public constructor() { super(); }
}
const docLinePool = new DocLinePool();

const docArray: Doc[] = [];

export class Doc {
    private _lines: DocLine[] = [];
    private _root: DocLine = DocLine.end;
    
    private constructor(public readonly name: string) {
        this.constructRoot(name);
    }
    private constructRoot(name: string): void {
        this._root = docLinePool.create((id) => DocLine.makeRoot(id, name))
        this._lines = [this._root];
    }
    public static create(content: string, name: string): Doc {
        const doc = new Doc(name);
        doc._setLines(content, name);
        doc.buildTree();
        return doc;
    }
    public updateContent(content: string): Doc {
        this.constructRoot(this.name);
        this._setLines(content, this.name);
        this.buildTree();
        return this;
    }
    public get lines(): DocLine[] { return this._lines; }

    public getRoot(): DocLine {
        return this._root;
    }
    public static get  end(): DocLine {
        return DocLine.end;
    }
    public findLine(id: DocLineId): DocLine {
        return docLinePool.find(id);
    }
    public buildTree(): void {
        if (this._lines.length === 0) {
            this._root = DocLine.end;
            return;
        }
        
        // First line (name) is always the root
        this._root = this._lines[0];

        this._root._buildSubTree(0, this._lines);
        
    }

    private _setLines(content: string, name: string): void {
        const lines = content.replace(/\r/g, '').split("\n");
        this._lines = [this._root,
            ...lines.map(line => docLinePool.create((id) => new DocLine(line, id)))
        ];
    }

    // Handler functions for each change type
    private _handleInsert(change: Change.Insert) : void{
        const newLines = change.lines.map(line => 
            docLinePool.create((id) => new DocLine(line, id)));
        const owner = this.findLine(change.owner);
        newLines.forEach(line => line.setIndent(owner.indent + 1));
        
        this._lines.splice(this._lines.indexOf(owner) + 1 + change.offset, 0, ...newLines);
        this._root._buildSubTree(this._lines.indexOf(owner), this._lines);
        owner._subscribers.forEach(subscriber => subscriber.docLinesInserted(owner, change.offset, newLines));
    }

    private _handleReinsert(change: Change.Reinsert) {
        console.log(`Reinserting ${change.lineIds.length} lines at offset ${change.offset}`);
    }

    private _handleDelete(change: Change.Delete) {
        console.log(`Deleting ${change.lines.length} lines at offset ${change.offset}`);
    }

    private _handleText(change: Change.Text) {
        console.log(`Changing text: ${change.oldText} -> ${change.newText}`);
    }

    private _handleMove(change: Change.Move) {
        const oldowner = this.findLine(change.oldowner);
        const newowner = this.findLine(change.newowner);

        oldowner._subscribers.forEach(subscriber => 
            subscriber.docLinesDeleting(oldowner, change.oldoffset, change.lines.length));

        const firstChild = oldowner.children[change.oldoffset];
        const movinglines = change.lines.map(lineid => this.findLine(lineid));
        const movingLength = movinglines.reduce((sum, line:DocLine) => sum + line.subTreeLength, 0);

        // Remove from _lines array
        this._lines.splice(this._lines.indexOf(firstChild), movingLength);

        // Remove from old owner's children
        const lines = oldowner.children.splice(change.oldoffset, change.lines.length);
        // lines should equal movinglines.  in future may generalize to any assorted lines moving.

        // Adjust newoffset for same-parent downward moves (after removal indices shift left)
        let adjustedNewOffset = change.newoffset;
        if (oldowner === newowner && change.oldoffset < change.newoffset) {
            adjustedNewOffset = change.newoffset - change.lines.length;
        }

        // Compute target offset in _lines array BEFORE modifying newowner.children
        let targetOffset: number;
        if (newowner.children.length === 0 || adjustedNewOffset === 0) {
            // No existing children, or inserting at beginning
            targetOffset = this._lines.indexOf(newowner) + 1;
        } else if (adjustedNewOffset >= newowner.children.length) {
            // Inserting at the end
            const lastChild = newowner.children[newowner.children.length - 1];
            targetOffset = this._lines.indexOf(lastChild) + lastChild.subTreeLength;
        } else {
            // Inserting in the middle - insert before the child currently at newoffset
            const insertBeforeChild = newowner.children[adjustedNewOffset];
            targetOffset = this._lines.indexOf(insertBeforeChild);
        }
        
        // Insert the moved lines back into _lines array at the correct position
        this._lines.splice(targetOffset, 0, ...movinglines);

        // Add to new owner's children and update their parent
        newowner.children.splice(adjustedNewOffset, 0, ...lines);
        lines.forEach(line => line.setParent(newowner));

        newowner._subscribers.forEach(subscriber => 
            subscriber.docLinesInserted(newowner, adjustedNewOffset, lines));
    }

    public processChange(change: Change.Change) {
        switch (change.type) {
            case Change.Type.Insert:
                this._handleInsert(change);
                break;
            case Change.Type.Reinsert:
                this._handleReinsert(change);
                break;
            case Change.Type.Delete:
                this._handleDelete(change);
                break;
            case Change.Type.Text:
                this._handleText(change);
                break;
            case Change.Type.Move:
                this._handleMove(change);
                break;
        }
    }    


}
export const noDoc = Doc.create('', '');