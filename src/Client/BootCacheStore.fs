module Gambol.Client.BootCacheStore

open Fable.Core
open Fable.Core.JsInterop
open Thoth.Json.Core
open Thoth.Json.JavaScript
open Gambol.Shared
open Gambol.Client.JsInterop

let private recordObj (record: BootCache.SnapshotRecord) : obj =
    createObj
        [ "codecVersion" ==> record.codecVersion
          "file" ==> record.file
          "scopeKey" ==> record.scopeKey
          "revision" ==> record.revision
          "isReady" ==> record.isReady
          "stateJson" ==> record.stateJson
          "writtenAt" ==> record.writtenAt
          "bootstrapHash" ==> record.bootstrapHash ]

[<Emit("""
(function(dbName, snapStore, chStore, record, onDone){
  try {
    var req = indexedDB.open(dbName, 1);
    req.onerror = function(){ onDone(false); };
    req.onupgradeneeded = function(e){
      var db = e.target.result;
      if(!db.objectStoreNames.contains(snapStore))
        db.createObjectStore(snapStore, {keyPath:'file'});
      if(!db.objectStoreNames.contains(chStore)){
        var s = db.createObjectStore(chStore, {keyPath:['file','id']});
        s.createIndex('byFile','file',{unique:false});
      }
    };
    req.onsuccess = function(e){
      var db = e.target.result;
      var tx = db.transaction([snapStore, chStore], 'readwrite');
      tx.objectStore(snapStore).put(record);
      var cs = tx.objectStore(chStore);
      var idx = cs.index('byFile');
      idx.openCursor(IDBKeyRange.only(record.file)).onsuccess = function(ev){
        var cursor = ev.target.result;
        if(cursor){ cursor.delete(); cursor.continue(); }
      };
      tx.oncomplete = function(){ db.close(); onDone(true); };
      tx.onerror = function(){ db.close(); onDone(false); };
    };
  } catch (err) { onDone(false); }
})($0,$1,$2,$3,$4)
""")>]
let private writeSnapshotJs
    (dbName: string)
    (snapStore: string)
    (chStore: string)
    (record: obj)
    (onDone: bool -> unit)
    : unit = jsNative

let writeSnapshotAndClearLog
    (record: BootCache.SnapshotRecord)
    (onDone: bool -> unit)
    : unit =
    writeSnapshotJs
        BootCache.databaseName
        BootCache.snapshotStore
        BootCache.changeStore
        (recordObj record)
        onDone

let persistAfterState
    (file: string)
    (scope: string)
    (stateJson: string)
    (response: StateResponse)
    : unit =
    let started = perfNowMs ()
    let record =
        BootCache.snapshotRecord
            file
            scope
            stateJson
            response.revision.Value
            response.isReady
            (System.DateTime.UtcNow.ToString("o"))
            ""
    writeSnapshotAndClearLog record (fun ok ->
        let ms = int (perfNowMs () - started)
        consoleLog (
            "[Gambol boot] IndexedDB snapshot "
            + (if ok then "oncomplete" else "error")
            + $": {ms}ms, {stateJson.Length} chars"))

let private changeObj (file: string) (change: Change) : obj =
    createObj
        [ "file" ==> file
          "id" ==> change.id
          "changeId" ==> change.changeId.ToString()
          "changeJson"
          ==> Thoth.Json.JavaScript.Encode.toString
                  0
                  (Serialization.encodeChange change) ]

[<Emit("""
(function(dbName, chStore, recs, onDone){
  try {
    var req = indexedDB.open(dbName, 1);
    req.onerror = function(){ onDone(false); };
    req.onupgradeneeded = function(e){
      var db = e.target.result;
      if(!db.objectStoreNames.contains('snapshots'))
        db.createObjectStore('snapshots', {keyPath:'file'});
      if(!db.objectStoreNames.contains(chStore)){
        var s = db.createObjectStore(chStore, {keyPath:['file','id']});
        s.createIndex('byFile','file',{unique:false});
      }
    };
    req.onsuccess = function(e){
      var db = e.target.result;
      var tx = db.transaction([chStore], 'readwrite');
      var store = tx.objectStore(chStore);
      recs.forEach(function(r){ store.put(r); });
      tx.oncomplete = function(){ db.close(); onDone(true); };
      tx.onerror = function(){ db.close(); onDone(false); };
    };
  } catch (err) { onDone(false); }
})($0,$1,$2,$3)
""")>]
let private appendChangesJs
    (dbName: string)
    (chStore: string)
    (records: obj array)
    (onDone: bool -> unit)
    : unit = jsNative

let appendChanges (file: string) (changes: Change list) : unit =
    if changes.IsEmpty then
        ()
    else
        appendChangesJs
            BootCache.databaseName
            BootCache.changeStore
            (changes |> List.map (changeObj file) |> List.toArray)
            (fun ok ->
                if not ok then
                    consoleLog "[Gambol boot] IndexedDB change append error")

[<Emit("""
(function(dbName, snapStore, chStore, file, onDone){
  try {
    var req = indexedDB.open(dbName, 1);
    req.onerror = function(){ onDone(false); };
    req.onupgradeneeded = function(e){
      var db = e.target.result;
      if(!db.objectStoreNames.contains(snapStore))
        db.createObjectStore(snapStore, {keyPath:'file'});
      if(!db.objectStoreNames.contains(chStore)){
        var s = db.createObjectStore(chStore, {keyPath:['file','id']});
        s.createIndex('byFile','file',{unique:false});
      }
    };
    req.onsuccess = function(e){
      var db = e.target.result;
      var tx = db.transaction([snapStore, chStore], 'readwrite');
      tx.objectStore(snapStore).delete(file);
      tx.objectStore(chStore).index('byFile')
        .openCursor(IDBKeyRange.only(file)).onsuccess = function(ev){
          var cursor = ev.target.result;
          if(cursor){ cursor.delete(); cursor.continue(); }
        };
      tx.oncomplete = function(){ db.close(); onDone(true); };
      tx.onerror = function(){ db.close(); onDone(false); };
    };
  } catch (err) { onDone(false); }
})($0,$1,$2,$3,$4)
""")>]
let private deleteCacheJs
    (dbName: string)
    (snapStore: string)
    (chStore: string)
    (file: string)
    (onDone: bool -> unit)
    : unit = jsNative

let deleteCache (file: string) (onDone: bool -> unit) : unit =
    deleteCacheJs
        BootCache.databaseName
        BootCache.snapshotStore
        BootCache.changeStore
        file
        onDone

[<Emit("""
(function(dbName, snapStore, chStore, file, onDone){
  var settled = false;
  function finish(json){ if(settled) return; settled = true; onDone(json); }
  function miss(){ finish(""); }
  function payload(rec, changes){
    finish(JSON.stringify({
      codecVersion: rec.codecVersion,
      file: rec.file,
      scopeKey: rec.scopeKey,
      revision: rec.revision,
      ready: rec.isReady,
      stateJson: rec.stateJson,
      writtenAt: rec.writtenAt,
      bootstrapHash: rec.bootstrapHash || "",
      changes: changes
    }));
  }
  try {
    var req = indexedDB.open(dbName, 1);
    req.onerror = function(){ miss(); };
    req.onblocked = function(){ miss(); };
    req.onupgradeneeded = function(e){
      var db = e.target.result;
      if(!db.objectStoreNames.contains(snapStore))
        db.createObjectStore(snapStore, {keyPath:'file'});
      if(!db.objectStoreNames.contains(chStore)){
        var s = db.createObjectStore(chStore, {keyPath:['file','id']});
        s.createIndex('byFile','file',{unique:false});
      }
    };
    req.onsuccess = function(e){
      try {
        var db = e.target.result;
        var tx = db.transaction([snapStore], 'readonly');
        var snapReq = tx.objectStore(snapStore).get(file);
        tx.oncomplete = function(){
          var rec = snapReq.result;
          if(!rec){ db.close(); miss(); return; }
          if(!db.objectStoreNames.contains(chStore)){
            db.close(); payload(rec, []); return;
          }
          try {
            var tx2 = db.transaction([chStore], 'readonly');
            var changes = [];
            var store = tx2.objectStore(chStore);
            if(store.indexNames.contains('byFile')){
              store.index('byFile')
                .openCursor(IDBKeyRange.only(file)).onsuccess = function(ev){
                  var cursor = ev.target.result;
                  if(cursor){
                    changes.push(cursor.value.changeJson);
                    cursor.continue();
                  }
                };
            }
            tx2.oncomplete = function(){ db.close(); payload(rec, changes); };
            tx2.onerror = function(){ db.close(); miss(); };
          } catch (err2) { db.close(); miss(); }
        };
        tx.onerror = function(){ db.close(); miss(); };
      } catch (err) { miss(); }
    };
  } catch (err) { miss(); }
})($0,$1,$2,$3,$4)
""")>]
let private readCacheJs
    (dbName: string)
    (snapStore: string)
    (chStore: string)
    (file: string)
    (onDone: string -> unit)
    : unit = jsNative

let private decodeCachePayload
    (json: string)
    : BootCache.SnapshotRecord option * Change list =
    if json = "" then
        None, []
    else
        let decoder =
            Decode.object (fun get ->
                get.Required.Field "codecVersion" Decode.int,
                get.Required.Field "file" Decode.string,
                get.Required.Field "scopeKey" Decode.string,
                get.Required.Field "revision" Decode.int,
                get.Required.Field "ready" Decode.bool,
                get.Required.Field "stateJson" Decode.string,
                get.Required.Field "writtenAt" Decode.string,
                (get.Optional.Field "bootstrapHash" Decode.string
                 |> Option.defaultValue ""),
                get.Required.Field "changes" (Decode.list Decode.string))
        match Decode.fromString decoder json with
        | Error _ -> None, []
        | Ok (codec, file, scope, rev, ready, stateJson, written, hash, changeJsons) ->
            let snap0 =
                BootCache.snapshotRecord
                    file scope stateJson rev ready written hash
            let snap = { snap0 with codecVersion = codec }
            let parsed =
                changeJsons
                |> List.choose (fun body ->
                    match Decode.fromString Serialization.decodeChange body with
                    | Ok change -> Some change
                    | Error _ -> None)
            if parsed.Length <> changeJsons.Length then None, []
            else Some snap, parsed

let readSnapshotAndLog
    (file: string)
    (onDone: BootCache.SnapshotRecord option -> Change list -> unit)
    : unit =
    readCacheJs
        BootCache.databaseName
        BootCache.snapshotStore
        BootCache.changeStore
        file
        (fun json ->
            let record, log = decodeCachePayload json
            onDone record log)

let requestIdleTruncate
    (file: string)
    (scope: string)
    (zoom: NodeId option)
    (revision: int)
    (isReady: bool)
    (graph: Graph)
    : unit =
    setTimeout
        (fun () ->
            if isDocumentHidden () then
                ()
            else
                readSnapshotAndLog file (fun record log ->
                    match record with
                    | Some snap when
                        BootCache.shouldTruncate
                            log.Length snap.revision revision ->
                        let scoped = BootCache.truncationGraph graph zoom
                        let response =
                            { graph = scoped
                              revision = Revision revision
                              isReady = isReady }
                        let json =
                            Thoth.Json.JavaScript.Encode.toString
                                0
                                (ApiResponseSerialization.encodeStateResponse
                                    response)
                        persistAfterState file scope json response
                    | _ -> ()))
        2500
    |> ignore
