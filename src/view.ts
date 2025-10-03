import { PostDoc } from './ambit.js';
import * as lm from './elements.js';
import * as Editor from './editor.js';
import * as Scene from './scene.js';


const LineElement = 'div'; // <div> is the line element
const VISIBLE_TAB = '→'; // Visible tab character

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
        const currentRow = Editor.CurrentRow();
        if (!currentRow.valid()) return;

        switch (combo) {
            case "F5": return;
            case "F6": return;
            case "Tab":
                handleTab(currentRow);
                 e.preventDefault();
                return;
            case "S-Tab":
                handleShiftTab(currentRow);
                e.preventDefault();
                return;
            case "C-s": 
                save();
                e.preventDefault();
                return;
            case "Enter":
                handleEnter(currentRow);
                e.preventDefault();
                return;
            case "Backspace":
                handleBackspace(currentRow);
                e.preventDefault();
                return;
            case "ArrowUp":
                handleArrowUp(currentRow);
                e.preventDefault();
                return;
            case "ArrowDown":
                handleArrowDown(currentRow);
                e.preventDefault();
                return;
            case "C-ArrowUp":
                handleSwapUp(currentRow);
                e.preventDefault();
                return;
            case "C-ArrowDown":
                handleSwapDown(currentRow);
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

function handleEnter(currentRow: Editor.Row) {
    // 1) Get the next row
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

function handleBackspace(currentRow: Editor.Row) {
    const prevRow = currentRow.Previous;
    if (!prevRow.valid()) return;
    
    const scene = Scene.getContent();
    scene.updateLineContent(currentRow.Id, "");
    Editor.moveBefore(currentRow, prevRow);
}

function handleArrowUp(currentRow: Editor.Row) {
    const prevP = currentRow.Previous;
    if (!prevP.valid()) return;
    
    prevP.moveCaretToThisRow();
}

function handleArrowDown(currentRow: Editor.Row) {
    const nextP = currentRow.Next;
    if (!nextP.valid()) return;
    
    nextP.moveCaretToThisRow();
}

function handleSwapUp(currentRow: Editor.Row) {
    const prevP = currentRow.Previous;
    if (!prevP.valid()) return;
    
    Editor.moveBefore(currentRow, prevP);
    
    currentRow.focus();
}

function handleSwapDown(currentRow: Editor.Row) {
    const nextP = currentRow.Next;
    if (!nextP.valid()) return;
    
    Editor.moveBefore(nextP, currentRow);
    
    currentRow.focus();
}

function handleTab(currentRow: Editor.Row) {
    const offset = currentRow.offset;
    let c = currentRow.content;
    currentRow.setContent(c.substring(0, offset) + VISIBLE_TAB + c.substring(offset));
}

function handleShiftTab(currentRow: Editor.Row) {
    // Normalize to merge fragmented text nodes
    currentRow.el.normalize();
    
    const cursorPos = currentRow.offset;
    // Get text content and find tabs to the left of cursor
    const text = currentRow.el.textContent || '';
    const textBeforeCursor = text.substring(0, cursorPos);
    
    // Find the last visible tab character to the left of the cursor
    const lastTabIndex = textBeforeCursor.lastIndexOf(VISIBLE_TAB);
    if (lastTabIndex === -1) return; // No tab to remove
    
    // Remove the tab character
    const newText = text.substring(0, lastTabIndex) + text.substring(lastTabIndex + 1);
    currentRow.el.textContent = newText;
    
    // Restore cursor position (adjusted for removed tab)
    const newCursorPos = cursorPos > lastTabIndex ? cursorPos - 1 : cursorPos;
    currentRow.setCaretInRow(newCursorPos);
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
        // Convert regular tabs to visible tabs for display
        const visibleContent = line.Content.replace(/\t/g, VISIBLE_TAB);
        const row = Editor.addBefore(end, line.Id, visibleContent);
    }
}

export function getEditorContent(): string {
    // Convert visible tabs back to regular tabs for saving
    return Editor.getContent().replace(new RegExp(VISIBLE_TAB, 'g'), '\t');
}