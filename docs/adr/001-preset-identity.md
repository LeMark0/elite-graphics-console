# ADR 001: Stable preset identity with update and fork

Date: 2026-09-16
Status: Accepted before implementation

The library represents named presets, not an undifferentiated list of snapshots. Update this preset keeps its identity and name and replaces the value set shown in its existing sidebar entry. Internally it appends a verified immutable revision so benchmark references and recovery remain valid. Fork always creates a new identity, even with the same name, and retains its source revision as provenance. Protected baselines can only be forked. Updating a superseded revision is rejected. Prior revisions remain available through history.

Existing files are not rewritten during migration. Legacy revisions share an identity only when an explicit parent chain connects equal names and modes; unrelated same-name snapshots stay distinct. The sidebar shows latest revisions; comparison and history can still access older revisions.
