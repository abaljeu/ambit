import { PostDoc } from './ambit.js';
import * as lm from './elements.js';
import { Editor } from './editor.js';
import { Scene } from './scene.js';


const LineElement = 'div'; // <div> is the line element

export function setMessage(message : string) {
    lm.messageArea.innerHTML = message;
}

export function links() {
    // Get the content from the Editor div
    const textareaValue = getEditorContent();

    // Clear previous links
    lm.linksDiv.innerHTML = '';

    // Regular expression to find wikilinks
    const wikilinkRegex = /\[\[([a-zA-Z0-9 _\.-]+)\]\]/g;
    let match;
    let linksHTML = '';

    // Find all matches and generate links
    while ((match = wikilinkRegex.exec(textareaValue)) !== null) {
        const linkText = match[1]; // Get the text inside [[ ]]
        linksHTML += `<a href="ambit.php?doc=${encodeURIComponent(linkText) + '.amb'}">${linkText}</a><br>`;
    }

    // Inject the generated links into the links div
    lm.linksDiv.innerHTML = linksHTML;
}

let lastCombo="";
export function editorKeyDown(e : KeyboardEvent) {
    if ( // just a mod key was pressed.
        e.key === "Control" ||
        e.key === "Shift" ||
        e.key === "Alt" ||
        e.key === "Meta"
    ) return;
    const mods =
        `${e.ctrlKey ? "C-" : ""}` +
        `${e.altKey ? "A-" : ""}` +
        `${e.shiftKey ? "S-" : ""}` +
        `${e.metaKey ? "M-" : ""}`;
    const combo =  mods + e.key;

    // // Skip duplicates
    if (combo !== lastCombo) {
        console.log(combo);
        lastCombo = combo;
    }

    if (e.ctrlKey || e.metaKey || e.altKey || e.key.length > 1)
    {
        lm.messageArea.textContent = combo;
        switch (combo) {
            case "F5": return;
            case "F6": return;
            case "Tab":
                handleTab();
                // e.preventDefault();
                return;
            case "S-Tab":
                handleShiftTab(e);
                e.preventDefault();
                return;
            case "C-s": 
                save();
                e.preventDefault();
                return;
            case "Enter":
                handleEnter(e);
                e.preventDefault();
                return;
            case "ArrowUp":
                handleArrowUp(e);
                e.preventDefault();
                return;
            case "ArrowDown":
                handleArrowDown(e);
                e.preventDefault();
                return;
            case "C-ArrowUp":
                handleSwapUp(e);
                e.preventDefault();
                return;
            case "C-ArrowDown":
                handleSwapDown(e);
                e.preventDefault();
                return;
        }
        //e.preventDefault(); return;
    }
}

// Helper: Set cursor position in a paragraph
function setCursorInParagraph(p: HTMLElement, offset: number) {
    p.focus();
    const selection = window.getSelection();
    if (!selection) return;
    
    const range = document.createRange();
    const textNode = p.firstChild || p;
    range.setStart(textNode, offset);
    range.collapse(true);
    selection.removeAllRanges();
    selection.addRange(range);
}

function handleEnter(e: KeyboardEvent) {
    // 1) Ask the Editor for the current row
    const currentRow = Editor.CurrentRow();
    const nextRow = currentRow.Next;

    // 2) Ask Scene for the matching Row and update it with current Editor content
    const scene = Scene.getContent();
    const sceneRow = scene.findByLineId(currentRow.Id);
    const currentContent = currentRow.el.textContent ?? "";
    scene.updateLineContent(currentRow.Id, currentContent);

    // 3) Ask Scene to add a Row below (Scene forwards to Model for a new line)
    const inserted = scene.insertBefore(nextRow.Id, "");

    // 4) Use the new Scene Row to request the Editor add a row below
    const newRow = Editor.addBefore(nextRow, inserted.line.id, inserted.line.content);
    newRow.setCaretInRow(0);
}

function handleArrowUp(e: KeyboardEvent) {
    const currentP = Editor.CurrentRow();
    if (!currentP) return;
    
    const prevP = currentP.Previous;
    if (!prevP.valid()) return;
    
    prevP.moveCaretToThisRow();
}

function handleArrowDown(e: KeyboardEvent) {
    const currentP = Editor.CurrentRow();
    if (!currentP.valid()) return;
    
    const nextP = currentP.Next;
    if (!nextP.valid()) return;
    
    nextP.moveCaretToThisRow();
}

function handleSwapUp(e: KeyboardEvent) {
    const currentP = Editor.CurrentRow();
    if (!currentP) return;
    
    const prevP = currentP.Previous;
    if (!prevP.valid()) return;
    
    Editor.moveBefore(currentP, prevP);
    
    currentP.focus();
}

function handleSwapDown(e: KeyboardEvent) {
    const currentP = Editor.CurrentRow();
    if (!currentP.valid()) return;
    
    const nextP = currentP.Next;
    if (!nextP.valid()) return;
    
    Editor.moveBefore(nextP, currentP);
    
    currentP.focus();
}

// Helper: Get absolute cursor position within a paragraph
function getAbsoluteCursorPosition(paragraph: HTMLElement): number {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return 0;
    
    const range = selection.getRangeAt(0);
    const currentNode = range.startContainer;
    const offsetInNode = range.startOffset;
    
    // If cursor is in the paragraph's text node, use offset directly
    if (currentNode === paragraph.firstChild || currentNode === paragraph) {
        return offsetInNode;
    }
    
    // Walk through all text nodes to find absolute position
    let position = 0;
    const walker = document.createTreeWalker(paragraph, NodeFilter.SHOW_TEXT);
    let node: Node | null;
    while ((node = walker.nextNode())) {
        if (node === currentNode) {
            position += offsetInNode;
            break;
        }
        position += node.textContent?.length || 0;
    }
    
    return position;
}

function handleTab() {
    // const currentP = Editor.CurrentRow();
    // if (!currentP.valid()) return;
    
    // const selection = window.getSelection();
    // if (!selection || selection.rangeCount === 0) return;
    
    // const text = currentP.textContent || '';
    // const newText = text.substring(0, cursorPos) + '\t' + text.substring(cursorPos);
    // currentP.textContent = newText;
    
    // // Normalize to merge fragmented text nodes
    // currentP.normalize();
    
    // // Restore cursor position after the tab
    // setCursorInParagraph(currentP, cursorPos + 1);
}

function handleShiftTab(e: KeyboardEvent) {
    const currentP = Editor.CurrentRow();
    if (!currentP) return;
    
    // Normalize to merge fragmented text nodes
    currentP.el.normalize();
    
    const cursorPos = currentP.offset();
    // Get text content and find tabs to the left of cursor
    const text = currentP.el.textContent || '';
    const textBeforeCursor = text.substring(0, cursorPos);
    
    // Find the last tab character to the left of the cursor
    const lastTabIndex = textBeforeCursor.lastIndexOf('\t');
    if (lastTabIndex === -1) return; // No tab to remove
    
    // Remove the tab character
    const newText = text.substring(0, lastTabIndex) + text.substring(lastTabIndex + 1);
    currentP.el.textContent = newText;
    
    // Restore cursor position (adjusted for removed tab)
    const newCursorPos = cursorPos > lastTabIndex ? cursorPos - 1 : cursorPos;
    currentP.setCaretInRow(newCursorPos);
}

export function save() {
    PostDoc(lm.path.textContent, getEditorContent());
}

export function setEditorContent(doc: Scene.Content) {
    // Clear the Editor
    Editor.clear();
    
    // Create a Line element for each line using Editor.Rows
    let end = Editor.NoRow;
    for (const line of doc.rows) {
        const row = Editor.addBefore(end, line.Id, line.Content);
    }
}

export function getEditorContent(): string {
    return Editor.getContent();
}