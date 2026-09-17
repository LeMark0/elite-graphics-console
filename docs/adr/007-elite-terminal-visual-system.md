# ADR 007: Elite terminal visual system

Date: 2026-09-16
Status: Accepted — approved static prototype; implementation authorized 2026-09-16

Implement the reviewed warm-black station-terminal theme in shared WPF resources. Use filled vector triangles for selectors, square controls, orange selection with dark text and cyan keyboard focus. Keep Segoe UI for dependable Windows scaling. Apply resources to dialogs and comparison as well as the main window. Animations remain deferred.

Use flat station-service composition with cockpit row/control patterns from the seven supplied screenshots. Coriolis provides a secondary desktop density reference. Adopt warm near-black surfaces, orange selection, square primitives, thin separators, compact aligned typography and consistent vector icons. Apply semantic roles rather than indiscriminate orange. Validate legibility, focus and DPI scaling before accepting exact tokens. Keep WPF and existing configuration services. Defer animation until static workflows pass review. Details and reference mapping: ../REDESIGN-PLAN.md.
