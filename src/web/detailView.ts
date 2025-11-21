import { Cell } from "./cell.js";
import { activeContent } from "./elements.js";
import * as HtmlUtil from "./htmlutil.js";
import { PureTextSelection } from "./pureData.js";

let _focus : number = 0;
let _anchor : number = 0;
export function setDetailView(text : string): void {
    activeContent.textContent = text;
}

export function getDetailView(): string {
    return activeContent.textContent;
}

export function setSelection(sel: Selection | null, focus: number, anchor: number): void {
    if (!sel) return;

    const range = new Range();
    const text = activeContent.textContent;
    const start = focus < text.length ? focus : text.length;
    const end = anchor < text.length ? anchor : text.length;
    range.setStart(activeContent.firstChild!, start);
    range.setEnd(activeContent.firstChild!, end);
    
    sel.addRange(range);
}
export function hasFocus(): boolean {
    return activeContent.contains(window.getSelection()?.focusNode ?? null);
}
export function getSelection(): { focus: number, anchor: number } {
    if (hasFocus())
        return { focus: _focus, anchor: _anchor };
    return { focus: -1, anchor: -1 };
}

