import { Doc, DocLine, DocLineId } from './doc.js';
import { Scene } from './scene.js';
import { Site, SiteRow } from './site.js';
import { postDoc } from './ambit.js';
import { Cell } from './editor.js';
import { CellSpec } from './cellblock.js';
export class Transaction {
}

class Model {
    public  docArray: Doc[] = [];
    public  history : Transaction[] = [];
    public site: Site = new Site();
    public scene: Scene = new Scene(this.site);
    public get activeCell(): CellSpec | null {
        return this.site.activeCell;
    }
    constructor() {
    }
    public addOrUpdateDoc(text: string, path:string): Doc {
        let doc = this.docArray.find(d => d.name === path);
        if (!doc) {
            doc =  new Doc(path);
            this.docArray.push(doc);
        }
        doc.updateContent(text);
        this.site.setDoc(doc);
        this.scene.loadFromSite(this.site.getRoot());
        return doc;
    }
    public save(): void {
        for (const doc of this.docArray) {
            if (doc.name.endsWith('.amb'))
                postDoc(doc.name, doc.docContent());
        }
    }
}

export const model = new Model();
