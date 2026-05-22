# Enterprise POS Prototype — Architecture

> **R Technologies POS · v8.4 · Enterprise Multi-Tenant Desktop Platform**
> Document covers the prototype HTML architecture. For the real system (.NET 8 + BlazorWebView + SQLite) see `POS/ARCHITECTURE.md`.

---

## 1. Overview

This is a high-fidelity interactive HTML prototype of the full Enterprise POS desktop application. It is built entirely in the browser using React 18 + Babel (inline JSX), with no build toolchain, no bundler, and no server. All state is in-memory. It runs as a single HTML entry point loading several JSX source files.

```
Enterprise POS.html          ← Entry point, CSS tokens, script loader
src/
  ui.jsx                     ← Shared UI primitives & design system components
  data.jsx                   ← Mock data (tenants, items, employees, orders, cash)
  screens-auth.jsx           ← Login, PIN, Shift open/close screens
  screens-checkout.jsx       ← Checkout, Payment, Receipt, Modals
  screens-cash.jsx           ← Cash drawer, Drawer→Vault, Vault→Bank
  screens-reports.jsx        ← Z-Report, Daily sales, Employee-wise report
  screens-admin.jsx          ← Item management, People & devices, Admin dashboard
  screens-extra.jsx          ← Returns/Refund, Held orders
  app.jsx                    ← Root app, router, sidebar, status bar, hardware strip
assets/
  logo.png                   ← imogyn Technologies logo
```

---

## 2. Technology Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| UI framework | React 18.3.1 (UMD) | Loaded from unpkg with integrity hashes |
| JSX transpilation | Babel Standalone 7.29.0 | `type="text/babel"` script tags |
| Typography | Plus Jakarta Sans + IBM Plex Mono | Google Fonts CDN |
| State management | React `useState` / `useEffect` | Local component state only — no Redux/Context |
| Routing | Custom string-based route state | `route` string in root `App` component |
| Styling | Inline styles + CSS custom properties | Design tokens in `:root`, all layout via inline JSX styles |
| Data | In-memory JS objects | `src/data.jsx` exports mock tenant/item/order data to `window` |

---

## 3. Entry Point: `Enterprise POS.html`

### Responsibilities
- Declares all CSS custom property tokens (colors, spacing, radii, shadows, typography)
- Loads React, ReactDOM, Babel via CDN
- Loads all JSX source files in dependency order

### CSS Token System
```css
/* Primary palette — spec values */
--navy-800: #0F3A7D      /* Primary brand, buttons, nav */
--green-600: #10B341     /* Success, payment complete */
--red-600:   #E74C3C     /* Danger, void, remove */
--amber-600: #F39C12     /* Warning, manager approval */

/* Neutral palette */
--ink-900: #2C3E50       /* Primary text */
--ink-500: #7F8C8D       /* Secondary / muted text */
--line:    #E1E4E8       /* Borders, dividers */
--surface: #F8F9FA       /* Page backgrounds */

/* Radius tokens */
--radius-sm: 6px         /* Inputs, small elements */
--radius-md: 8px         /* Cards, buttons */
--radius-lg: 12px        /* Modals, large cards */
--radius-pill: 20px      /* Badges, chips */

/* Shadow tokens */
--shadow-1: 0 2px 4px rgba(0,0,0,.05)
--shadow-2: 0 4px 12px rgba(0,0,0,.10)
--shadow-3: 0 24px 56px -12px rgba(0,0,0,.22)
```

### Script Load Order
Each `<script type="text/babel">` gets its own Babel scope. Components are shared between files by exporting to `window` via `Object.assign(window, {...})` at the end of each file.

```
ui.jsx        → window.{ Icon, Btn, Badge, Card, Field, Input, Select, KV, ... }
data.jsx      → window.{ TENANT, LOCATIONS, ITEMS, EMPLOYEES, EXAMPLE_CART, ... }
screens-auth.jsx       → window.{ AccountLoginScreen, PinLoginScreen, ... }
screens-checkout.jsx   → window.{ CheckoutScreen, PaymentScreen, ReceiptScreen, ... }
screens-cash.jsx       → window.{ CashDrawerScreen, DrawerToVaultScreen, ... }
screens-reports.jsx    → window.{ ZReportScreen, DailySalesReport, EmployeeReport }
screens-admin.jsx      → window.{ ItemManagementScreen, EmployeeTerminalScreen, AdminDashboard }
screens-extra.jsx      → window.{ ReturnsScreen, HeldOrdersScreen }
app.jsx                → ReactDOM.createRoot(...).render(<App/>)
```

---

## 4. Routing Architecture

The prototype uses a simple string-based router. There is no URL routing, no history API, and no redirects. All navigation is driven by a single `route` React state variable in the root `App` component.

```
Route string         Screen rendered
─────────────────────────────────────────────────────
account-login        AccountLoginScreen (fullscreen, no chrome)
pin-login            PinLoginScreen     (fullscreen, no chrome)
shift-open           ShiftOpenScreen
checkout             CheckoutScreen
held-orders          HeldOrdersScreen
payment              PaymentScreen
receipt              ReceiptScreen
returns              ReturnsScreen
cash                 CashSection (CashDrawerScreen + SubTabs)
drawer-to-vault      DrawerToVaultSection
vault-to-bank        VaultToBankSection
shift-close          ShiftCloseScreen
reports              Reports container (Z / Daily / Employee subtabs)
catalog              ItemManagementScreen
people               EmployeeTerminalScreen
admin                AdminDashboard
```

### Shell vs Fullscreen
Routes `account-login` and `pin-login` are **fullscreen** — they render directly without the sidebar/status-bar shell. All other routes render inside the main shell layout:

```
┌──────────────────────────────────────────────────────┐
│  Sidebar (240px)  │  StatusBar (56px)                │
│                   ├──────────────────────────────────┤
│  [Logo]           │  HardwareStrip (28px) [optional] │
│  [Nav items]      ├──────────────────────────────────┤
│  [Hardware dots]  │  <renderScreen()>                │
│  [Operator]       │  (fills remaining height)        │
└──────────────────────────────────────────────────────┘
```

---

## 5. Global Navigation Helpers (PPTX / Testing)

The `App` component exposes global functions on `window` for programmatic navigation. These are used by the PPTX export tool and can also be used for automated testing.

```js
window.__posGo(route, account?)   // Navigate to any route string, optionally set employee name
window.__posReport(tab)           // Navigate to reports with specific tab ('z'|'daily'|'employee')
window.__posShowMgr()             // Open manager approval modal on checkout screen
window.__posShowVoid()            // Open void item modal on checkout screen
window.__posTender(tender)        // Navigate to payment screen with specific tender active
window.__setTender(tender)        // Set tender tab within active payment screen (set by PaymentScreen)
```

---

## 6. Component Architecture

### `src/ui.jsx` — Design System Primitives

All shared UI building blocks. Exported to `window`.

| Component | Props | Purpose |
|-----------|-------|---------|
| `Icon` | `name, size, color, strokeWidth` | SVG icon library (40+ icons, inline paths) |
| `Btn` | `variant, size, icon, full, disabled` | Button with 9 variants: primary, blue, success, danger, amber, default, ghost, outline, dangerOutline |
| `Badge` | `tone, dot` | Semantic pill badge: neutral, success, danger, amber, blue, navy, outline |
| `Card` | `padding, style` | White rounded card container |
| `Field` | `label, hint` | Form field wrapper with label + hint |
| `Input` | `icon, suffix, full` | Styled text input with icon slot |
| `Select` | `options, value, onChange, full` | Styled native select |
| `KV` | `k, v, mono, strong, big, divider` | Key-value display row |
| `SectionTitle` | `action, size` | Section heading with optional right action |
| `Table` | `columns, rows, dense, onRowClick` | Data table with column definitions |
| `Stat` | `label, value, delta, sub, accent` | Metric tile card |
| `Modal` | `open, onClose, width` | Overlay modal with backdrop blur |
| `ModalHeader` | `title, subtitle, onClose, tone` | Modal header (supports amber/danger tones) |
| `PinPad` | `onKey, large` | 3×4 numeric keypad for PIN entry |
| `Placeholder` | `label, ratio, height` | Striped image placeholder |
| `Dot` | `tone, size` | Semantic status dot |
| `fmt` | `(n, opts)` | Number formatter |
| `fmtT` | `(n)` | PKR currency formatter |

### `src/data.jsx` — Mock Data

All sample data for the prototype. No API calls. Exports to `window`:

- `TENANT` — Company info (R Technologies POS)
- `LOCATIONS` — 3 branches (Main, City, Airport)
- `TERMINALS` — 5 terminals with sync/outbox status
- `EMPLOYEES` — 7 employees (cashiers, managers, admin)
- `CATEGORIES` — 9 product categories
- `ITEMS` — 20 catalog items across all categories
- `CASH_ACCOUNTS` — 5 accounts (3 vaults, 2 banks)
- `EXAMPLE_CART` — 4-item sample order
- `RECENT_ORDERS` — 5 recent order history rows
- `REASON_CODES` — 7 manager-approval reason strings

---

## 7. Screen Modules

### `src/screens-auth.jsx`

| Component | Description |
|-----------|-------------|
| `LoginBG` | Dark navy gradient background with glow accents |
| `AuthLeft` | Shared left panel: logo card, hardware status dots, footer links |
| `AuthCard` | Split card wrapper: left panel + right form slot |
| `AccountLoginScreen` | Account + password form. Enter key submits. Loading state on button. |
| `PinLoginScreen` | 6-digit PIN entry. Cash-register keypad (7-8-9 top). Attendance modal. Shake animation on wrong PIN. Log out link. |
| `ShiftOpenScreen` | Denomination count grid + total override. Opening variance display. |
| `ShiftCloseScreen` | Cash reconciliation. Expected vs counted. Variance display. Tender mix bars. |

### `src/screens-checkout.jsx`

| Component | Description |
|-----------|-------------|
| `ItemTile` | 240×220 item card: gray image zone (240×120), category-colored initial icon, 20px price, full-width Add to Cart button |
| `CartLine` | OrderLine row: name, unit price, qty ±, line total, ✕ remove |
| `CheckoutScreen` | Main POS screen. 65/35 split. Category chips, search bar, item grid, cart with inline Cash Received + Change, Pay button |
| `ManagerApprovalModal` | Amber-toned modal. Reason dropdown, comment textarea, manager PIN, Approve/Reject buttons. Audit trail display. |
| `VoidItemModal` | Red-toned modal. Item preview, reason code, manager approval required warning. |
| `PaymentScreen` | Centered overlay card. 4 tender modes: Cash (quick amounts + change), Card (terminal waiting), Wallet (QR), Split. `initialTender` prop for PPTX navigation. |
| `ReceiptScreen` | Thermal receipt paper styling. Barcode. Reprint / Email / New Order actions. |

### `src/screens-cash.jsx`

| Component | Description |
|-----------|-------------|
| `CashDrawerScreen` | Drawer movement ledger table. 5 stat tiles. Skim recommendation card. |
| `DrawerToVaultScreen` | Source → Destination flow card. Amount input. Authorization chain. Manager approval trigger. |
| `VaultToBankScreen` | Source vault + destination bank. Deposit amount + slip ref. 4-step verification chain. |

### `src/screens-reports.jsx`

| Component | Description |
|-----------|-------------|
| `SparkBar` | Mini bar chart component (data array → bars) |
| `ZReportScreen` | Per-shift Z-Report. Hourly sales spark chart. Tender mix bars. Reconciliation summary. Audit events. Top items. |
| `DailySalesReport` | 7-day multi-location line chart. By-location breakdown table. Payment donut chart. |
| `EmployeeReport` | Cashier performance table. Score progress bars. Orders, voids, overrides, avg basket. |

### `src/screens-admin.jsx`

| Component | Description |
|-----------|-------------|
| `ItemManagementScreen` | 3-pane layout: item list, item editor (identity/identifiers/price/stock), activity feed |
| `EmployeeTerminalScreen` | Tabbed: Terminals (with sync/outbox status), Employees (roles, status), Roles & Permissions (permission chips) |
| `AdminDashboard` | Tenant overview. Sales by location bars. Cash position across all CashAccounts. Pending approvals table. Sync & device health. |

### `src/screens-extra.jsx`

| Component | Description |
|-----------|-------------|
| `ReturnsScreen` | Order lookup by receipt/ID. OrderLine checkboxes for return selection. Refund method + reason. Manager approval warning. |
| `HeldOrdersScreen` | List of paused orders with note, customer, total. Resume or discard. |

### `src/app.jsx`

| Component | Description |
|-----------|-------------|
| `HardwareStrip` | 28px dark strip: 6 hardware device dots (Printer, Drawer, Scanner, Card terminal, Sync, Network) |
| `Sidebar` | 240px navy sidebar. Logo in white card. Nav items with gold active accent. Hardware dots. Operator footer. |
| `StatusBar` | 56px bar. Company · Location · Terminal · Employee · Shift pill · Date/Time · Sync · Lock |
| `SubTabs` | Tab bar for multi-view sections (Cash, Reports) |
| `App` | Root component. Manages all route state, modal state, payment context. Exposes `window.__pos*` helpers. |
| `PrototypeNav` | Fixed bottom-right floating menu for reviewer navigation to any screen |

---

## 8. Layout System

### Shell Layout (all non-auth routes)
```
┌──────────────────────────────────────────────────────────────────┐
│  Sidebar 240px (fixed)   │  Right column (flex:1)                │
│  background: #0F3A7D     │  ┌────────────────────────────────┐   │
│                          │  │  StatusBar 56px (#F8F9FA)       │   │
│  [Logo in white card]    │  ├────────────────────────────────┤   │
│  ─────────────────       │  │  HardwareStrip 28px (#09192E)  │   │
│  [Nav items]             │  ├────────────────────────────────┤   │
│  ─────────────────       │  │                                │   │
│  [Hardware dots]         │  │  Screen content                │   │
│  [Operator footer]       │  │  (flex:1, overflow:hidden)     │   │
│                          │  │                                │   │
└──────────────────────────┴──┴────────────────────────────────┴───┘
```

### Checkout Layout (65/35)
```
┌──────────────────────────────┬───────────────────┐
│  Catalog panel (65%)         │  Order panel (35%) │
│  - Search bar (48px)         │  - Customer        │
│  - Category chips (36px)     │  - OrderLines      │
│  - Item grid (auto-fill)     │  - Totals          │
│                              │  - Cash received   │
│                              │  - Change          │
│                              │  - Pay button      │
└──────────────────────────────┴───────────────────┘
```

### Auth Layout (fullscreen)
```
┌─────────────────────────────────────────────────────────┐
│  Dark navy background (LoginBG gradient + glows)         │
│  ┌──────────────────────┬────────────────────────────┐  │
│  │  Left panel (50%)    │  Right form panel (50%)    │  │
│  │  - Logo white card   │  - Form title              │  │
│  │  - Hardware status   │  - Input fields            │  │
│  │  - Links             │  - CTA button              │  │
│  └──────────────────────┴────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## 9. Key Design Decisions

### Why inline styles throughout?
The prototype needs zero build toolchain. CSS-in-JS via inline styles makes it trivially portable — open `Enterprise POS.html` in any browser and it works. CSS custom properties (`var(--navy-800)`) are used for tokens that need to cascade.

### Why `Object.assign(window, {...})`?
Each `<script type="text/babel">` gets transpiled in its own scope by Babel Standalone. Sharing components between files requires attaching them to `window`. The pattern is safe within a controlled prototype context.

### Why string routing (not React Router)?
Zero dependencies. The prototype is self-contained. String routing is trivially inspectable, debuggable, and navigable programmatically via `window.__posGo()`.

### Append-only references
All void, refund, and return flows in the UI include explicit "append-only" language matching the real system architecture (§7.3 of `architecture.md`). Voids are new events referencing originals by ID — they never delete or overwrite.

### Manager approval pattern
Protected actions (voids, transfers, overrides) always invoke `ManagerApprovalModal` with:
- Action description
- Reason code (dropdown)
- Comment textarea
- Manager identity selector
- PIN entry (4 boxes)
- Audit trail display (operator, terminal, timestamp, correlation ID)

This matches the real system's `ManagerActions` table and `ReasonCodes` design.

---

## 10. PPTX Export Architecture

The prototype supports programmatic navigation for PPTX export via `window.__pos*` helpers. The `gen_pptx` tool calls `showJs` for each slide before capture.

**Full slide sequence (23 slides):**
1. Account login → `window.__posGo('account-login')`
2. PIN login → `window.__posGo('pin-login', 'Adeel')`
3. Shift open → `window.__posGo('shift-open')`
4. Checkout → `window.__posGo('checkout')`
5. Held orders → `window.__posGo('held-orders')`
6. Manager approval modal → `window.__posShowMgr()`
7. Void item modal → `window.__posShowVoid()`
8. Payment — Cash → `window.__posTender('cash')`
9. Payment — Card → `window.__posTender('card')`
10. Payment — Wallet → `window.__posTender('wallet')`
11. Payment — Split → `window.__posTender('split')`
12. Receipt → `window.__posGo('receipt')`
13. Returns & refunds → `window.__posGo('returns')`
14. Cash drawer → `window.__posGo('cash')`
15. Drawer → Vault → `window.__posGo('drawer-to-vault')`
16. Vault → Bank → `window.__posGo('vault-to-bank')`
17. Shift close → `window.__posGo('shift-close')`
18. Z-Report → `window.__posReport('z')`
19. Daily sales → `window.__posReport('daily')`
20. Employee report → `window.__posReport('employee')`
21. Item management → `window.__posGo('catalog')`
22. People & devices → `window.__posGo('people')`
23. Admin dashboard → `window.__posGo('admin')`
