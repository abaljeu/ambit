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
            case "Tab": return;
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