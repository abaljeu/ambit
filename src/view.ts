import * as elements from './elements.js';

// Function to load file content into the textarea
async function loadFileContent() {
    const response = await fetch(window.location.href);
    if (response.ok) {
        const text = await response.text();
        elements.editor.value = text;
    } else {
        console.error('Failed to load file content:', response.status);
    }
}

// Call the function to load content when the page loads
// loadFileContent();

export function setMessage(message : string) {
    elements.messageArea.innerHTML = message;
}

export function links() {
    // Get the value from the textarea
    const textareaValue = elements.editor.value;

    // Clear previous links
    elements.linksDiv.innerHTML = '';

    // Regular expression to find wikilinks
    const wikilinkRegex = /\[\[([a-zA-Z0-9 _\.-]+)\]\]/g;
    let match;
    let linksHTML = '';

    // Find all matches and generate links
    while ((match = wikilinkRegex.exec(textareaValue)) !== null) {
        const linkText = match[1]; // Get the text inside [[ ]]
        linksHTML += `<a href="ambit.html?doc=${encodeURIComponent(linkText) + '.amb'}">${linkText}</a><br>`;
    }

    // Inject the generated links into the links div
    elements.linksDiv.innerHTML = linksHTML;
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
        `${e.ctrlKey ? "C" : ""}` +
        `${e.altKey ? "A" : ""}` +
        `${e.shiftKey ? "S" : ""}` +
        `${e.metaKey ? "M" : ""}`;
    const combo =  mods + " " + e.key;

    // // Skip duplicates
    if (combo !== lastCombo) {
        console.log(combo);
        lastCombo = combo;
    }

    if (e.ctrlKey || e.metaKey || e.altKey || e.key.length > 1)
    {
        e.preventDefault(); return;
    }
}