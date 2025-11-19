// Test infrastructure
export interface TestResult {
    name: string;
    passed: boolean;
    error?: string;
}

export class TestRunner {
    private results: TestResult[] = [];
    
    runTest(testFn: () => void ): boolean {
        try {
            console.log(`Running test: ${testFn.name}`);
            testFn();
            this.results.push({ name: testFn.name, passed: true });
            // console.log(`✓ ${testFn.name} passed`);
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

export function assert(condition: boolean, message: string = "Assert"): void {
    if (!condition) {
        throw new Error(message);
    }
}

export type Equatable<T> = { equals(other: T): boolean };

export function hasEquals<T>(obj: unknown): obj is Equatable<T> {
    return !!obj && typeof (obj as any).equals === 'function';
}

export function assertEquals<T>(expected: T, actual: T, message: string = "Assert"): void {
    if (hasEquals<T>(expected)) {
        if (!(expected as unknown as Equatable<T>).equals(actual)) {
            console.log(`${message}: Expected "${expected.toString()}", got "${(actual as any).toString()}"`);
            throw new Error(`${message}: Expected "${expected.toString()}", got "${(actual as any).toString()}"`);
        }
    } else {
        if (expected !== actual) {
            console.log(`${message}: Expected "${expected?.toString?.()}", got "${(actual as any)?.toString?.()}"`);
            throw new Error(`${message}: Expected "${expected?.toString?.()}", got "${(actual as any)?.toString?.()}"`);
        }
    }
}

export function assertNotEquals<T>(expected: T, actual: T, message: string = "Assert"): void {
    if (hasEquals<T>(expected)) {
        if ((expected as unknown as Equatable<T>).equals(actual)) {
            throw new Error(`${message}: Expected not equal to "${expected}", got "${actual}"`);
        }
    } else {
        if (expected === actual) {
            throw new Error(`${message}: Expected not equal to "${expected}", got "${actual}"`);
        }
    }
}

