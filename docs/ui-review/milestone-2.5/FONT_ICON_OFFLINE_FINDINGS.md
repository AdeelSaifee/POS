# Font & Icon Offline Findings & UI/UX Impact

## Fallback Behavior Analysis

### Typography (Google Fonts)
- **Primary Fonts:** Space Grotesk (Headings), Inter Tight (Body), IBM Plex Mono (Numbers/Receipts).
- **Offline Fallback:** Browsers default to system sans-serif and monospace fonts.
- **UI/UX Impact:** Loss of IMAGYN brand identity (Space Grotesk) and potential slight layout shifts due to differing font metrics. However, usability remains largely intact as text remains readable.

### Icons (Material Symbols Outlined)
- **Dependency:** The UI relies heavily on the `Material Symbols Outlined` web font. Icons are rendered using text ligatures (e.g., `<span class="material-symbols-outlined">account_circle</span>`).
- **Offline Fallback:** If the external font fails to load, the browser will render the literal ligature text (e.g., the word "account_circle" instead of the icon).
- **UI/UX Impact (CRITICAL):** 
  - **Layout Destruction:** Button widths will expand unexpectedly to fit the ligature text, breaking grids and navigation sidebars.
  - **Cashier Speed/Usability:** Touch targets become confusing. Fast, muscle-memory-driven POS operations rely on visual iconography. Reading "shopping_cart_checkout" instead of seeing a cart icon severely degrades the cashier experience.
  - **Status Indicators:** Icons used for validation (like "check_circle" or "warning") lose their immediate visual communication power.

## Conclusion
While the text font fallback is an acceptable degradation for aesthetics, the **Material Symbols fallback is a critical usability failure** for a kiosk POS system that must operate offline. This makes Phase 8.4 (Offline Asset Bundling) a strict requirement before production deployment.
