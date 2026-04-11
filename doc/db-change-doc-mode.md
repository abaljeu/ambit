I have two tasks to accomplish: Convert my app to use Postgres instead of file storage (main model file; log file; backup files); and introduce the document structure: convert my app to have this document-based filtered information pattern between client and server. Which first and why?

This is a classic "Big Bang vs. Incremental" architectural dilemma. 

**You should implement the Database Migration (Postgres) FIRST, and the Document Structure SECOND.**

Here is the strategic reasoning why:

### 1. Why Database First? (The Foundation)
You are currently using files (model, log, backup). This is your greatest technical risk. File systems do not provide:
*   **ACID Compliance:** If the app crashes during a write, your log or model file can corrupt.
*   **Concurrency:** You cannot have two processes/users safely appending to a file at the same time without complex file-locking logic.
*   **Querying:** Files require you to load *everything* into memory to perform even a simple "find all" search. 

**Migrating to Postgres first creates a "safe harbor."** You can migrate your existing file-based logic into a simple `blob` or `jsonb` column in a single table. You don't even have to change your application logic much—just point your file-reading/writing functions to `SELECT/UPDATE` statements instead. 

**The benefit:** Once your data is in Postgres, you gain the ability to write SQL queries to audit your data. You can verify that your replayable operations actually produce the state you expect. You are moving from a state of "unverifiable file blob" to "verifiable relational database."

### 2. Why Document Structure Second? (The Evolution)
Converting your app to a document-based structure is a **logical refactor** of your F# models and your API. 

If you try to do this *before* moving to Postgres, you will be trying to manage "documents" inside local files, which will make your serialization/deserialization logic a nightmare of complexity. You'll be manually splitting files, handling re-linking, and debugging file corruption all at once.

**By migrating to Postgres first:**
1.  You have a single source of truth (`operations_log` and `snapshot_table`).
2.  You can write a "projection worker" (a script) that runs against the Postgres table to test your document-splitting logic without affecting your live client.
3.  You can easily "replay" the entire history of your file-based app into the new database structure.

---

### Your Execution Roadmap

#### Phase 1: The "Lift and Shift" (Postgres)
1.  **Setup Postgres:** Create the `operations_log` table.
2.  **The Adapter:** Write a simple F# wrapper that replaces `File.WriteAllText` and `File.ReadAllText`. It should write the *entire* state as a single JSON blob into a `snapshots` table in Postgres.
3.  **The Migration Script:** Write a small utility that reads your current `model.json` and `log.json` and imports them into your new Postgres tables.
4.  **Result:** Your app now works exactly as it did before, but with the durability of a database.

#### Phase 2: The "Decomposition" (Document Structure)
1.  **Define Document Boundaries:** Decide on the logic for what constitutes a "document" (e.g., a top-level node and its children).
2.  **Update the Projection Worker:** Modify the server-side code that updates your `documents` snapshot table. Instead of one big `snapshot` table, it now calculates and stores 50 smaller document snapshots.
3.  **Client-Side Refactor:** Update your F# client to request specific `DocumentId`s instead of a single `model.json`.
4.  **Lazy Loading:** Implement the "adjacent documents" prefetching in your API.

### The "Avoidance" Checklist
*   **Don't build the document-splitting UI in Phase 1.** Keep the UI "flat" for now. It’s okay to have one big document initially, then split it later.
*   **Don't over-engineer the CRDTs.** Just get your basic operation log working in Postgres. 
*   **Don't worry about performance yet.** 1,000,000 nodes in Postgres is fine, even if you are just performing full-table snapshots at first. Optimization comes in Phase 2.

**Does this feel manageable?** By separating the *Storage Reliability* (Phase 1) from the *Data Modeling* (Phase 2), you ensure that if the new document-splitting logic has a bug, you can easily roll back to the previous snapshot without losing your data.