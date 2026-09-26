# 015 — Validated targets and exclusive transactions

Status: Accepted, 2026-09-26. Recorded before implementation.

Writes require an existing local Windows directory ending in Frontier Developments/
Elite Dangerous/Options/Graphics, a GraphicsOptions Settings.xml and a valid Custom
schema. Relocated copies may use that same directory structure. Arbitrary folders,
network/device paths and reparse points in any ancestor are refused. This validates
structure and saved schema, not proof that the game currently uses a relocated copy.

Hold an exclusive FileShare.None handle to a persistent .egc-transaction.lock file
inside the target for Apply, Restore and recovery, before reading state. The filesystem
resolves case, relative segments and short-name aliases to the same lock file. Never
delete the lock file; the OS releases its handle on process exit. Its durable contents
identify the owning library while a transaction is pending. Another library must
refuse writes until the original library completes recovery, including after a crash.

Validate transaction/library ancestors and managed leaf paths before reads/writes.
Recheck each destination against the captured state immediately before replacement.
Recovery remains conservative on external edits. These checks do not eliminate
hostile concurrent directory swaps or coordinate unrelated external graphics tools.

Test with synthetic directories, including separate processes, abrupt termination,
reentrant operations, directory junctions and wrong-target refusal. No live game writes.
