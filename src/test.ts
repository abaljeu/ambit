// tests need to be reviewed.
// fold is broken
import * as Model from './model.js';
import * as Scene from './scene.js';
import * as Editor from './editor.js';
import * as Controller from './controller.js';
import * as ambit from './ambit.js';
// Test infrastructure
interface TestResult {
    name: string;
    passed: boolean;
    error?: string;
}

class TestRunner {
    private results: TestResult[] = [];
    
    runTest(testFn: () => void ): boolean {
        try {
            console.log(`Running test: ${testFn.name}`);
            testFn();
            this.results.push({ name: testFn.name, passed: true });
            console.log(`✓ ${testFn.name} passed`);
			return true;
        } catch (error) {
            this.results.push({ 
                name: testFn.name, 
                passed: false, 
                error: error instanceof Error ? error.message : String(error) 
            });
            console.log(`✗ ${testFn.name} failed: ${error}`);
			return false;
        }
    }
    
    getResults(): TestResult[] {
        return this.results;
    }
    
    getSummary(): string {
        const passed = this.results.filter(r => r.passed).length;
        const total = this.results.length;
        return `${passed}/${total} tests passed`;
    }
}

// Global test runner instance
const testRunner = new TestRunner();

// Test data setup
const TEST_DOC_PATH = "test.amb";
let TEST_DOC_CONTENT = "";

// Cache the test data from a single call to GetDoc
async function initializeTestData(): Promise<void> {
    TEST_DOC_CONTENT = await ambit.GetDoc(TEST_DOC_PATH);
}

// Helper function to load test document using cached content
function loadTestDoc(): void {
    ambit.LoadDoc(TEST_DOC_CONTENT);
}

// Helper function to get current editor state
function getEditorState(): { 
    rowCount: number, 
    content: string, 
    currentRowId: string,
    sceneRowCount: number 
} {
    const rows = Array.from(Editor.rows());
    const content = Editor.getContent();
    const currentRow = Editor.CurrentRow();
    
    return {
        rowCount: rows.length,
        content,
        currentRowId: currentRow.id,
        sceneRowCount: Scene.data.length
    };
}

// Test 1: Load test file and assert model/scene/editor state
function testLoadModel(): void {
    // Arrange
    loadTestDoc();
    
    // Act
    const state = getEditorState();
    
    // Assert
    if (state.rowCount !== 3) {
        throw new Error(`Expected 3 rows, got ${state.rowCount}`);
    }
    
    if (state.sceneRowCount !== 3) {
        throw new Error(`Expected 3 scene rows, got ${state.sceneRowCount}`);
    }
    
    if (state.content !== TEST_DOC_CONTENT.replace(/\r/g, '')) {
        throw new Error(`Expected content "${TEST_DOC_CONTENT}", got "${state.content}"`);
    }
    
    // Verify each row has proper content
    const rows = Array.from(Editor.rows());
    const expectedLines = TEST_DOC_CONTENT.replace(/\r/g, '').split('\n');
    
    for (let i = 0; i < rows.length; i++) {
        if (rows[i].content !== expectedLines[i]) {
            throw new Error(`Row ${i} content mismatch. Expected "${expectedLines[i]}", got "${rows[i].content}"`);
        }
    }
}

// Test 2: handleEnter function
function testHandleEnter(): void {
    // Arrange
    loadTestDoc();
    const currentRow = Editor.at(0);
    if (!currentRow.valid) {
        throw new Error("No valid current row found");
    }
    
    // Position cursor in middle of first line
    currentRow.setCaretInRow(3); // "Line 1" -> position after "Lin"
    
    // Act
    Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'Enter' }));
    
    // Assert
    const state = getEditorState();
    if (state.rowCount !== 4) {
        throw new Error(`Expected 4 rows after Enter, got ${state.rowCount}`);
    }
    
    const rows = Array.from(Editor.rows());
    if (rows[0].content !== "Lin") {
        throw new Error(`Expected first row to be "Lin", got "${rows[0].content}"`);
    }
    
    if (rows[1].content !== "e 1") {
        throw new Error(`Expected second row to be "e 1", got "${rows[1].content}"`);
    }
}

// Test 3: handleBackspace function
function testHandleBackspace(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const secondRow = rows[1];
    secondRow.setCaretInRow(0); // Position at start of second row
    
    // Act
    Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'Backspace' }));
    
    // Assert
    const state = getEditorState();
    if (state.rowCount !== 2) {
        throw new Error(`Expected 2 rows after Backspace, got ${state.rowCount}`);
    }
    
    const updatedRows = Array.from(Editor.rows());
    if (updatedRows[0].content !== "Line 1Line 2") {
        throw new Error(`Expected merged content "Line 1Line 2", got "${updatedRows[0].content}"`);
    }
}

// Test 4: handleArrowUp function
function testHandleArrowUp(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const secondRow = rows[1];
    secondRow.setCaretInRow(2); // Position in middle of "Line 2"
    
    // Act
    Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'ArrowUp' }));
    
    // Assert
    const currentRow = Editor.CurrentRow();
    if (currentRow.id !== rows[0].id) {
        throw new Error(`Expected to move to first row, but current row is ${currentRow.id}`);
    }
}

// Test 5: handleArrowDown function
function testHandleArrowDown(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const firstRow = rows[0];
    firstRow.setCaretInRow(2); // Position in middle of "Line 1"
    
    // Act
    Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    
    // Assert
    const currentRow = Editor.CurrentRow();
    if (currentRow.id !== rows[1].id) {
        throw new Error(`Expected to move to second row, but current row is ${currentRow.id}`);
    }
}

// Test 6: handleArrowLeft function
function testHandleArrowLeft(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const secondRow = rows[1];
    secondRow.setCaretInRow(2); // Position in middle of "Line 2"
    
    // Act
    Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));
    
    // Assert
    const currentRow = Editor.CurrentRow();
    if (currentRow.visibleTextOffset !== 1) {
        throw new Error(`Expected cursor at position 1, got ${currentRow.visibleTextOffset}`);
    }
}

// Test 7: handleArrowRight function
function testHandleArrowRight(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const firstRow = rows[0];
    firstRow.setCaretInRow(2); // Position in middle of "Line 1"
    
    // Act
    Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    
    // Assert
    const currentRow = Editor.CurrentRow();
    if (currentRow.visibleTextOffset !== 3) {
        throw new Error(`Expected cursor at position 3, got ${currentRow.visibleTextOffset}`);
    }
}

// Test 8: handleSwapUp function
function testHandleSwapUp(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const secondRow = rows[1];
    secondRow.setCaretInRow(0);
    
    // Act
    Controller.editorKeyDown(new KeyboardEvent('keydown', { 
        key: 'ArrowUp', 
        ctrlKey: true 
    }));
    
    // Assert
    const updatedRows = Array.from(Editor.rows());
    if (updatedRows[0].content !== "Line 2") {
        throw new Error(`Expected first row to be "Line 2" after swap, got "${updatedRows[0].content}"`);
    }
    
    if (updatedRows[1].content !== "Line 1") {
        throw new Error(`Expected second row to be "Line 1" after swap, got "${updatedRows[1].content}"`);
    }
}

// Test 9: handleSwapDown function
function testHandleSwapDown(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const firstRow = rows[0];
    firstRow.setCaretInRow(0);
    
    // Act
    Controller.editorKeyDown(new KeyboardEvent('keydown', { 
        key: 'ArrowDown', 
        ctrlKey: true 
    }));
    
    // Assert
    const updatedRows = Array.from(Editor.rows());
    if (updatedRows[0].content !== "Line 2") {
        throw new Error(`Expected first row to be "Line 2" after swap, got "${updatedRows[0].content}"`);
    }
    
    if (updatedRows[1].content !== "Line 1") {
        throw new Error(`Expected second row to be "Line 1" after swap, got "${updatedRows[1].content}"`);
    }
}

// Test 10: handleTab function
function testHandleTab(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const firstRow = rows[0];
    firstRow.setCaretInRow(0); // Position at start of line
    
    // Act
    Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'Tab' }));
    
    // Assert
    const updatedRows = Array.from(Editor.rows());
    if (!updatedRows[0].content.startsWith('\t')) {
        throw new Error(`Expected first row to start with tab, got "${updatedRows[0].content}"`);
    }
}

// Test 11: handleShiftTab function
function testHandleShiftTab(): void {
    // Arrange
    loadTestDoc();
    const rows = Array.from(Editor.rows());
    const firstRow = rows[0];
    firstRow.setCaretInRow(0);
    
    // First add a tab
    Controller.editorKeyDown(new KeyboardEvent('keydown', { key: 'Tab' }));
    
    // Then position cursor in indent area
    const updatedRows = Array.from(Editor.rows());
    updatedRows[0].setCaretInRow(0);
    
    // Act
    Controller.editorKeyDown(new KeyboardEvent('keydown', { 
        key: 'Tab', 
        shiftKey: true 
    }));
    
    // Assert
    const finalRows = Array.from(Editor.rows());
    if (finalRows[0].content !== "Line 1") {
        throw new Error(`Expected tab to be removed, but content is still "${finalRows[0].content}"`);
    }
}

// Test 12: handleToggleFold function
function testHandleToggleFold(): void {
    // Arrange - create content with indented lines
    const foldedContent = "Parent\n\tChild 1\n\tChild 2";
    const doc = Model.addOrUpdateDoc("fold_test.amb", foldedContent);
    Scene.data.loadFromDoc(doc);
    Controller.setEditorContent(Scene.data);
    
    const rows = Array.from(Editor.rows());
    const parentRow = rows[0];
    parentRow.setCaretInRow(0);
    
    // Act
    Controller.editorKeyDown(new KeyboardEvent('keydown', { 
        key: '.', 
        ctrlKey: true 
    }));
    
    // Assert
    const state = getEditorState();
    // After folding, only the parent row should be visible
    if (state.rowCount !== 1) {
        throw new Error(`Expected 1 row after fold, got ${state.rowCount}`);
    }
}

// Run all tests
async function runAllTests(): Promise<void> {
    console.log("Starting Ambit regression tests...\n");
    
    // Initialize test data first
    await initializeTestData();
    
     testRunner.runTest(testLoadModel) &&
     testRunner.runTest(testHandleEnter) &&
     testRunner.runTest(testHandleBackspace) &&
     testRunner.runTest(testHandleArrowUp) &&
     testRunner.runTest(testHandleArrowDown) &&
     testRunner.runTest(testHandleArrowLeft) &&
     testRunner.runTest(testHandleArrowRight) &&
     testRunner.runTest(testHandleSwapUp) &&
     testRunner.runTest(testHandleSwapDown) &&
     testRunner.runTest(testHandleTab) &&
     testRunner.runTest(testHandleShiftTab) &&
     testRunner.runTest(testHandleToggleFold);
    
    console.log(`\nTest Summary: ${testRunner.getSummary()}`);
    
    const failedTests = testRunner.getResults().filter(r => !r.passed);
    if (failedTests.length > 0) {
        console.log("\nFailed tests:");
        failedTests.forEach(test => {
            console.log(`- ${test.name}: ${test.error}`);
        });
    }
}
export { runAllTests, testRunner }
// const { runAllTests } = await import('/dist/test.js'); runAllTests();
