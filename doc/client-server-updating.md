# Client/Server Updating

After [[Transaction-based-Undo-System]].

The existing system has post file and get file.  We will add new endpoints for get changes and post changes.

### 7. Add Transaction Export for Server Sync

Add functions to Model:

- `getPendingTransactions()` → returns transactions since last baseline
- `markTransactionsPosted()` → marks transactions as posted to server
- `getBaselineId()` → returns current baseline ID
- These prepare for future server POST implementation


### 8. Server side
a. The server has:
- files.

Will add:
- a database of posted transactions and a current version id for each file.
- raw implementation: a log file.  open in read or append mode.  each line is a json transaction.

b. When server receives a state and a list of transactions from a client.
- If the baseline version id received matches the current version id, we store the transations and update the model, and return Accept
- If it doesn't match, we notify changes exist.

c. When the client requests the current server changeset, since a baseline.
- Read the stored transactions since the baseline version
- Send this list back to the client

c. When the client sends notice of acceptance:
- (note, the proposal is not kept in memory, but the client's acceptance message includes the proposal)
- no special code is required; this is case b, as the clients acceptance note will be current.

### 9. Posting

#### When posting
Send to the server the baseline id plus the list of transactions.
Until we have acceptance, display a warning.

When the server responds:
- If the client has modified the local model since the post, ignore the response.  A new POST will (have been) arranged.
- If the server responded with Acceptance, it will upload the baseline id, and reset the transaction list to []
- If the server responded with a merge proposal
  - (this means the server is proposing a merger of our edits with edits from other sources)
  - store as a pending merge

### 10. Merging

Conflicts should be rare.  So a simple retry system should suffice.  In the event the network is down, client can accumulate changes until the net is up, click merge, and then merge would proceed after one or two attempts.

#### Client side
Poll server periodically for changes since baseline.
If poll times out, display warning.
If the server reports changes and we had none locally, apply those.

If the server reports external changes, and we have changes.
- controller asks the editor to add a merge button

When merge is clicked, synchronously:
- Purge the redo stack.  This cannot be restored.
- shows "Merging..." and a Cancel button.
- If cancelled, no local model change happens.

- rewind our state to the baseline.
- construct a merge of the server's transactions and our transactions
- post the merge to the server
  - if rejected because additional transactions have occured serverside
    - remove the merge object
    - take the server's new transaction list
    - try again.
  - otherwise, get confirmation and new version number.

- Hide the merge/merging display elements.
- purge transaction list, and update baseline version.

