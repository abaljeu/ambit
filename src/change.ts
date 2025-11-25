import { ArraySpan } from './arrayspan.js';
import { Doc, DocLine, DocLineId } from './doc.js';
import { SiteRow } from './site.js';
import { RowCell } from './site.js';
import { SceneCell } from './sitecells.js';
export enum Type {
    InsertBefore = 'insertBefore',
    InsertBelow = 'insertBelow',
    Reinsert = 'reinsert',
    Remove = 'delete',
    LineTextChange = 'lineText',
    CellTextChange = 'cellText',
    MoveBefore = 'moveBefore',
    MoveBelow = 'moveBelow',
    NoOp = 'noop',
}
export class NoOp {
    readonly type = Type.NoOp;
}
export class InsertBefore {
    readonly type = Type.InsertBefore;
    constructor(public readonly lines: DocLine[], public readonly before: DocLine) { }
}
export function makeInsertBefore(before: DocLine, lines: DocLine[]): InsertBefore {
    return new InsertBefore(lines, before);
}
export class MoveBefore {
    readonly type = Type.MoveBefore;
    constructor(public readonly line: DocLine, public readonly targetBefore: DocLine) { }
}
// Because of the complexity of moving lines into and out of views, move one at a time.
// We could support more provided the movers are all in the same view.  But caveat:
// just because this view is the same doesn't mean all views are shared.
export class MoveBelow {
    readonly type = Type.MoveBelow;
    constructor(public readonly line: DocLine, public readonly targetBelow: DocLine) { }
}
export function makeMoveBelow(line: DocLine, targetBelow: DocLine): MoveBelow {
    return new MoveBelow(line, targetBelow);
}
export class InsertBelow {
    readonly type = Type.InsertBelow;
    constructor(public readonly lines: DocLine[], public readonly above: DocLine) { }
}
export function makeInsertBelow(above: DocLine, lines: DocLine[]): InsertBelow {
    return new InsertBelow(lines, above);
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

export class Remove {
    readonly type = Type.Remove;
    constructor(public readonly lines: DocLine[]) { }
}
export function makeRemove(lines: DocLine[]): Remove {
    return new Remove(lines);
}
export class LineTextChange {
    readonly type = Type.LineTextChange;
    
    constructor(public readonly line: DocLine, 
        public readonly newText: string) { }
}
export class CellTextChange {
    readonly type = Type.CellTextChange;
    
    constructor(public readonly line: DocLine, 
        public readonly cellIndex: number,
        public readonly newText: string) { }
}
export function makeLineTextChange(row: SiteRow, newText: string): LineTextChange {
    const line = row.docLine;
    return new LineTextChange(line, newText);
}
export function makeCellTextChange(rowCell: RowCell, position: number,  length: number, newText: string): CellTextChange {
    const line = rowCell.row.docLine;
    const siteToDocIndent = rowCell.row.indent - line.indent;
    const before = line.content.substring(0, position);
    const after = line.content.substring(position + length);
    const resultText = before + newText + after;
    return new CellTextChange(line, siteToDocIndent + rowCell.cellIndex, resultText);
}

function isAncestorOf(potentialAncestor: DocLine, descendant: DocLine): boolean {
    let current = descendant;
    while (current !== DocLine.end) {
        if (current === potentialAncestor) {
            return true;
        }
        current = current.parent;
    }
    return false;
}

export type Change = InsertBefore | InsertBelow | Reinsert | Remove | LineTextChange 
    | CellTextChange | MoveBelow | MoveBefore | NoOp;
