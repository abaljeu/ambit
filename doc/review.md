# Documentation Review - Implementation vs. Documentation

Review of `src/Shared/` implementation against documentation in `doc/`.

## Implementation Status

### ✅ Implemented (matches docs)

1. **Core Domain Model** (`Model.fs`)
   - `NodeId` (wrapped Guid) ✓
   - `Node` with `id`, `text`, `children` ✓
   - `Graph` with `root` and `nodes` Map ✓
   - `Graph.create()` creates root node ✓

2. **Operations** (`History.fs`)
   - `Op` type with `NewNode`, `SetText`, `Replace` ✓
   - `Change` type with `id` and `ops` list ✓
   - `History` type with `past`, `future`, `nextId` ✓
   - `State` type with `graph` and `history` ✓

3. **Op Application**
   - `Op.apply` applies ops to state ✓
   - `Op.undo` inverts ops (except NewNode) ✓
   - `Change.apply` applies change (ops in order) ✓
   - `Change.undo` inverts change (ops reversed) ✓

4. **History Management**
   - `History.applyChange` applies change and updates history ✓
   - `History.undo` pops from past, applies inverse, pushes to future ✓
   - `History.redo` pops from future, applies, pushes to past ✓

5. **Graph Operations**
   - `Graph.setText` with old/new validation ✓
   - `Graph.replace` with validation ✓
   - `Graph.newNode` creates new node ✓

## Inconsistencies Found

### 1. **Domain Model Naming**
fixed.

### 2. **History.nextId Field**

**Documentation** (`undo.md` line 19):
```
type History = { past: Change list; future: Change list }
```

**Implementation** (`History.fs` line 19-22):
```fsharp
type History =
    { past: Change list
      future: Change list
      nextId: int }
```

**Issue**: Implementation has `nextId` field not mentioned in docs. This is actually needed for the implementation, so docs should be updated.

### 3. **ApplyResult Type**

**Documentation**: Docs mention `Result<Graph, string>` for apply functions.

**Implementation**: Uses `ApplyResult` discriminated union:
```fsharp
type ApplyResult =
    | Changed of State
    | Unchanged of State
    | Invalid of State * string
```

**Issue**: Implementation is more sophisticated (includes `Unchanged` variant), but docs don't mention this pattern. This is an improvement, but should be documented.

### 4. **Op vs Change API Mismatch**

**Documentation** (`undo.md` line 26-29):
```
- `applyOp : Op -> Graph -> Result<Graph, string>`
- `invertOp : Op -> Op`
- `applyChange : Change -> Graph -> Result<Graph, string>`
- `invertChange : Change -> Change`
```

**Implementation**:
- `Op.apply : Op -> State -> ApplyResult` (takes State, not Graph)
- `Op.undo : Op -> State -> ApplyResult` (no separate `invertOp`)
- `Change.apply : Change -> State -> ApplyResult` (takes State, not Graph)
- `Change.undo : Change -> State -> ApplyResult` (no separate `invertChange`)

**Issue**: 
- Functions take `State` not `Graph` (includes history)
- No separate `invertOp`/`invertChange` functions (inversion happens in `undo`)
- Return type is `ApplyResult` not `Result<Graph, string>`

### 5. **Replace Operation Status**

**Documentation** (`arch.md` line 108):
```
- [ ] replace (establishes parent-child relations)
```

**Implementation**: `Replace` op is fully implemented in `History.fs` and `Model.fs`.

**Issue**: Docs show `Replace` as not implemented, but it is.

### 6. **ModelBuilder Status**

**Documentation** (`arch.md` line 117):
```
- [ ] create many nodes from text
```

**Implementation**: `ModelBuilder.createNodes` exists and is implemented.

**Issue**: Docs show as not implemented, but it is.

## Missing Implementations

### 1. **JSON Serialization** (Critical Gap)

**Documentation** (`plan.md` line 24):
```
Deliverable: a JSON encoding for ops + state.
```

**Implementation**: No JSON serialization code found in `Shared/`.

**Missing**:
- JSON encoding for `Op` (all variants)
- JSON encoding for `Change`
- JSON encoding for `State`/`Graph` (for snapshots)
- JSON encoding for `NodeId` (Guid serialization)
- JSON decoding (round-trip)

**Impact**: Cannot communicate between client and server.

### 2. **API Contract Definition** (Critical Gap)

**Documentation** (`arch.md` line 78-88, `plan.md` line 36-42):
- `GET /` -> HTML
- `GET /state` -> current graph + revision
- `POST /op/new-node`
- `POST /op/set-text`
- `POST /op/replace`
- `POST /op/undo`
- `POST /op/redo`
- `GET /ops?since={revision}` -> ops since revision

**Implementation**: 
- Server skeleton exists (`src/Server/Program.fs`) but only has `GET /` returning "Hello World!"
- No API endpoints implemented
- No request/response types defined
- No revision tracking

**Missing**:
- Request DTOs for each POST endpoint
- Response DTOs (ack with revision + change id)
- Revision type/management
- API contract documentation

### 3. **Revision Tracking**

**Documentation**: Mentions `revision` throughout (monotonically increasing, used for sync).

**Implementation**: No `revision` type or tracking in `Shared/`.

**Missing**:
- `Revision` type (likely `int`)
- Revision in `State` or separate tracking
- Revision assignment logic

### 4. **Persistence Format**

**Documentation** (`plan.md` line 53-70, `arch.md` line 175-189):
- Append-only ops log format
- Snapshot format
- Change number tracking

**Implementation**: No persistence code exists.

**Missing**:
- Log file format specification
- Snapshot file format specification
- Serialization for persistence (may differ from API JSON)

## Recommendations

### High Priority

1. **Add JSON serialization module** (`Shared/Json.fs` or `Shared/Serialization.fs`)
   - Use `System.Text.Json` (works in both .NET and Fable)
   - Define JSON shapes for all types
   - Add round-trip tests

2. **Define API contract** (`doc/api.md` or section in `arch.md`)
   - Request/response shapes for each endpoint
   - Error response format
   - Revision handling

3. **Add Revision type** to `Shared/Model.fs`
   - Simple `type Revision = int` or similar
   - Include in state or separate tracking

4. **Update documentation** to match implementation:
   - Remove `name` field from node description
   - Document `History.nextId`
   - Document `ApplyResult` pattern
   - Update function signatures in `undo.md`
   - Mark `Replace` and `ModelBuilder` as implemented

### Medium Priority

5. **Define persistence format** (`doc/persistence.md`)
   - Log file format (JSON lines? custom format?)
   - Snapshot format
   - Change number assignment

6. **Add API request/response types** (`Shared/Api.fs`)
   - DTOs for each endpoint
   - Error response type
   - Revision in responses

### Low Priority

7. **Consider consolidating** `invertOp`/`invertChange` if needed for other use cases
   - Currently inversion is only in `undo`, but docs suggest separate functions
   - May be needed for conflict resolution or other features

## Notes

- Implementation is more complete than docs suggest (Replace, ModelBuilder done)
- Implementation uses better patterns than docs describe (`ApplyResult`, `State`-based)
- Critical missing piece is serialization - blocks client/server communication
- API contract needs definition before server implementation can proceed

