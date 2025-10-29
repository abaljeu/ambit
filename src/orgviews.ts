import { DocLine, DocLineView } from "./doc.js";

export function orgByViews(as : readonly DocLineView[], bs : readonly DocLineView[]) : {a : DocLineView|null, b : DocLineView|null}[] {
    const result: {a : DocLineView|null, b : DocLineView|null}[] = [];
    const matchedBs = new Set<DocLineView>();
    
    // Find matching pairs
    for (const a of as) {
        let found = false;
        for (const b of bs) {
            if (sameView(a, b)) {
                // Add pair to list
                result.push({a, b});
                matchedBs.add(b);
                found = true;
                break;
            }
        }
        // If a has no match, push {a, null}
        if (!found) {
            result.push({a, b: null});
        }
    }
    
    // For unmatched b's, push {null, b}
    for (const b of bs) {
        if (!matchedBs.has(b)) {
            result.push({a: null, b});
        }
    }
    
    return result;
}

export function sameView(a : DocLineView, b : DocLineView) : boolean {
    const aRoot = a.docviewRoot();
    const bRoot = b.docviewRoot();
    return aRoot === bRoot;
}
