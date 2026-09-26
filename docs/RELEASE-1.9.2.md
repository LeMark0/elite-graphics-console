# 1.9.2 — Target and transaction safety (prerelease)

Apply, Restore and recovery now hold an exclusive target-file lock across libraries
and processes. Case and relative-segment aliases cannot bypass the lock. Terminating
the owning process releases the OS handle, but a durable marker blocks other libraries
until the original library recovers. Interrupted restores are now journalled as pending.

Targets must be existing local Windows folders ending in
`Frontier Developments\Elite Dangerous\Options\Graphics`. Apply also requires an
Elite GraphicsOptions Settings.xml and matching Custom schema. Relocated folders
must retain that structure; arbitrary custom folders, network/device paths and linked
ancestors are unsupported. The Apply review displays the destination. Folder structure
and saved schema cannot prove that Elite uses a relocated copy; verify your selected path.

Transaction-library, journal, backup and destination paths are checked for reparse
points. Each destination is checked again for external edits immediately before its
replacement. Other graphics tools do not participate in this lock; hostile concurrent
directory swaps and filesystem-wide atomicity are outside this guarantee.

If an operation is interrupted, open the same library (same `--data-dir`) and choose
**More → Restore previous apply** to recover. Resolve any reported external-file
conflict first. Do not delete `.egc-transaction.lock`: it normally remains as an empty
file after success and is not a game setting. A lost original library requires manual
backup inspection; automatic cross-library recovery is deliberately refused. Older
app versions do not honour this lock, so do not run them against the same target.

Validation uses synthetic folders, a separate process terminated during Apply,
competing libraries, interrupted Restore, junctions and external-edit conflicts.
No live game settings are changed. The Windows package remains unsigned; repository
protections, CI, licensing and other public-readiness milestones are still pending.
