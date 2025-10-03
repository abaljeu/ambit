import { PostDoc } from './ambit.js';
import * as lm from './elements.js';
import { Model } from './model.js';


const LineElement = 'div'; // <div> is the line element

export function setMessage(message : string) {
    lm.messageArea.innerHTML = message;
}

export function links() {
    // Get the content from the editor div
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
                handleTab(e);
                e.preventDefault();
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

// Helper: Get the current Line element containing the cursor
function getCurrentParagraph(): HTMLElement | null {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return null;
    
    let currentP = selection.anchorNode;
    while (currentP && currentP.nodeName !== LineElement.toUpperCase()) {
        currentP = currentP.parentNode;
    }
    
    if (!currentP || currentP.parentNode !== lm.editor) return null;
    return currentP as HTMLElement;
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
    const currentP = getCurrentParagraph();
    if (!currentP) return;
    
    const newP = document.createElement(LineElement);
    newP.contentEditable = 'true';
    newP.textContent = '';
    
    currentP.parentNode!.insertBefore(newP, currentP.nextSibling);
    setCursorInParagraph(newP, 0);
}

function handleArrowUp(e: KeyboardEvent) {
    const currentP = getCurrentParagraph();
    if (!currentP) return;
    
    const prevP = currentP.previousElementSibling as HTMLElement;
    if (!prevP) return;
    
    const offset = prevP.textContent?.length || 0;
    setCursorInParagraph(prevP, offset);
}

function handleArrowDown(e: KeyboardEvent) {
    const currentP = getCurrentParagraph();
    if (!currentP) return;
    
    const nextP = currentP.nextElementSibling as HTMLElement;
    if (!nextP) return;
    
    setCursorInParagraph(nextP, 0);
}

function handleSwapUp(e: KeyboardEvent) {
    const currentP = getCurrentParagraph();
    if (!currentP) return;
    
    const prevP = currentP.previousElementSibling as HTMLElement;
    if (!prevP) return; // Already at the top
    
    // Swap by inserting current before the previous element
    currentP.parentNode!.insertBefore(currentP, prevP);
    
    // Keep focus on the current paragraph
    currentP.focus();
}

function handleSwapDown(e: KeyboardEvent) {
    const currentP = getCurrentParagraph();
    if (!currentP) return;
    
    const nextP = currentP.nextElementSibling as HTMLElement;
    if (!nextP) return; // Already at the bottom
    
    // Swap by inserting next before the current element
    currentP.parentNode!.insertBefore(nextP, currentP);
    
    // Keep focus on the current paragraph
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

function handleTab(e: KeyboardEvent) {
    const currentP = getCurrentParagraph();
    if (!currentP) return;
    
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return;
    
    
    // Get absolute cursor position
    const cursorPos = getAbsoluteCursorPosition(currentP);
    
    // Insert tab into text content
    const text = currentP.textContent || '';
    const newText = text.substring(0, cursorPos) + '\t' + text.substring(cursorPos);
    currentP.textContent = newText;
    
    // Normalize to merge fragmented text nodes
    currentP.normalize();
    
    // Restore cursor position after the tab
    setCursorInParagraph(currentP, cursorPos + 1);
}

function handleShiftTab(e: KeyboardEvent) {
    const currentP = getCurrentParagraph();
    if (!currentP) return;
    
    // Normalize to merge fragmented text nodes
    currentP.normalize();
    
    // Get absolute cursor position
    const cursorPos = getAbsoluteCursorPosition(currentP);
    
    // Get text content and find tabs to the left of cursor
    const text = currentP.textContent || '';
    const textBeforeCursor = text.substring(0, cursorPos);
    
    // Find the last tab character to the left of the cursor
    const lastTabIndex = textBeforeCursor.lastIndexOf('\t');
    if (lastTabIndex === -1) return; // No tab to remove
    
    // Remove the tab character
    const newText = text.substring(0, lastTabIndex) + text.substring(lastTabIndex + 1);
    currentP.textContent = newText;
    
    // Restore cursor position (adjusted for removed tab)
    const newCursorPos = cursorPos > lastTabIndex ? cursorPos - 1 : cursorPos;
    setCursorInParagraph(currentP, newCursorPos);
}

export function save() {
    PostDoc(lm.path.textContent, getEditorContent());
}

export function setEditorContent(doc: Model.Doc) {
    // Clear the editor
    lm.editor.innerHTML = '';
    
    // Create a Line element for each line
    for (const line of doc.lines) {
        const p = document.createElement(LineElement);
        p.contentEditable = 'true';
        p.textContent = line.content;
        p.dataset.lineId = line.id;
        lm.editor.appendChild(p);
    }
}

export function getEditorContent(): string {
    // Extract text from all Line elements
    const paragraphs = lm.editor.querySelectorAll(LineElement);
    const lines: string[] = [];
    
    paragraphs.forEach(p => {
        lines.push(p.textContent || '');
    });
    
    return lines.join('\n');
}