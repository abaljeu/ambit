import { Site, SiteRow } from './site.js';
/*    => filter, flatten
    Scene
        SceneRow
        reference SiteNode
        compute visible
*/

export class SceneRow {
    constructor(public readonly site: SiteRow, public readonly visible: boolean) {}
    public get content(): string { return this.site.doctree.line.content; }
}

export class Scene {
    private _rows: SceneRow[] = [];
    constructor() {}
    public loadFromSite(site: SiteRow): void {
        this._rows = this._flattenTree(site);
    }
    
    private _flattenTree(siteRow: SiteRow): SceneRow[] {
        const result: SceneRow[] = [];
        this._flattenRecursive(siteRow, result);
        return result;
    }
    
    private _flattenRecursive(siteRow: SiteRow, result: SceneRow[], parentVisible: boolean = true, parentFolded: boolean = false): void {
        // A SceneRow is visible if its parent is visible and not folded
        const visible = parentVisible && !parentFolded;
        
        // Add the current node
        result.push(new SceneRow(siteRow, visible));
        
        // Recursively add all children
        for (const child of siteRow.children) {
            this._flattenRecursive(child, result, visible, siteRow.folded);
        }
    }
    
    public get rows(): readonly SceneRow[] { return this._rows; }
}
