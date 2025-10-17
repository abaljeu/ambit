import { ArraySpan } from './arrayspan.js';
import { Doc, DocLine, DocLineId } from './doc.js';
export enum Type {
    Insert = 'insert',
    Reinsert = 'reinsert',
    Delete = 'delete',
    Text = 'text',
    Move = 'move',
}
export class Insert {
    readonly type = Type.Insert;

    constructor(public readonly owner: DocLineId, 
        public readonly offset: number, 
        public readonly lines: string[]) { }
}
export function makeInsert(doc:Doc, ownerId: DocLineId, beforeId: DocLineId, 
        lines: string[]): Insert {
    const owner = doc.findLine(ownerId);
    const before = doc.findLine(beforeId);
    
    const offset = owner.indexOrLast(before);
    
    return new Insert(ownerId, offset, lines);
}

export class Reinsert {
    readonly type = Type.Reinsert;

    constructor(public readonly owner: DocLineId, 
        public readonly offset: number, 
        public readonly lineIds: DocLineId[]) { }
}
export function makeReinsert(doc:Doc, ownerId: DocLineId, beforeId: DocLineId, 
        lineIds: DocLineId[]): Reinsert {
    const owner = doc.findLine(ownerId);
    const before = doc.findLine(beforeId);
    
    const offset = owner.indexOrLast(before);
    return new Reinsert(ownerId, offset, lineIds);
}

export class Delete {
    readonly type = Type.Delete;
    constructor(public readonly owner: DocLineId, 
        public readonly offset: number, 
        public readonly lines: DocLineId[]) { }
}
export function makeDelete(doc:Doc, id: DocLineId, count: number): Delete {
    const line = doc.findLine(id);
    const owner = line.parent;
    const offset = owner.indexOf(line);

    const lines = owner.children.slice(offset, offset + count);
    const lineIds = lines.map(line => line.id);
    return new Delete(owner.id, offset, lineIds);
}
export class Text {
    readonly type = Type.Text;
    
    constructor(public readonly owner: DocLineId, 
        public readonly offset: number, 
        public readonly oldText: string[],
        public readonly newText: string[]) { }
}
export function makeText(doc:Doc, id: DocLineId, newText: string[]): Text {
    const line = doc.findLine(id);
    const owner = line.parent;
    const offset = owner.indexOf(line);
    if (offset + newText.length > owner.children.length) {
        throw new RangeError(`Text change out of range`);
    }
    const lines = owner.children.slice(offset, offset + newText.length);
    const oldText:string[] = lines.map(line => line.content);
    return new Text(owner.id, offset, oldText, newText);
}
export class Move {
    readonly type = Type.Move;
    constructor(public readonly oldowner: DocLineId, 
        public readonly newowner: DocLineId, 
        public readonly oldoffset: number, 
        public readonly newoffset: number, 
        public readonly lines: DocLineId[]) { }
}
export function makeMove(doc:Doc, lineId: DocLineId, count : number
        , targetOwnerId : DocLineId
        , targetBeforeId : DocLineId) : Move {

    const line = doc.findLine(lineId);
    const targetParent = doc.findLine(targetOwnerId);
    const oldowner = line.parent;
    const oldoffset = oldowner.indexOf(line);
    const targetBefore : DocLine = doc.findLine(targetBeforeId);
    const newoffset = targetParent.indexOrLast(targetBefore);
    if (oldoffset + count > oldowner.children.length) {
        throw new RangeError(`Move change out of range`);
    }
    const lines = oldowner.children.slice(oldoffset, oldoffset + count)
    const lineIds = lines.map(line => line.id);
    return new Move(oldowner.id, targetParent.id, oldoffset, newoffset, lineIds);
}

export type Change = Insert | Reinsert | Delete | Text | Move ;