# Phase 8.4 Bundling Requirement

## Requirement Overview
To ensure the offline-first IMAGYN POS terminal functions reliably without internet access, all external font and icon dependencies currently loaded from Google CDNs must be bundled locally into the `POS.Desktop` application.

## Assets to Bundle
The following assets must be downloaded and served locally from `POS.Desktop/Assets/ui/fonts/`:
1. **Space Grotesk** (Weights: 500, 600, 700)
2. **Inter Tight** (Weights: 400, 500, 600, 700, 800)
3. **IBM Plex Mono** (Weights: 400, 500, 600, 700)
4. **Material Symbols Outlined** (Include standard variations used: FILL 0/1, wght 400)

## Execution Checklist for Phase 8.4
- [ ] **Phase 8.4.1 (Licensing):** Verify all fonts and the Material Symbols font use OFL (Open Font License) or Apache licenses permitting local redistribution.
- [ ] **Phase 8.4.2 (Asset Acquisition):** Download `.woff2` files for all required fonts/icons. Place them in `POS.Desktop/Assets/ui/fonts/`.
- [ ] **Phase 8.4.3 (CSS Updates):** Rewrite `<link>` tags in all 7 HTML screens to use local `@font-face` definitions pointing to the downloaded `.woff2` files.
- [ ] **Phase 8.4.4 (Verification):** Confirm no `fonts.googleapis.com` or `fonts.gstatic.com` references remain in the codebase.
- [ ] **Phase 8.4.5 (UI/UX Review):** Re-run a visual UI/UX review (online and offline) to ensure hierarchy, icons, and touch readability remain 100% intact with the local assets. No layout shifts should occur.
