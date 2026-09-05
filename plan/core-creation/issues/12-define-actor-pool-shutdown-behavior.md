# Define Actor-pool shutdown behavior

Type: grilling
Status: open
Blocked by: 09, 10, 11

## Question

When the Server or Core shuts down, how are running and queued Actors cancelled or awaited, how are already-enqueued Change batches treated, and what terminal job information must remain observable before process exit without adding process crash isolation?
