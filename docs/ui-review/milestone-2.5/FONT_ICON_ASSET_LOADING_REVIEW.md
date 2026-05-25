# Font & Icon Asset Loading Review

## Overview
This document reviews the external font and icon dependencies for the POS.Desktop application across its 7 production screens (`POS.Desktop/Assets/ui/*.html`), in preparation for Phase 8.4 (offline bundling).

## Dependency Map

| Screen | Google Fonts Preconnect | Google Fonts Stylesheet (Space Grotesk, Inter Tight, IBM Plex Mono) | fonts.gstatic Preconnect | Material Symbols Stylesheet | Notes |
|---|---|---|---|---|---|
| `cash_control.html` | Yes | Yes | Yes | Yes | Uses UI/UX critical action icons |
| `main_checkout.html` | Yes | Yes | Yes | Yes | Heavy icon usage in sidebar, cart, checkout |
| `payment_screen.html` | Yes | Yes | Yes | Yes | Tender type icons |
| `provision_terminal.html` | Yes | Yes | Yes | **No** | No Material Symbols used |
| `shift_close.html` | Yes | Yes | Yes | Yes | Icons for navigation and metrics |
| `shift_open.html` | Yes | Yes | Yes | Yes | Navigation icons |
| `terminal_login.html` | Yes | Yes | Yes | Yes | Auth/keypad and status icons |

## Runtime Network Observation
- **Online Observation**: PASS WITH LIMITED RUNTIME NETWORK EVIDENCE. Static inspection confirms valid `<link>` tags pointing to Google's CDN. 
- **Offline Observation**: PASS WITH LIMITED RUNTIME NETWORK EVIDENCE. Offline network blocking cannot be safely automated in this CLI. However, the exact degradation behavior is known and documented in `FONT_ICON_OFFLINE_FINDINGS.md`.

## Local Asset Check
- All `logo.png` relative references (`src="logo.png"`) were verified.
- The target `POS.Desktop/Assets/ui/logo.png` exists in the local project and build output.
- No local-asset 404s are expected.
