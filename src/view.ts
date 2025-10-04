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

export function editorInput() {
    // Sync all editor content back to scene
    syncAllRowsToScene();
    
    // Update all fold indicators
    updateAllFoldIndicators();
    
    // Update wikilinks
    links();
}

function syncAllRowsToScene() {
    const scene = Scene.getContent();
    const paragraphs = lm.editor.querySelectorAll('div');
    
    paragraphs.forEach(div => {
        const lineId = (div as HTMLElement).dataset.lineId;
        if (!lineId) return;
        
        const contentSpan = div.querySelector('.content');
        if (!contentSpan) return;
        
        // Convert visible tabs back to real tabs for scene
        const content = (contentSpan.textContent || '')
            .replace(new RegExp(VISIBLE_TAB, 'g'), '\t');
        scene.updateLineContent(lineId, content);
    });
}

function updateAllFoldIndicators() {
    const scene = Scene.getContent();
    const paragraphs = lm.editor.querySelectorAll('div');
    
    paragraphs.forEach(div => {
        const lineId = (div as HTMLElement).dataset.lineId;
        if (!lineId) return;
        
        const sceneRow = scene.findByLineId(lineId);
        const editorRow = new Editor.Row(div as HTMLDivElement, 0);
        updateFoldIndicator(editorRow, sceneRow);
    });
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
            case "C-.":
                handleToggleFold(currentRow);
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

    // 2) Update scene with the new contents (convert visible tabs to real tabs)
    const scene : Scene.Content = Scene.getContent();
    const sceneRow : Scene.Row = scene.findByLineId(currentRow.Id);
    const realTabCurrentContent = currentRowNewContent
        .replace(new RegExp(VISIBLE_TAB, 'g'), '\t');
    const realTabNewContent = newRowContent
        .replace(new RegExp(VISIBLE_TAB, 'g'), '\t');
    scene.updateLineContent(currentRow.Id, realTabCurrentContent);
    const newSceneRow : Scene.Row = scene.insertBefore(nextRow.Id, realTabNewContent);
    
    // 3) Update editor with the new contents
    const newEditorRow = Editor.addBefore(nextRow, newSceneRow.Id, 
        newSceneRow.Content.replace(/\t/g, VISIBLE_TAB));
    
    // 4) Update fold indicators for both rows
    updateFoldIndicator(currentRow, sceneRow);
    updateFoldIndicator(newEditorRow, newSceneRow);
    
    newEditorRow.setCaretInRow(0);
}

function handleBackspace(currentRow: Editor.Row) {
    const scene = Scene.getContent();
    
    if (currentRow.offset === 0) {
        const prevRow = currentRow.Previous;
        if (!prevRow.valid()) return;
        
        const prevRowOriginalLength = prevRow.content.length;
        // take the content of this row and add it to the previous row
        const newContent = prevRow.content + currentRow.content;
        prevRow.setContent(newContent);
        
        // Update scene
        const realTabContent = newContent.replace(new RegExp(VISIBLE_TAB, 'g'), '\t');
        scene.updateLineContent(prevRow.Id, realTabContent);
        scene.deleteRow(scene.findByLineId(currentRow.Id));
        
        // Update fold indicators
        updateAllFoldIndicators();
        
        prevRow.setCaretInRow(prevRowOriginalLength);
        Editor.deleteRow(currentRow);
        return;
    }
    else { // we're not at the beginning of the row so just delete the character
        const newContent = currentRow.content.substring(0, currentRow.offset - 1) + 
            currentRow.content.substring(currentRow.offset);
        currentRow.setContent(newContent);
        
        // Update scene
        const realTabContent = newContent.replace(new RegExp(VISIBLE_TAB, 'g'), '\t');
        scene.updateLineContent(currentRow.Id, realTabContent);
        
        // Update fold indicators (indentation may have changed)
        updateAllFoldIndicators();
        
        currentRow.setCaretInRow(currentRow.offset - 1);
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
    
    // Sync editor to scene
    syncAllRowsToScene();
    
    // Update fold indicators (indentation context may have changed)
    updateAllFoldIndicators();
    
    currentRow.focus();
}

function handleSwapDown(currentRow: Editor.Row) {
    const nextP = currentRow.Next;
    if (!nextP.valid()) return;
    
    Editor.moveRowAbove(nextP, currentRow);
    
    // Sync editor to scene
    syncAllRowsToScene();
    
    // Update fold indicators (indentation context may have changed)
    updateAllFoldIndicators();
    
    currentRow.focus();
}

function handleToggleFold(currentRow: Editor.Row) {
    const scene = Scene.getContent();
    const sceneRow = scene.findByLineId(currentRow.Id);
    
    // Ask scene to calculate which rows should be toggled
    const affectedRows = scene.toggleFold(currentRow.Id);
    
    // If no children to fold, do nothing
    if (affectedRows.length === 0) return;
    
    // Update fold indicator for current row
    updateFoldIndicator(currentRow, sceneRow);
    
    // Apply visibility changes
    if (sceneRow.fold) {
        // Folding - remove rows from DOM
        Editor.deleteAfter(currentRow, affectedRows.length);
    } else {
        // Unfolding - add rows back to DOM
        const rowsToAdd = affectedRows.map(r => ({
            id: r.Id,
            content: r.Content.replace(/\t/g, VISIBLE_TAB)
        }));
        const addedEditorRows = Editor.addAfter(currentRow, rowsToAdd);
        
        // Update fold indicators for newly added rows
        for (let i = 0; i < addedEditorRows.length; i++) {
            updateFoldIndicator(addedEditorRows[i], affectedRows[i]);
        }
    }
    
    // Restore focus
    currentRow.focus();
}

function updateFoldIndicator(editorRow: Editor.Row, sceneRow: Scene.Row) {
    const scene = Scene.getContent();
    const idx = scene.findIndexByLineId(sceneRow.Id);
    const baseIndent = sceneRow.getIndentLevel();
    
    // Check if this row has any more-indented children
    let hasChildren = false;
    for (let i = idx + 1; i < scene.rows.length; i++) {
        const nextRow = scene.rows[i];
        const nextIndent = nextRow.getIndentLevel();
        
        if (nextIndent <= baseIndent) break;
        hasChildren = true;
        break;
    }
    
    if (!hasChildren) {
        editorRow.setFoldIndicator(' ');
    } else if (sceneRow.fold) {
        editorRow.setFoldIndicator('+');
    } else {
        editorRow.setFoldIndicator('-');
    }
}

function handleTab(currentRow: Editor.Row) {
    const offset = currentRow.offset;
    let c = currentRow.content;
    const newContent = c.substring(0, offset) + VISIBLE_TAB + c.substring(offset);
    currentRow.setContent(newContent);
    
    // Sync to scene (convert visible tab to real tab)
    const scene = Scene.getContent();
    const realTabContent = newContent.replace(new RegExp(VISIBLE_TAB, 'g'), '\t');
    scene.updateLineContent(currentRow.Id, realTabContent);
    
    // Update fold indicators
    updateAllFoldIndicators();
    
    // Restore cursor position
    currentRow.setCaretInRow(offset + 1);
}

function handleShiftTab(currentRow: Editor.Row) {
    // // Normalize to merge fragmented text nodes
    // currentRow.el.normalize();
    
    // const cursorPos = currentRow.offset;
    // // Get text content and find tabs to the left of cursor
    // const text = currentRow.el.textContent || '';
    // const textBeforeCursor = text.substring(0, cursorPos);
    
    // // Find the last visible tab character to the left of the cursor
    // const lastTabIndex = textBeforeCursor.lastIndexOf(VISIBLE_TAB);
    // if (lastTabIndex === -1) return; // No tab to remove
    
    // // Remove the tab character
    // const newText = text.substring(0, lastTabIndex) + text.substring(lastTabIndex + 1);
    // currentRow.el.textContent = newText;
    
    // // Restore cursor position (adjusted for removed tab)
    // const newCursorPos = cursorPos > lastTabIndex ? cursorPos - 1 : cursorPos;
    // currentRow.setCaretInRow(newCursorPos);
}

export function save() {
    PostDoc(lm.path.textContent, getEditorContent());
}

export function setEditorContent(doc: Scene.Content) {
    // Clear the Editor
    Editor.clear();
    
    // Create a Line element for each visible line
    let end = Editor.NoRow;
    for (const sceneRow of doc.rows) {
        if (!sceneRow.visible) continue;
        
        // Convert regular tabs to visible tabs for display
        const visibleContent = sceneRow.Content.replace(/\t/g, VISIBLE_TAB);
        const editorRow = Editor.addBefore(end, sceneRow.Id, visibleContent);
        
        // Update fold indicator
        updateFoldIndicator(editorRow, sceneRow);
    }
}

export function getEditorContent(): string {
    // Convert visible tabs back to regular tabs for saving
    return Editor.getContent().replace(new RegExp(VISIBLE_TAB, 'g'), '\t');
}
