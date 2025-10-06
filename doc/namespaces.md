# DOM interactions in [[src/controller.ts]] (grouped by operation)

- **Create elements**
  - document.createElement(LineElement): handleEnter, setEditorContent

- **Clear/remove content**
  - linksDiv.innerHTML = '': links
  - editor.innerHTML = '': setEditorContent

- **Insert/append/move nodes**
  - parentNode.insertBefore(newP, currentP.nextSibling): handleEnter
  - parentNode.insertBefore(currentP, prevP): handleSwapUp
  - parentNode.insertBefore(nextP, currentP): handleSwapDown
  - editor.appendChild(p): setEditorContent

- **Query/select nodes**
  - editor.querySelectorAll(LineElement): getEditorContent
  - previousElementSibling / nextElementSibling: handleArrowUp, handleArrowDown
  - window.getSelection(), selection.anchorNode: getCurrentParagraph
  - currentP.parentNode !== editor, parentNode traversal: getCurrentParagraph

- **Read text**
  - el.textContent (read): getEditorContent, handleTab, handleShiftTab, handleArrowUp (length), getAbsoluteCursorPosition
  - path.textContent (read): save

- **Write text/HTML**
  - messageArea.innerHTML = ...: setMessage
  - linksDiv.innerHTML = linksHTML: links
  - messageArea.textContent = combo: editorKeyDown
  - p.textContent = line.content: setEditorContent
  - newP.textContent = '': handleEnter
  - currentP.textContent = newText: handleTab
  - currentP.textContent = newText: handleShiftTab

- **Attributes / dataset / properties**
  - p.contentEditable = 'true', newP.contentEditable = 'true': setEditorContent, handleEnter
  - p.dataset.lineId = line.id: setEditorContent

- **Focus and caret**
  - HTMLElement.focus(): setCursorInParagraph, handleSwapUp, handleSwapDown
  - window.getSelection(): setCursorInParagraph, getCurrentParagraph, getAbsoluteCursorPosition, handleTab
  - document.createRange(), range.setStart(...), range.collapse(true), selection.removeAllRanges(), selection.addRange(range): setCursorInParagraph
  - document.createTreeWalker(paragraph, NodeFilter.SHOW_TEXT): getAbsoluteCursorPosition

- **Normalization**
  - currentP.normalize(): handleTab, handleShiftTab

- **Event control**
  - e.preventDefault(): handled combos in editorKeyDown (e.g., Tab, Shift+Tab, Enter, arrows, Ctrl+S)

- **String/HTML generation for links**
  - Build <a> HTML string and assign via innerHTML: links