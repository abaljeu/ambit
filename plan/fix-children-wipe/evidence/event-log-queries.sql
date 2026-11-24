-- Ambit child-list investigation: READ-ONLY queries for Alan to run against
-- Azure Postgres gambol-pg / database gambol (e.g. psql with a read-only role).
-- Every statement is a SELECT inside a READ ONLY transaction. Nothing writes.
--
-- Schema source: src/Server/Database.fs, Database.initSchema
--   events(event_id INT PK, submission_id UUID, payload TEXT, recorded_at TIMESTAMPTZ DEFAULT NOW())
--   graph(singleton, root_id UUID, revision INT)
--   nodes(id UUID PK, text, name, css_classes JSONB, update_time TIMESTAMPTZ, kind TEXT,
--         document_state TEXT, persist_state TEXT, persisted_version INT, changed_version INT)
--   node_children(parent_id, ordinal, child_id, ownership 'owner'|'ref'), FK ON DELETE CASCADE
-- NOTE: doc/current/persistence-model.md still says "changes"; initSchema DROPs "changes"
-- and the real log table is "events".
-- Payload JSON: src/Server/EventLogFile.fs encodeEvent -> src/Shared/EventJson.fs encode:
--   {eventId, submissionId, authority, commandName, body}; body = {kind:'notice', notice:<tag>, nodeId}
--   or {kind:'order', order:{kind:'change'|'undo'|'redo'|'startActor'|..., ops:[...], origin}}
--   notice tags: nodeParsed nodeUnparsed nodeParsing nodePersisting nodePersisted nodeUnpersisted ...
--   op types (src/Shared/Serialization.fs encodeOp): NewNode SetText SetClasses Replace
--   (parentId, oldChildren, newChildren) NewSpecialNode SetName SetUpdateTime.
-- Fixed ids (src/Shared/GraphBuild.fs): root 0000...0000, TRASH ...0001, Workspaces ...0002, SYSTEM ...0003.
-- recorded_at is TIMESTAMPTZ (absolute). Windows below are given in America/Toronto (EST, UTC-5):
--   W1 = 2026-11-20 09:30-10:30 EST = 14:30-15:30 UTC (life new-id merge 62ec2d0 / e31beb6)
--   W2 = 2026-11-21 13:00-17:00 EST = 18:00-22:00 UTC (ambit merge a4e17e56, Save 3a1a0aa8)

BEGIN TRANSACTION READ ONLY;
SET LOCAL TIME ZONE 'America/Toronto';

-- Q0. Sanity: log span and current revision.
-- Proves the event log covers W1/W2 (if min(recorded_at) is later, the log was reset and
-- events cannot be replayed for these windows).
SELECT min(event_id), max(event_id), min(recorded_at), max(recorded_at) FROM events;
SELECT * FROM graph;

-- Q1. Find the nodes of interest by name/kind (ids are needed by later queries).
-- Proves which node ids are the current issue-07 File, the life/ambit Workspaces,
-- and whether any duplicate (numbered, e.g. 'life1') Workspace exists.
SELECT n.id, n.name, n.kind, n.document_state, n.persist_state, n.update_time,
       p.parent_id, p.ownership,
       (SELECT count(*) FROM node_children c WHERE c.parent_id = n.id) AS child_rows
FROM nodes n
LEFT JOIN node_children p ON p.child_id = n.id
WHERE n.name IN ('07-document-the-command-line-actor.md','life','ambit','issues','command-line-actor')
   OR n.kind IN ('workspace','workspaces','trash')
   OR n.name ~* '^(life|ambit)[0-9]+$'
ORDER BY n.kind, n.name;

-- Q2. Workspaces node: which Workspaces are owned children right now.
-- A mapping (GitHub remote = .git under DataDir/<name>; desktop local mapping keyed by
-- label) is found by Workspace NAME (GraphQuery.trySyncRootByLabel). If the expected
-- Workspace is missing here, renamed, Ref-only, or sits under TRASH, that proves why its
-- GitHub and local mappings disappeared.
SELECT c.ordinal, c.ownership, n.id, n.name, n.kind, n.document_state,
       (SELECT count(*) FROM node_children x WHERE x.parent_id = n.id) AS child_rows
FROM node_children c JOIN nodes n ON n.id = c.child_id
WHERE c.parent_id = '00000000-0000-0000-0000-000000000002'
ORDER BY c.ordinal;

-- Q3. TRASH contents with sizes. Reconcile deletes (DirectoryReconcile.planFromEntries ->
-- ViewModelDeleteOps.planDeleteDroppedOwnedMany, LazyLoadReconciliation.planTrashNode)
-- MOVE subtrees to TRASH, keeping their child lists. Rows here with large child_rows and
-- names like employment/finance/... prove the "missing" children were moved, not destroyed.
SELECT c.ordinal, n.id, n.name, n.kind, n.document_state, n.update_time,
       (SELECT count(*) FROM node_children x WHERE x.parent_id = n.id) AS child_rows
FROM node_children c JOIN nodes n ON n.id = c.child_id
WHERE c.parent_id = '00000000-0000-0000-0000-000000000001'
ORDER BY c.ordinal DESC
LIMIT 200;

-- Q4. Old (pre-Nov-20) life root child ids: do they still exist, where, how many children.
-- exists=false  -> the node row was removed (cascade also removed its node_children).
-- parent=TRASH  -> moved to trash with its list (restorable by moving back).
-- parent=null   -> orphaned (row exists, no parent references it).
-- child_rows>0  -> the old list survives and can be re-attached.
WITH ids(id) AS (VALUES ('daa94119-6401-48a6-be2f-04ab030b7115'::uuid),('1d286f72-5ff1-4685-854e-90477aabc8b1'::uuid),('73946aba-2933-4f99-89fd-0c001d94b781'::uuid),('560aa9cb-b081-4d92-8afe-89319b47942a'::uuid),('bf2bc0d6-9d2f-467e-9a76-203dda294375'::uuid),('fd2921c6-fc28-4125-a6ca-d7b476000ca5'::uuid),('5f6cbb5c-5b6c-47f0-8958-5428cdac20c5'::uuid),('4f984fb6-c257-49d1-a46a-d1fb198b687f'::uuid),('8ba169a3-0375-4781-9f28-3070192cf922'::uuid),('3f1140f9-5eb7-4cd6-89a2-6721a0047d3a'::uuid),('b688fef7-986d-4b92-956c-068ab5951069'::uuid),('dd898d80-8ee4-4d5c-a7f3-4abcc24c7c28'::uuid),('0ebb6c82-2117-4a3e-9773-a3f54ab52c86'::uuid),('228aeeda-5ade-41ef-b9b5-3904341b1cac'::uuid),('a17edf6c-ea34-479c-8a6f-4a57e295135e'::uuid),('80994be6-18e2-477d-85d9-3bb163252330'::uuid),('22c88c18-a289-4b6d-be63-697983a24d76'::uuid),('ba9f3aa8-3e2e-4498-896f-d3a790bf2631'::uuid),('71551738-51e6-450c-82ef-c12ec3d763e9'::uuid),('8ea369e5-7db1-49ae-a510-c29f6b7e0e24'::uuid))
SELECT ids.id, n.id IS NOT NULL AS exists, n.name, n.kind, n.document_state,
       p.parent_id, p.ownership,
       (SELECT count(*) FROM node_children c WHERE c.parent_id = ids.id) AS child_rows
FROM ids LEFT JOIN nodes n ON n.id = ids.id
LEFT JOIN node_children p ON p.child_id = ids.id
ORDER BY n.name;

-- Q5. New (Nov-20) life root child ids: parse state and child count now.
-- document_state='current' (Parsed) with child_rows=0 proves the DB itself holds
-- Parsed-with-empty-children (solid circle), i.e. real empty lists, not a client display
-- bug. document_state='unparsed' with 0 rows would draw a slashed hollow circle instead.
WITH ids(id) AS (VALUES ('928abdde-8464-4b43-a026-649b11999b95'::uuid),('e5157963-48de-4ee7-b477-ef7be906b2b1'::uuid),('f2e42546-39bd-4e41-9a52-ab20f9470938'::uuid),('7a99b3bf-fa3b-4639-aaa6-bdd767f7137b'::uuid),('02b6535a-555b-4c2c-ba01-5ee751a31760'::uuid),('54b50f5a-ba18-4cc7-9fca-2c70c9ab0487'::uuid),('99ce4261-930a-4ec1-84c7-9eaa9c0fda12'::uuid),('29d8eae9-a050-4f7e-8993-cd437ad2641f'::uuid),('5f701244-2e72-4858-9d13-4721907ca648'::uuid),('26e1693f-713a-43a9-8333-64d377313876'::uuid),('d9172901-1168-45ab-a466-bf186a52eca1'::uuid),('d29f3d0b-ecf0-4493-963e-7461cf38fc5f'::uuid),('4879b581-7c46-45bb-83d8-043bcfa0c5fa'::uuid),('67456321-6cb7-4aae-b288-7c6df2240cf8'::uuid),('4a638786-b3d1-446f-8070-27c71613eb88'::uuid),('95dac1f5-8bb9-4e95-8f6c-ce2e3cb387eb'::uuid),('9827c995-8ced-4ead-bafe-600a71ae2c92'::uuid),('a0ccb347-d96b-4126-9abb-12c1c82dc8cd'::uuid),('e616fcb4-6785-431b-b26b-5f81bdd59f57'::uuid))
SELECT ids.id, n.id IS NOT NULL AS exists, n.name, n.kind, n.document_state,
       n.persist_state, n.update_time, p.parent_id,
       (SELECT count(*) FROM node_children c WHERE c.parent_id = ids.id) AS child_rows
FROM ids LEFT JOIN nodes n ON n.id = ids.id
LEFT JOIN node_children p ON p.child_id = ids.id
ORDER BY n.name;

-- Q6. All Directory/File/Workspace nodes that are Parsed ('current') with zero children.
-- This is the candidate list of "filled circle" nodes across the whole graph.
-- (A genuinely empty file/dir also lands here; compare with disk.)
SELECT n.id, n.name, n.kind, n.update_time, p.parent_id, pn.name AS parent_name
FROM nodes n
LEFT JOIN node_children p ON p.child_id = n.id AND p.ownership = 'owner'
LEFT JOIN nodes pn ON pn.id = p.parent_id
WHERE n.kind IN ('file','directory','workspace')
  AND n.document_state = 'current'
  AND NOT EXISTS (SELECT 1 FROM node_children c WHERE c.parent_id = n.id)
ORDER BY n.update_time DESC
LIMIT 500;

-- Q7. Orphans: nodes nothing references (not root/system ids). Children that exist but
-- whose parent list no longer references them prove the "orphaned" pattern.
SELECT n.id, n.name, n.kind, n.update_time,
       (SELECT count(*) FROM node_children c WHERE c.parent_id = n.id) AS child_rows
FROM nodes n
WHERE NOT EXISTS (SELECT 1 FROM node_children p WHERE p.child_id = n.id)
  AND n.id NOT IN ('00000000-0000-0000-0000-000000000000','00000000-0000-0000-0000-000000000001',
                   '00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003')
ORDER BY child_rows DESC, n.update_time DESC
LIMIT 500;

-- Q8. Event overview for both windows: one row per event, with kind, command and authority.
-- Proves what ran (Parse changes from reconcile, Load/Save orders, notices) and in what order.
SELECT event_id, recorded_at,
       payload::jsonb->>'commandName'           AS command,
       payload::jsonb->'authority'              AS authority,
       payload::jsonb#>>'{body,kind}'           AS body_kind, payload::jsonb#>>'{body,order,kind}' AS order_kind,
       payload::jsonb#>>'{body,notice}'         AS notice,
       payload::jsonb#>>'{body,nodeId}'         AS notice_node,
       jsonb_array_length(COALESCE(payload::jsonb#>'{body,order,ops}','[]'::jsonb)) AS n_ops,
       left(payload, 300)                       AS payload_head
FROM events
WHERE recorded_at BETWEEN '2026-11-20 09:30' AND '2026-11-20 10:30'
   OR recorded_at BETWEEN '2026-11-21 13:00' AND '2026-11-21 17:00'
ORDER BY event_id;

-- Q9. Replace ops (child-list writes) in the windows, with old/new list sizes.
-- A row whose newChildren is [] (or much shorter than oldChildren) for one of our parent
-- ids is the specific event that cleared that child list. A Replace on TRASH appending
-- many ids at the same event_id proves a reconcile delete moved them.
SELECT e.event_id, e.recorded_at, e.payload::jsonb->>'commandName' AS command,
       op->>'parentId' AS parent_id, pn.name AS parent_name,
       jsonb_array_length(op->'oldChildren') AS old_n,
       jsonb_array_length(op->'newChildren') AS new_n
FROM events e
CROSS JOIN LATERAL jsonb_array_elements(COALESCE(e.payload::jsonb#>'{body,order,ops}','[]'::jsonb)) op
LEFT JOIN nodes pn ON pn.id = (op->>'parentId')::uuid
WHERE op->>'type' = 'Replace'
  AND (e.recorded_at BETWEEN '2026-11-20 09:30' AND '2026-11-20 10:30'
    OR e.recorded_at BETWEEN '2026-11-21 13:00' AND '2026-11-21 17:00')
ORDER BY e.event_id;

-- Q10. NewSpecialNode ops in the windows: proves the Nov 20 re-mint (new ids for names
-- that already existed) and the Nov 21 issue-07 stub creation, and their command name
-- ('Parse' -> created Unparsed by ChangeAmendment.markCreatedUnparsed; anything else -> Current).
SELECT e.event_id, e.recorded_at, e.payload::jsonb->>'commandName' AS command,
       op->>'nodeId' AS node_id, op->>'kind' AS kind, op->>'name' AS name
FROM events e
CROSS JOIN LATERAL jsonb_array_elements(COALESCE(e.payload::jsonb#>'{body,order,ops}','[]'::jsonb)) op
WHERE op->>'type' = 'NewSpecialNode'
  AND (e.recorded_at BETWEEN '2026-11-20 09:30' AND '2026-11-20 10:30'
    OR e.recorded_at BETWEEN '2026-11-21 13:00' AND '2026-11-21 17:00')
ORDER BY e.event_id;

-- Q11. Full per-node history for the issue-07 File (all time). Replace :issue07 with the id
-- from Q1. Expect NewSpecialNode (Parse) -> nodeUnparsed ... then on Nov 21 ~16:37 EST:
-- nodeUnpersisted + nodeParsed WITHOUT a preceding nodeParsing/Parse change, then
-- nodePersisting/nodePersisted. That sequence proves GithubTransportActor.persistFocus
-- flipped it to Parsed without reading disk and persisted empty content.
-- \set issue07 '<uuid-from-Q1>'
SELECT event_id, recorded_at, payload::jsonb->>'commandName' AS command,
       payload::jsonb#>>'{body,kind}' AS body_kind, payload::jsonb#>>'{body,order,kind}' AS order_kind, payload::jsonb#>>'{body,notice}' AS notice,
       left(payload, 400) AS payload_head
FROM events
WHERE payload LIKE '%' || :'issue07' || '%'
ORDER BY event_id;

-- Q12. Per-node history for TRASH, Workspaces, and every old+new life root child, in W1/W2.
-- For each id: the event that moved it (Replace on its parent), any nodeParsed with no
-- following Parse change, and Load/Save orders. Read with Q9 to name the clearing event.
SELECT e.event_id, e.recorded_at, e.payload::jsonb->>'commandName' AS command,
       e.payload::jsonb#>>'{body,kind}' AS body_kind, e.payload::jsonb#>>'{body,order,kind}' AS order_kind, e.payload::jsonb#>>'{body,notice}' AS notice,
       ids.id AS matched_id, left(e.payload, 300) AS payload_head
FROM events e
JOIN (VALUES ('00000000-0000-0000-0000-000000000001'),('00000000-0000-0000-0000-000000000002'),
      ('daa94119-6401-48a6-be2f-04ab030b7115'),('1d286f72-5ff1-4685-854e-90477aabc8b1'),('73946aba-2933-4f99-89fd-0c001d94b781'),('560aa9cb-b081-4d92-8afe-89319b47942a'),('bf2bc0d6-9d2f-467e-9a76-203dda294375'),('fd2921c6-fc28-4125-a6ca-d7b476000ca5'),('5f6cbb5c-5b6c-47f0-8958-5428cdac20c5'),('4f984fb6-c257-49d1-a46a-d1fb198b687f'),('8ba169a3-0375-4781-9f28-3070192cf922'),('3f1140f9-5eb7-4cd6-89a2-6721a0047d3a'),('b688fef7-986d-4b92-956c-068ab5951069'),('dd898d80-8ee4-4d5c-a7f3-4abcc24c7c28'),('0ebb6c82-2117-4a3e-9773-a3f54ab52c86'),('228aeeda-5ade-41ef-b9b5-3904341b1cac'),('a17edf6c-ea34-479c-8a6f-4a57e295135e'),('80994be6-18e2-477d-85d9-3bb163252330'),('22c88c18-a289-4b6d-be63-697983a24d76'),('ba9f3aa8-3e2e-4498-896f-d3a790bf2631'),('71551738-51e6-450c-82ef-c12ec3d763e9'),('8ea369e5-7db1-49ae-a510-c29f6b7e0e24'),('928abdde-8464-4b43-a026-649b11999b95'),('e5157963-48de-4ee7-b477-ef7be906b2b1'),('f2e42546-39bd-4e41-9a52-ab20f9470938'),('7a99b3bf-fa3b-4639-aaa6-bdd767f7137b'),('02b6535a-555b-4c2c-ba01-5ee751a31760'),('54b50f5a-ba18-4cc7-9fca-2c70c9ab0487'),('99ce4261-930a-4ec1-84c7-9eaa9c0fda12'),('29d8eae9-a050-4f7e-8993-cd437ad2641f'),('5f701244-2e72-4858-9d13-4721907ca648'),('26e1693f-713a-43a9-8333-64d377313876'),('d9172901-1168-45ab-a466-bf186a52eca1'),('d29f3d0b-ecf0-4493-963e-7461cf38fc5f'),('4879b581-7c46-45bb-83d8-043bcfa0c5fa'),('67456321-6cb7-4aae-b288-7c6df2240cf8'),('4a638786-b3d1-446f-8070-27c71613eb88'),('95dac1f5-8bb9-4e95-8f6c-ce2e3cb387eb'),('9827c995-8ced-4ead-bafe-600a71ae2c92'),('a0ccb347-d96b-4126-9abb-12c1c82dc8cd'),('e616fcb4-6785-431b-b26b-5f81bdd59f57')) ids(id)
  ON e.payload LIKE '%' || ids.id || '%'
WHERE e.recorded_at BETWEEN '2026-11-20 09:30' AND '2026-11-20 10:30'
   OR e.recorded_at BETWEEN '2026-11-21 13:00' AND '2026-11-21 17:00'
ORDER BY e.event_id, ids.id;

-- Q13. Load and Save orders anywhere since Nov 19 (body kind 'order' / load / loadSave /
-- startActor). Proves which focus each Load/Save ran on, e.g. Save on issue 07 at ~16:37 EST.
SELECT event_id, recorded_at, payload::jsonb->>'commandName' AS command,
       payload::jsonb#>>'{body,kind}' AS body_kind, payload::jsonb#>>'{body,order,kind}' AS order_kind, left(payload, 400) AS payload_head
FROM events
WHERE recorded_at >= '2026-11-19'
  AND (payload ILIKE '%"load%' OR payload ILIKE '%save%' OR payload ILIKE '%"startActor"%'
       OR payload ILIKE '%"actorOrder"%' OR payload ILIKE '%"order"%')
ORDER BY event_id;

-- ===================== Added 2026-11-23 21:0x EST =====================

-- Q14. Every event whose ops REMOVED children from a watched parent (Replace with fewer
-- children, or a child that left the list), across all time, with who sent it.
-- Watched parents: every Workspace node, TRASH, Workspaces, plus the issue-07 parent.
-- authority 'Parse' + commandName 'Parse' right after an order kind 'load' on the same
-- workspace = server reconcile (DirectoryReconcile.planFromEntries) trashing File/Directory
-- children because the folder listing was empty/missing (DocumentPersistPath
-- discoverInDirectory returns Ok [] for a folder that does not exist).
-- authority 'Browser' (or an actor name) with commandName like Delete/Move/Paste/Undo/Redo
-- = a client edit. Proves WHICH event emptied the unbased workspace and the others.
WITH watched AS (
  SELECT id FROM nodes WHERE kind IN ('workspace','workspaces','trash')
  UNION SELECT p.parent_id FROM node_children p JOIN nodes n ON n.id = p.child_id
        WHERE n.name = '07-document-the-command-line-actor.md'
),
ops AS (
  SELECT e.event_id, e.recorded_at, e.submission_id,
         e.payload::jsonb->'authority' AS authority,
         e.payload::jsonb->>'commandName' AS command,
         e.payload::jsonb#>>'{body,order,kind}' AS order_kind,
         op
  FROM events e
  CROSS JOIN LATERAL jsonb_array_elements(COALESCE(e.payload::jsonb#>'{body,order,ops}','[]'::jsonb)) op
  WHERE op->>'type' = 'Replace'
)
SELECT o.event_id, o.recorded_at, o.submission_id, o.authority, o.command, o.order_kind,
       o.op->>'parentId' AS parent_id, pn.name AS parent_name, pn.kind AS parent_kind,
       jsonb_array_length(o.op->'oldChildren') AS old_n,
       jsonb_array_length(o.op->'newChildren') AS new_n,
       (SELECT count(*) FROM jsonb_array_elements(o.op->'oldChildren') oc
         WHERE NOT EXISTS (SELECT 1 FROM jsonb_array_elements(o.op->'newChildren') nc
                           WHERE nc->>'id' = oc->>'id')) AS removed_n
FROM ops o
JOIN watched w ON w.id = (o.op->>'parentId')::uuid
LEFT JOIN nodes pn ON pn.id = w.id
WHERE (SELECT count(*) FROM jsonb_array_elements(o.op->'oldChildren') oc
        WHERE NOT EXISTS (SELECT 1 FROM jsonb_array_elements(o.op->'newChildren') nc
                          WHERE nc->>'id' = oc->>'id')) > 0
ORDER BY o.event_id;

-- Q15. The Load order (and its actor/authority) that preceded each removal in Q14:
-- nearest earlier 'load' order or startActor naming the same workspace/parent within
-- 50 events. A match proves the removal was the Load -> parse -> reconcile chain
-- (GithubTransportActor.runLoadBody -> runNeither/finishLoadOn -> startParse ->
-- CoreMailboxApi.mbLoadNode -> ParseThread.parseDirectory), not a user edit.
WITH rem AS (
  SELECT e.event_id, op->>'parentId' AS parent_id
  FROM events e
  CROSS JOIN LATERAL jsonb_array_elements(COALESCE(e.payload::jsonb#>'{body,order,ops}','[]'::jsonb)) op
  WHERE op->>'type' = 'Replace'
    AND jsonb_array_length(op->'newChildren') < jsonb_array_length(op->'oldChildren')
)
SELECT r.event_id AS removal_event, r.parent_id, pn.name,
       l.event_id AS load_event, l.recorded_at, l.payload::jsonb->'authority' AS authority,
       l.payload::jsonb#>>'{body,order,kind}' AS order_kind, left(l.payload, 300) AS load_payload
FROM rem r
LEFT JOIN nodes pn ON pn.id = r.parent_id::uuid
LEFT JOIN LATERAL (
  SELECT * FROM events l
  WHERE l.event_id < r.event_id AND l.event_id >= r.event_id - 50
    AND l.payload::jsonb#>>'{body,order,kind}' IN ('load','startActor','actorOrder')
  ORDER BY l.event_id DESC LIMIT 1) l ON true
WHERE pn.kind IN ('workspace','directory')
ORDER BY r.event_id;

-- Q16. Hit vs unhit, per Workspace: every Parsed ('current') Directory or File with ZERO
-- children, paired with a SIBLING of the same kind that still has children, plus the last
-- event that touched each. If hit nodes' last event is a Parse reconcile after a Load and
-- unhit siblings' last event is older, that proves the partial pattern is "which folders
-- were (re)loaded while missing on disk", not random corruption.
WITH RECURSIVE tree AS (
  SELECT c.child_id AS id, w.id AS ws_id, w.name AS ws_name, c.parent_id, 1 AS depth
  FROM nodes w JOIN node_children c ON c.parent_id = w.id AND c.ownership = 'owner'
  WHERE w.kind = 'workspace'
  UNION ALL
  SELECT c.child_id, t.ws_id, t.ws_name, c.parent_id, t.depth + 1
  FROM tree t JOIN node_children c ON c.parent_id = t.id AND c.ownership = 'owner'
  WHERE t.depth < 30
),
info AS (
  SELECT t.*, n.name, n.kind, n.document_state,
         (SELECT count(*) FROM node_children x WHERE x.parent_id = t.id) AS child_rows,
         (SELECT max(e.event_id) FROM events e WHERE e.payload LIKE '%' || t.id::text || '%') AS last_event
  FROM tree t JOIN nodes n ON n.id = t.id
  WHERE n.kind IN ('directory','file')
)
SELECT hit.ws_name, hit.depth, hit.parent_id,
       hit.id AS hit_id, hit.name AS hit_name, hit.kind, hit.document_state AS hit_state, hit.last_event AS hit_last_event,
       ok.id AS sibling_id, ok.name AS sibling_name, ok.child_rows AS sibling_children, ok.last_event AS sibling_last_event
FROM info hit
JOIN info ok ON ok.parent_id = hit.parent_id AND ok.kind = hit.kind AND ok.id <> hit.id AND ok.child_rows > 0
WHERE hit.document_state = 'current' AND hit.child_rows = 0
ORDER BY hit.ws_name, hit.depth, hit.parent_id, hit.name;

-- Q17. Unbased workspace check: Workspaces whose children are only TRASH-able kinds now
-- missing, and what is in TRASH that came from them (TRASH children whose last Replace
-- removed them from that workspace). Proves where the unbased workspace's children went.
SELECT w.id AS ws_id, w.name AS ws_name, e.event_id, e.recorded_at,
       e.payload::jsonb->'authority' AS authority, e.payload::jsonb->>'commandName' AS command,
       oc->>'id' AS removed_child, cn.name AS child_name, cn.kind AS child_kind,
       (SELECT parent_id FROM node_children WHERE child_id = (oc->>'id')::uuid LIMIT 1) AS now_under,
       (SELECT count(*) FROM node_children x WHERE x.parent_id = (oc->>'id')::uuid) AS its_children_now
FROM nodes w
JOIN events e ON e.payload LIKE '%"parentId":"' || w.id::text || '"%'
CROSS JOIN LATERAL jsonb_array_elements(COALESCE(e.payload::jsonb#>'{body,order,ops}','[]'::jsonb)) op
CROSS JOIN LATERAL jsonb_array_elements(op->'oldChildren') oc
LEFT JOIN nodes cn ON cn.id = (oc->>'id')::uuid
WHERE w.kind = 'workspace' AND op->>'type' = 'Replace' AND op->>'parentId' = w.id::text
  AND NOT EXISTS (SELECT 1 FROM jsonb_array_elements(op->'newChildren') nc WHERE nc->>'id' = oc->>'id')
ORDER BY w.name, e.event_id;

-- ===================== Added 2026-11-23 ~21:40 EST =====================

-- Q18. Run / actor removals. Every Replace that REMOVED children and came from a Run:
--   (a) server actor posts: authority mentions 'Actor' (RunAgentActor / SearchActor / TestActor;
--       commandName is usually ''), or
--   (b) client Amble/Expr Run: Commands.fs execAmbleRunOp -> UpdateOps.deleteChildSpan
--       (commandName 'Delete') immediately followed by the Exec/Run Replace on the same parent.
-- For each one, the nearest earlier startActor order (body.order.start.focusId = the node the Run
-- was started on, .commandId = the `?` definition node, .zoomId) within 200 events, and the
-- comparison target_is_start_focus. NOTE: an 'actorOrder' order's "focusId" field is really
-- actorNodeId (EventJson.actorOrderFields), so it is shown separately, not used for the comparison.
-- removed_in_trash / removed_orphaned: Run's Replace has no TRASH op (RunAgentActor.postAdd,
-- FocusChildrenReplace.plan, ExprRun.replaceChildren), so Run-erased children end up ORPHANED;
-- the reconcile path (Q14/Q15) moves them to TRASH. That split identifies which path did it.
WITH rem AS (
  SELECT e.event_id, e.recorded_at, e.submission_id,
         e.payload::jsonb->'authority' AS authority,
         e.payload::jsonb->>'commandName' AS command,
         e.payload::jsonb#>>'{body,order,kind}' AS order_kind,
         op
  FROM events e
  CROSS JOIN LATERAL jsonb_array_elements(COALESCE(e.payload::jsonb#>'{body,order,ops}','[]'::jsonb)) op
  WHERE op->>'type' = 'Replace'
    AND EXISTS (SELECT 1 FROM jsonb_array_elements(op->'oldChildren') oc
                WHERE NOT EXISTS (SELECT 1 FROM jsonb_array_elements(op->'newChildren') nc
                                  WHERE nc->>'id' = oc->>'id'))
),
runish AS (
  SELECT r.* FROM rem r
  WHERE r.authority::text ILIKE '%actor%'
     OR r.command IN ('Exec','Run','Delete')
)
SELECT r.event_id, r.recorded_at, r.submission_id, r.authority, r.command, r.order_kind,
       (r.op->>'parentId')::uuid AS target_id, tn.name AS target_name, tn.kind AS target_kind,
       tn.text AS target_text,
       (SELECT c.parent_id FROM node_children c WHERE c.child_id = (r.op->>'parentId')::uuid
          AND c.ownership = 'owner' LIMIT 1) AS target_parent_now,
       jsonb_array_length(r.op->'oldChildren') AS old_n,
       jsonb_array_length(r.op->'newChildren') AS new_n,
       s.event_id AS start_event, s.recorded_at AS start_at, s.payload::jsonb->'authority' AS start_authority,
       s.payload::jsonb#>>'{body,order,start,focusId}' AS start_focus_id,
       sf.text AS start_focus_text,
       s.payload::jsonb#>>'{body,order,start,commandId}' AS run_definition_id,
       dn.text AS run_definition_text,
       s.payload::jsonb#>>'{body,order,start,zoomId}' AS start_zoom_id,
       a.payload::jsonb#>>'{body,order,focusId}' AS actor_order_actor_node_id,
       (s.payload::jsonb#>>'{body,order,start,focusId}') = r.op->>'parentId' AS target_is_start_focus,
       (SELECT count(*) FROM jsonb_array_elements(r.op->'oldChildren') oc
         WHERE NOT EXISTS (SELECT 1 FROM jsonb_array_elements(r.op->'newChildren') nc WHERE nc->>'id' = oc->>'id')
           AND EXISTS (SELECT 1 FROM node_children t WHERE t.parent_id = '00000000-0000-0000-0000-000000000001'
                       AND t.child_id = (oc->>'id')::uuid)) AS removed_in_trash,
       (SELECT count(*) FROM jsonb_array_elements(r.op->'oldChildren') oc
         WHERE NOT EXISTS (SELECT 1 FROM jsonb_array_elements(r.op->'newChildren') nc WHERE nc->>'id' = oc->>'id')
           AND NOT EXISTS (SELECT 1 FROM node_children t WHERE t.child_id = (oc->>'id')::uuid)) AS removed_orphaned
FROM runish r
LEFT JOIN nodes tn ON tn.id = (r.op->>'parentId')::uuid
LEFT JOIN LATERAL (
  SELECT * FROM events s
  WHERE s.event_id < r.event_id AND s.event_id >= r.event_id - 200
    AND s.payload::jsonb#>>'{body,order,kind}' = 'startActor'
  ORDER BY s.event_id DESC LIMIT 1) s ON true
LEFT JOIN LATERAL (
  SELECT * FROM events a
  WHERE a.event_id < r.event_id AND a.event_id >= r.event_id - 200
    AND a.payload::jsonb#>>'{body,order,kind}' = 'actorOrder'
  ORDER BY a.event_id DESC LIMIT 1) a ON true
LEFT JOIN nodes sf ON sf.id = (s.payload::jsonb#>>'{body,order,start,focusId}')::uuid
LEFT JOIN nodes dn ON dn.id = (s.payload::jsonb#>>'{body,order,start,commandId}')::uuid
ORDER BY r.event_id;
-- Reading Q18: target_is_start_focus = false on an actor post would be a real wrong-target bug
-- (none found in code; see REPORT.md section 8). target_is_start_focus = true with the target
-- being a Workspace/Directory/affected node = Run was started ON that node (or on a node under a
-- `?ai` line): by-design clear-and-replace. removed_orphaned > 0 is the Run signature.

ROLLBACK;
