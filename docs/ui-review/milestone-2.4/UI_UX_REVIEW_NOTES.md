# Milestone 2.4 UI/UX Review Notes

Date: 2026-05-25
Resolution used: 1366x768
Scope: Tasks 2.4.1 through 2.4.10 only

## Evidence Summary

Reference/browser screenshot evidence was captured at 1366x768 under `docs/ui-review/milestone-2.4/browser/`.

In-app WebView2 evidence is limited. A POS.Desktop smoke screenshot was captured for the initial `terminal_login.html` screen under `docs/ui-review/milestone-2.4/in-app/`. CLI-safe navigation across all seven WebView2 screens is not currently exposed without adding automation hooks, JS bridge, host objects, or test-only code, which are outside this milestone.

## Blocking Defects

No blocking UI/UX defects found.

## Non-blocking Polish Items

1. `main_checkout.html` cannot be cleanly captured by direct browser launch without pre-seeding shift session state. It triggers the existing "open shift first" guard and redirects to `shift_open.html`. This is expected demo/session behavior, but it limits direct screenshot evidence for the checkout screen.
2. The in-app WebView2 screenshot was limited to `terminal_login.html`. When the WPF outer window was forced to 1366x768, the right side of the login keypad area appeared clipped in the captured image. This should be manually rechecked with the app maximized or with a confirmed 1366x768 WebView client area before final packaging.
3. `provision_terminal.html` intentionally uses a dark left provisioning/status panel. The full product UI remains the refined white/light IMAGYN POS theme; this panel does not appear to be the discarded dark operator-terminal redesign.

## Deferred Items

1. Google Fonts and Material Symbols remain online dependencies for now. Bundling fonts/icons is deferred to Phase 8.4.
2. Full in-app automated route screenshots for all seven WebView2 screens should be deferred until a proper test harness or approved automation route exists.
3. Checkout clean-state screenshot capture should be repeated after an approved Phase 2.5/2.6 review setup can seed session state without changing production demo logic.

## Review Findings

The reviewed white/light theme has bright work surfaces, clear contrast, large touch-oriented controls, and consistent IMAGYN green usage for primary actions and active navigation. Amber and red semantic states are visible in payment warnings, shift variance, and destructive/required actions.

Fonts are consistently referenced as:

- Space Grotesk for headings and large numeric emphasis
- Inter Tight for body and operator UI text
- IBM Plex Mono for terminal IDs, receipts, numeric metadata, and audit-like values

Touch targets are generally large enough for POS terminal use. Keypads, primary CTAs, tender tabs, cash denomination controls, and the recently labeled sidebar items are all visually large and readable in the captured evidence.

Modal and overlay styling appears readable in source review and captured screen context. Receipt/Z-report paper areas remain light, which is appropriate for readability and thermal-print preview behavior.

No active `app.css` dark-theme link, Tailwind CDN, visible demo PIN hint, or simulator-only `syncParent` dependency was found in the shipped UI asset search.
