# ADR 008: Unified inline settings and persistent working-state header

Date: 2026-09-16
Status: Accepted — approved static prototype; implementation authorized 2026-09-16

Use a categorized, searchable settings table with inline choices/text and a source/value inspector. Effective planet and galaxy texture rows target the selected tier; raw overrides remain discoverable separately. Reuse existing draft and persistence services, with row presentation models rather than a broad MVVM migration. Existing control mappings are retained privately as a compatibility bridge for validated editing and apply. Pending invalid input must block save/apply, survive focus changes, and support Escape cancellation. Maintenance actions move to a labelled More menu. Delivery is v1.6.0; verify with an isolated library before publishing and installing.

Unify Configure and All settings into a categorized editable settings surface with inline controls and a setting-detail inspector. Exact XML paths and unknown values remain available in Advanced. Keep Apply and applied/draft/current state in the persistent header. Expose only contextual save/update/fork actions; move infrequent maintenance to Library/More while preserving confirmation behavior. Represent current snapshot, saved revision and draft separately in the view model. Existing IO, history and benchmark contracts remain unchanged. Prototype and review before implementation; see ../REDESIGN-PLAN.md for state table and acceptance criteria.
