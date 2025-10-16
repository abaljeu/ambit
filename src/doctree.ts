import { Doc, DocLine, DocLineId } from './doc.js';

export class DocTree {
    public readonly line: DocLine;
    
    private constructor(root: DocLine) {
        this.line = root;
    }
    
    public static create(rootLine: DocLine): DocTree {
        return new DocTree(rootLine);
    }
    
    public static fromDoc(doc: Doc): DocTree {
        const root = doc.getRoot();
        return  new DocTree(root);
    }
    
    public getTotalNodes(): number {
        return this.line.length;
    }
    
    public findNode(lineId: DocLineId): DocLine {
        return this._findNodeRecursive(this.line, lineId);
    }
    
    private _findNodeRecursive(node: DocLine, lineId: DocLineId): DocLine {
        if (node.id.equals(lineId)) {
            return node;
        }
        
        for (const child of node.children) {
            const found = this._findNodeRecursive(child, lineId);
            if (found) return found;
        }
        
        return Doc.end;
    }
    
    public getSubtree(node: DocLine): DocTree {
        return new DocTree(node);
    }
    
    public walk(callback: (node: DocLine, depth: number) => void): void {
        this._walkRecursive(this.line, 0, callback);
    }
    
    private _walkRecursive(node: DocLine, depth: number, callback: (node: DocLine, depth: number) => void): void {
        callback(node, depth);
        for (const child of node.children) {
            this._walkRecursive(child, depth + 1, callback);
        }
    }
}