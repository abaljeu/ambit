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
            case "ArrowLeft":
                handleArrowLeft(currentRow);
                e.preventDefault();
                return;
            case "ArrowRight":
                handleArrowRight(currentRow);
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

    // split the current row at the cursor position
    const currentContent = currentRow.content;
    const currentRowNewContent = currentContent.substring(0, currentRow.offset); 
    const newRowContent = currentContent.substring(currentRow.offset);
    currentRow.setContent(currentRowNewContent);

    // 2) Update scene with the new contents
    const scene : Scene.Content = Scene.getContent();
    const sceneRow : Scene.Row = scene.findByLineId(currentRow.Id);
    scene.updateLineContent(currentRow.Id, currentRowNewContent);
    const newSceneRow : Scene.Row = scene.insertBefore(sceneRow.Id, newRowContent);
    
    // 3) Update editor with the new contents
    const newEditorRow = Editor.addBefore(nextRow, newSceneRow.Id, newSceneRow.Content);
    newEditorRow.setCaretInRow(0);

}

function handleBackspace(currentRow: Editor.Row) {
    if (currentRow.offset === 0) {
        const prevRow = currentRow.Previous;
        if (!prevRow.valid()) return;
        // take the content of this row and add it to the previous row
        prevRow.setContent(prevRow.content + currentRow.content);
        // delete this row
        const scene = Scene.getContent();
        scene.deleteRow(scene.findByLineId(currentRow.Id));
        prevRow.setCaretInRow(prevRow.content.length);
        Editor.deleteRow(currentRow);
        return;
    }
    else { // we're not at the beginning of the row so just delete the character
        currentRow.setContent(currentRow.content.substring(0, currentRow.offset - 1) + currentRow.content.substring(currentRow.offset));
    }
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

function handleArrowLeft(currentRow: Editor.Row) {
    if (currentRow.offset > 0) {
        // Move cursor left within current row
        currentRow.setCaretInRow(currentRow.offset - 1);
    } else {
        // Move to end of previous row
        const prevRow = currentRow.Previous;
        if (prevRow.valid()) {
            prevRow.setCaretInRow(prevRow.content.length);
        }
    }
}

function handleArrowRight(currentRow: Editor.Row) {
    if (currentRow.offset < currentRow.content.length) {
        // Move cursor right within current row
        currentRow.setCaretInRow(currentRow.offset + 1);
    } else {
        // Move to beginning of next row
        const nextRow = currentRow.Next;
        if (nextRow.valid()) {
            nextRow.setCaretInRow(0);
        }
    }
}

function handleSwapUp(currentRow: Editor.Row) {
    const prevP = currentRow.Previous;
    if (!prevP.valid()) return;
    
    Editor.moveRowAbove(currentRow, prevP);
    
    currentRow.focus();
}

function handleSwapDown(currentRow: Editor.Row) {
    const nextP = currentRow.Next;
    if (!nextP.valid()) return;
    
    Editor.moveRowAbove(nextP, currentRow);
    
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
