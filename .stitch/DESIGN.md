# Design System: Enterprise POS Platform
**Project ID:** 11547690661215488576

## 1. Visual Theme & Atmosphere
The interface is a desktop-first, dense, highly efficient, and professional POS terminal layout. It is optimized for high-speed touch input, barcode scanning, and keyboard navigation. The atmosphere is clean, structured, and operational, offering cashiers and managers a calm, distraction-free environment for high-volume retail work.

## 2. Color Palette & Roles
* **Deep Slate Navy (#0F172A):** Primary color, used for the persistent left sidebar navigation and headers to establish a solid hierarchy.
* **Pure White (#FFFFFF):** Main content area backgrounds.
* **Light Ice Gray (#F8FAFC):** Secondary content backgrounds, input areas, and table alternate rows.
* **Emerald Green (#10B981):** Success indicator, complete checkout, cash received, and positive financial balance changes.
* **Rose Red (#EF4444):** Danger indicator, void order, delete line item, cancel actions, and error states.
* **Amber Orange (#F59E0B):** Warning indicator, manager authorization required, and pending states.
* **Slate Gray (#1E293B):** Primary high-contrast text color for labels, product names, and totals.
* **Cool Gray (#64748B):** Secondary medium-contrast text for SKU numbers, subtitle labels, and helper descriptions.

## 3. Typography Rules
* **Font Family:** Inter (sans-serif) across the entire application for maximum readability.
* **Headings:** Bold weight, Deep Slate Navy (#0F172A), used sparingly to clearly separate functional zones.
* **Numeric Displays:** Monospaced or high-clarity sans-serif numbers, extra-large font size, bold weight. Used for grand totals, cash change, and quantity counters to eliminate checkout errors.
* **Body Text:** Regular weight, Slate Gray (#1E293B), sized at 14px or 16px to ensure readability on POS screens.

## 4. Component Stylings
* **Buttons:**
  * Shape: Subtly rounded corners (8px corner radius).
  * Main Action Buttons (e.g., Pay, Open Shift): Solid Emerald Green (#10B981) background with bold white text.
  * Danger Actions (e.g., Void, Log Out): Solid Rose Red (#EF4444) background or outline with bold white/red text.
  * Standard Utility Buttons: Light Ice Gray (#F8FAFC) background with Slate Gray (#1E293B) text and a thin border.
* **Cards/Containers:**
  * Flat design with thin borders (#E2E8F0) and whisper-soft diffused shadows.
  * White or Light Ice Gray background with subtly rounded corners (8px).
* **Inputs/Forms:**
  * Background: Pure White (#FFFFFF) or Light Ice Gray (#F8FAFC).
  * Border: Thin Cool Gray border (#CBD5E1), turning Deep Slate Navy (#0F172A) on focus.
  * Large touch-friendly input fields with clear placeholders.

## 5. Layout Principles
* **Structure:** Left sidebar navigation (Deep Slate Navy, width 240px) with icons and text labels.
* **Top Status Bar:** High-visibility bar with Company Name (Imtiaz), Location, Terminal ID, Active User, and Shift Status (Open/Closed).
* **Workspace:** Large multi-column checkout grid. Cart panel on the left (or right) occupying 40% of the screen width, and the catalog browser/search grid occupying 60%.
* **Spacing:** Dense grid alignment to fit full operations on a single screen without vertical scrolling of the main layout container. Grid columns have standard 16px padding.
