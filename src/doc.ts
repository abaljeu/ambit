import { Id, Pool, Poolable } from './pool.js';

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
    public readonly id: DocLineId;
    public static end = new DocLine('', new DocLineId('D000000'));
    private _content: string;
    // DocTree structure
    private readonly _children: DocLine[] = [];
    private _parent: DocLine  = DocLine.end;
    private _length: number = 1;
    private _indent: number = 0;
    public _deferNotifications = false;
    
    public get indent(): number { return this._indent; }
    public get length(): number { return this._length; }
    public get parent(): DocLine { return this._parent; }
    public get children(): DocLine[] { return this._children; }

    public get content(): string { return this._content; }
    public constructor(content: string, id: DocLineId) {
        this.id = id;
        const { stripped, indent } = this._stripIndent(content);
        this._content = stripped;
        this._indent = indent;
    }
    public static makeRoot(id: DocLineId, name: string): DocLine {
        const root = new DocLine('', id);
        root._indent = -1;
        root._content = name;
        return root;
    }
    public setContent(content: string): DocLine {
        const { stripped, indent } = this._stripIndent(content);
        this._content = stripped;
        this._indent = indent;
        this._notifySubscribers();
        return this;
    }
    private _stripIndent(content: string): { stripped: string; indent: number } {
        const match = content.match(/^(\t*)/);
        const indent = match ? match[1].length : 0;
        const stripped = content.substring(indent);
        return { stripped, indent };
    }
    
    public addChild(child: DocLine): void {
        child._parent = this;
        this._children.push(child);
        this._updateLength();
        if (!this._deferNotifications) {
            this._notifySubscribers();
        }
    }
    public addChildAfter(newChild: DocLine, after: DocLine): void {
        const index = this.children.indexOf(after);
        if (index === -1) return;
        newChild._parent = this;
        this._children.splice(index + 1, 0, newChild);
        this._updateLength();
        if (!this._deferNotifications) {
            this._notifySubscribers();
        }
    }
    
    public removeChild(child: DocLine): boolean {
        const index = this.children.indexOf(child);
        if (index === -1) return false;
        
        child._parent = DocLine.end;
        this._children.splice(index, 1);
        this._updateLength();
        if (!this._deferNotifications) {
            this._notifySubscribers();
        }
        return true;
    }
    
    private _updateLength(): void {
        this._length = 1 + this._children.reduce((sum, child) => sum + child._length, 0);
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
        this._deferNotifications = true;
        parent._deferNotifications = true;
        
        const newContent = this.content.substring(offset);
        const newDocLine = docLinePool.create((id) => new DocLine(newContent, id));
        newDocLine._deferNotifications = true;
        
        this.setContent(this.content.substring(0, offset));
        parent.addChildAfter(newDocLine, this);
        
        // Re-enable notifications
        this._deferNotifications = false;
        newDocLine._deferNotifications = false;
        parent._deferNotifications = false;
        
        // Notify parent's subscribers ONCE - covers all changes
        parent._subscribers.forEach(subscriber => subscriber.docLineUpdated(parent));
    }
    
    public _subscribers: Subscriber[] = [];
    
    public subscribe(subscriber: Subscriber): void {
        this._subscribers.push(subscriber);
    }
    
    public unsubscribe(subscriber: Subscriber): void {
        this._subscribers = this._subscribers.filter(s => s !== subscriber);
    }
    
    private _notifySubscribers(): void {
        if (!this._deferNotifications) {
            this._subscribers.forEach(subscriber => subscriber.docLineUpdated(this));
        }
    }
}
export abstract class Subscriber { 
    abstract docLineUpdated(docLine: DocLine): void;
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
    public buildTree(): void {
        if (this._lines.length === 0) {
            this._root = DocLine.end;
            return;
        }
        
        // First line (name) is always the root
        this._root = this._lines[0];
        this._root._deferNotifications = true;
        const stack: DocLine[] = [this._root];
        
        // All other lines are children of the root, regardless of their indent
        for (let i = 1; i < this._lines.length; i++) {
            const line = this._lines[i];
            line._deferNotifications = true;
            const indent = line.indent;
            
            // Find the appropriate parent based on indentation
            while (stack.length > indent + 1) {
                stack.pop();
            }
            
            const parent = stack[indent];
            if (parent) {
                parent.addChild(line);
            }
            
            // Add to stack for potential children
            if (stack.length <= indent) {
                stack.push(line);
            } else {
                stack[indent + 1] = line;
            }
        }
        
        // Re-enable notifications after tree is built
        for (const line of this._lines) {
            line._deferNotifications = false;
        }
    }
    private _setLines(content: string, name: string): void {
        const lines = content.replace(/\r/g, '').split("\n");
        this._lines = [this._root,
            ...lines.map(line => docLinePool.create((id) => new DocLine(line, id)))
        ];
    }
}
export const noDoc = Doc.create('', '');