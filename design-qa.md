# Design QA — Mi Perfil

## Comparison target

- Source visual truth: Figma Make preview, **Mi Perfil** state: https://www.figma.com/make/yGW4sb2DzXZM7zECkchGIt/UniConnect-web-app-design?p=f&t=OEBGgLeWVEoSqvhn-0
- Implementation: browser-rendered local route `http://localhost:5180/Perfil`
- Viewport: desktop, 1280 × 720 CSS pixels, 1× density.
- State: authenticated student (`demo@usmp.pe`), no registered event activity.

## Evidence and normalization

The source and implementation were captured in the browser at desktop size. The source capture includes the Figma editor chrome, while the implementation capture uses the app viewport. The comparison was normalized to the app content region: left sidebar, top bar, profile card, statistics row, and badges section. The upper content region was readable at the captured size, so a separate focused crop was not necessary.

## Full-view comparison

- The implementation preserves the source hierarchy: dark fixed sidebar, compact top bar, wide photo cover, overlapping initials avatar, profile identity, verification mark, four activity cards, and an Insignias USMP section.
- The profile card uses session-backed name, code, and role rather than the Figma sample identity. Counts and activity state are derived from the signed-in user’s records, so the empty state is intentional for a new demo student.

## Required fidelity surfaces

- **Fonts and typography:** Inter and Poppins match the existing product typography and retain the reference’s small utility text / heavier profile-name hierarchy.
- **Spacing and layout rhythm:** Card widths, 14 px radii, cover-to-avatar overlap, 16–22 px section spacing, and four-column summary track the reference at desktop width. The layout collapses to two summary columns below 720 px.
- **Colors and visual tokens:** Existing USMP red, deep sidebar, white surfaces, gray borders, and muted text tokens map directly to the reference palette.
- **Image quality and asset fidelity:** The new local campus cover is a wide, sharp image with an intentional red overlay; it fills the same role as the Figma banner without an external image dependency. Existing USMP shield artwork is reused for verification.
- **Copy and app content:** Reference labels are retained where applicable. Identity, role, code, event state, and counts are user-specific rather than sample-only.

## Findings

- [P3] Decorative metric icons are intentionally omitted.
  Location: summary cards.
  Evidence: the Figma reference includes playful small pictograms above its metric values; the implementation prioritizes actual count and label data.
  Impact: minor visual-detail difference only; hierarchy and card layout remain clear.

- [P3] Profile editing is displayed as an unavailable control.
  Location: profile-card action area.
  Evidence: the reference shows an edit action, but the existing data model has no edit workflow.
  Impact: prevents promising an action that does not persist. A future edit form can enable this control.

## Primary interactions tested

- Sign in as the demo student.
- Open **Mi Perfil** from the sidebar.
- Verify the profile route renders session-backed identity and role.
- Verify the activity empty state offers navigation to events and tickets.

## Console errors

No page-level rendering or navigation errors were observed during the browser review.

## Implementation checklist

- [x] Add a protected `/Perfil` route.
- [x] Render session-backed profile information and ticket-derived activity.
- [x] Add responsive profile styling and local campus cover asset.
- [x] Check the browser-rendered student state against the Figma reference.

## Comparison history

1. Initial build compared against the Figma profile screen. The visual structure, responsive card grid, local banner asset, and session-backed content were implemented before the verification capture.
2. Post-build browser capture confirmed the desktop composition and empty state. No actionable P0, P1, or P2 issues remained.

final result: passed
