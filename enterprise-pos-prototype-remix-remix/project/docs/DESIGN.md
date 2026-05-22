# Enterprise POS Prototype — Design System

> **R Technologies POS · v8.4**
> Design language, component specifications, and visual direction.

---

## 1. Design Philosophy

**Guiding principles:**

| Principle | Description |
|-----------|-------------|
| **Zero confusion** | Any cashier understands any screen in under 2 seconds |
| **Fast checkout** | Minimal clicks from scan to payment complete |
| **Safety through clarity** | Protected actions have unmistakable visual warnings |
| **Apple/iOS simplicity** | Clean layouts, high whitespace, soft surfaces, elegant type |
| **Trust through numbers** | Large, bold, monospaced totals — always visible, never ambiguous |
| **Enterprise credibility** | Professional enough for a serious multi-location operator |

---

## 2. Color System

### Primary palette

| Token | Hex | Usage |
|-------|-----|-------|
| `--navy-800` | `#0F3A7D` | Primary buttons, sidebar, nav active state, headings |
| `--navy-900` | `#05112A` | Sidebar dark background |
| `--blue-600` | `#2155B5` | Links, secondary interactive elements |

### Semantic palette

| Token | Hex | Role | Use case |
|-------|-----|------|----------|
| `--green-600` | `#10B341` | Success | Payment complete, approval granted, positive state, Pay button |
| `--red-600` | `#E74C3C` | Danger | Void item, remove, error, cancellation |
| `--amber-600` | `#F39C12` | Warning | Manager approval required, protected actions, confirmations |

### Neutral palette

| Token | Hex | Use case |
|-------|-----|---------|
| `--ink-900` | `#2C3E50` | Primary text, headings, item names |
| `--ink-800` | `#34495E` | Strong body text |
| `--ink-500` | `#7F8C8D` | Secondary text, labels, muted content |
| `--ink-400` | `#95A5A6` | Placeholder text, disabled state |
| `--ink-200` | `#E1E4E8` | Borders, dividers |
| `--surface` | `#F8F9FA` | Page background, inactive surfaces |
| `--surface-2` | `#F0F3F7` | Card backgrounds, form fields |
| `--white` | `#FFFFFF` | Card surfaces, input backgrounds |

### Special

| Color | Hex | Use case |
|-------|-----|---------|
| Gold accent | `#F39C12` / `#C9922B` | Operator avatar, sidebar active bar (brand gold from logo) |
| Sidebar bg | `#0F3A7D` | Navigation rail |
| Auth bg | `#04101F` → `#091E3D` | Login screen dark panel |

---

## 3. Typography

### Font families

| Family | Usage |
|--------|-------|
| **Plus Jakarta Sans** | All UI text — clean, iOS-inspired, premium weight range |
| **IBM Plex Mono** | All numeric values — totals, prices, SKUs, timestamps, receipt text. Applied via `.num` class or `fontFamily:'var(--mono)'` |

### Type scale

| Role | Size | Weight | Color | Usage |
|------|------|--------|-------|-------|
| Page title | 26–28px | 700 | `#2C3E50` | Screen headings |
| Card title | 18px | 600 | `#2C3E50` | Section headers |
| Body | 14–16px | 400 | `#2C3E50` | General content |
| Label | 12px | 500 | `#7F8C8D` | Form labels, uppercase metadata |
| Hint / micro | 11–11.5px | 400 | `#7F8C8D` | Status text, timestamps |
| Total (large) | 24–44px | 700 | `#0F3A7D` | Order totals, payment amounts |
| Monospace numeric | 13–28px | 500–700 | contextual | Prices, PKR values, receipt |

### Rules
- Headings: `letterSpacing: '-.01em'` to `'-.02em'` for premium feel
- Section labels: `textTransform: 'uppercase'`, `letterSpacing: '.08em'`, `fontWeight: 600`, `fontSize: 11px`
- Never use font sizes not on the scale
- Monospaced class `.num` applied to: all PKR amounts, receipt numbers, terminal codes, timestamps

---

## 4. Spacing & Layout

### Grid
- **Base unit:** 8px
- **Page padding:** 24–28px
- **Card padding:** 16–22px
- **Vertical rhythm:** 4 · 6 · 8 · 12 · 14 · 16 · 20 · 22 · 24 · 28 · 32px

### Shell dimensions

| Element | Size | Background |
|---------|------|-----------|
| Sidebar | 240px wide | `#0F3A7D` navy |
| Status bar | 56px tall | `#F8F9FA` light gray |
| Hardware strip | 28px tall | `#09192E` near-black |
| Checkout catalog | 65% of main width | `#F8F9FA` |
| Checkout cart | 35% of main width | `#FFFFFF` |

### Border radius tokens

| Token | Value | Used on |
|-------|-------|---------|
| `--radius-sm` | 6px | Form inputs, small buttons, table cells |
| `--radius-md` | 8px | Cards, standard buttons, modals |
| `--radius-lg` | 12px | Large modals, login card, overlays |
| `--radius-pill` | 20px | Category chips, badges, status pills |

---

## 5. Component Specifications

### Buttons

| Variant | Background | Text | Border | Usage |
|---------|-----------|------|--------|-------|
| `primary` | `#0F3A7D` | white | none | Main actions, confirm |
| `success` | `#10B341` | white | none | Pay, complete, approve |
| `danger` | `#E74C3C` | white | none | Void, delete, reject |
| `amber` | `#F39C12` | white | none | Manager approval, warn |
| `default` | white | `#2C3E50` | `#E1E4E8` | Secondary actions |
| `ghost` | transparent | `#2C3E50` | none | Tertiary, cancel |
| `dangerOutline` | white | `#E74C3C` | `#FECACA` | Soft danger, void line |

**Sizes:**

| Size | Height | Font | Padding | Radius |
|------|--------|------|---------|--------|
| `sm` | 30px | 12.5px | 0 10px | 6px |
| `md` | 38px | 13.5px | 0 14px | 8px |
| `lg` | 48px | 15px | 0 18px | 9px |
| `xl` | 64px | 17px | 0 22px | 10px |

**States:**
- Hover: 5% darker fill
- Active/pressed: `scale(0.96)` transform
- Disabled: `opacity: 0.5`, `cursor: not-allowed`
- Focus: `box-shadow: 0 0 0 3px rgba(15,58,125,.12)`

### Form Inputs

- **Height:** 44–48px
- **Background:** `#F8F9FA` unfocused → `#FFFFFF` focused
- **Border:** `1px solid #E1E4E8` → `1px solid #0F3A7D` focused
- **Focus ring:** `box-shadow: 0 0 0 3px rgba(15,58,125,.12)`
- **Font size:** 15–16px, `#2C3E50`
- **Placeholder:** `#BDC3C7` or `#7F8C8D`
- **Padding:** `0 14px`
- **Border radius:** 8px

### Cards

- **Background:** `#FFFFFF`
- **Border:** `1px solid #E1E4E8`
- **Border radius:** 8–12px
- **Padding:** 16–22px
- **Shadow:** `--shadow-1` default, `--shadow-2` on hover (optional)

### Badges / Pills

- **Height:** 24–28px
- **Padding:** 4px 8px
- **Border radius:** 99px (full pill)
- **Font:** 11.5px, weight 500
- Optional `dot` prop adds a 6px colored indicator dot

| Tone | Background | Text | Dot |
|------|-----------|------|-----|
| `success` | `#F0FDF4` | `#0A7A2B` | `#10B341` |
| `danger` | `#FEF2F2` | `#C0392B` | `#E74C3C` |
| `amber` | `#FFFBEB` | `#C47D0E` | `#F39C12` |
| `blue` | `#EFF6FF` | `#0F3A7D` | `#2155B5` |
| `navy` | `#0F3A7D` | white | white |
| `neutral` | `#F0F3F7` | `#4A5568` | `#7F8C8D` |
| `outline` | transparent | `#4A5568` | `#7F8C8D` |

### Modals

- **Backdrop:** `rgba(11,18,32,.5)` with `backdropFilter: blur(2px)`
- **Card:** white, `border-radius: 14px`, `box-shadow: --shadow-3`
- **Width:** 480–620px, max 92vw
- **Tone variants:**
  - `amber`: header has `linear-gradient(180deg, var(--amber-50), #fff)` tint
  - `danger`: header has `linear-gradient(180deg, var(--red-50), #fff)` tint
- **Close button:** top-right, ghost, `Icon name="close"`

### Tables

- **Row height:** 48px standard, 36px dense
- **Header:** `#F0F3F7` background, 11.5px uppercase labels, `#7F8C8D`
- **Border:** only horizontal separators, `1px solid rgba(0,0,0,.04)`
- **Hover:** pointer cursor when `onRowClick` provided
- **Numeric columns:** `fontFamily: 'var(--mono)'`, right-aligned

---

## 6. Screen Design Patterns

### Auth screens

**Split card layout:**
- 50% dark navy left panel (brand, hardware status)
- 50% white right panel (form)
- Floating over dark gradient background with radial glows
- Logo displayed in a white elevated card on the dark panel
- Gold hairline accent across top of left panel

**PIN screen (single centered card):**
- 440px white card, no split
- 6 circular PIN boxes (44px diameter, navy fill)
- Cash-register keypad: `7 8 9 / 4 5 6 / 1 2 3 / Clear 0 ⌫`
- Keys: 64px tall, full grid width, scale animation on press
- `Clear` = red tint background
- `⌫` = backspace SVG icon

### Checkout screen

**65/35 split:**
- Left 65%: `#F8F9FA` catalog workspace
- Right 35%: `#FFFFFF` order panel

**Item cards (240×220px):**
- `240×120px` gray `#E8E8E8` image zone with category-colored initial letter icon
- Item name: 14px bold `#2C3E50`
- Price: 20px bold `#0F3A7D`
- Unit: 12px gray right-aligned
- "Add to Cart" button: full-width, 40px, navy bg

**Cart / order panel:**
- No thumbnails in cart lines — pure text + controls
- Qty controls: `− qty +` with 28px buttons
- Line total: 14px bold `#0F3A7D`
- Remove: `✕` text, turns red on hover
- Totals: subtotal/tax/discount 14px gray → Total Due 24px bold navy
- Cash Received + Change: side-by-side 44px inputs
- Pay button: full-width, 48px, green `#10B341`, 18px bold

### Payment screen

**Centered overlay card (560px):**
- Semi-transparent dark backdrop
- 4 tender type buttons (Cash / Card / EMV / Wallet / Split)
- Total Due displayed at 44px bold navy — most prominent element
- Cash: quick-amount buttons row + custom input + change auto-calc
- Card: terminal waiting state with dashed placeholder
- All modes: "Complete Payment" full-width 48px navy + "Back to Cart" text link

### Manager Approval modal

**Amber-toned:**
- Two-column: left (reason + comment + audit trail), right (manager selector + PIN)
- Audit trail shows: operator, terminal, timestamp, correlation ID
- PIN: 4 boxes, uses shared `PinPad` component
- Approve button only enabled when 4-digit PIN entered
- Bottom row: Cancel · Reject · Approve

### Cash / Transfer screens

**Drawer → Vault:**
- Visual flow card: Source (terminal drawer) → Arrow → Destination (vault selector)
- Amount input, reason dropdown, escort field
- Authorization chain: Recorded → Awaiting manager → Vault verification

**Vault → Bank:**
- Source vault with balance + utilization bar
- Destination bank with account details
- 4-step verification chain shown as status cards

### Reports

**Z-Report:**
- 4 stat tiles across top
- SVG spark bar chart (hourly buckets)
- Cash reconciliation KV list
- Tender mix progress bars
- Top items list + Audit events

**Data tables:**
- Location breakdown: right-aligned numerics, trend badges
- Employee report: score progress bars, amber highlights on high voids

### Admin screens

**Item management (3-pane):**
- Left 320px: scrollable item list with search
- Center: item editor (identity, identifiers, price/tax, stock by location)
- Right 380px: activity feed / audit trail

**People & Devices:**
- Tabs: Terminals / Employees / Roles
- Terminal rows show sync status, outbox depth, last sync
- Employee rows show role badge, location, activity

---

## 7. Iconography

All icons are inline SVG paths. The `Icon` component renders a `24×24` viewBox SVG with `stroke` only (no fills). Icons used:

```
cart, cash, chart, catalog, admin, shift, search, scan, plus, minus,
close, check, chevron, chevronR, chevronL, print, user, lock, drawer,
vault, bank, receipt, sync, offline, warning, edit, trash, settings,
arrowR, arrowL, download, filter, location, terminal, grid, list, qr,
dot, star
```

**Visual style:**
- `strokeWidth: 1.75` default (1.5 for fine detail)
- `strokeLinecap: round`, `strokeLinejoin: round`
- Size: 12–22px depending on context
- Color: inherits or explicit `color` prop

---

## 8. Interaction Patterns

### Lock / logout cycle

```
Account login
     │ login success
     ▼
PIN login ◄──────────────── Lock button (status bar)
     │ pin success               │
     ▼                           │ log out
Shift open                       ▼
     │ shift opened         Account login
     ▼
Main checkout
```

### Protected action flow

```
Cashier taps "Void item"
     │
     ▼
VoidItemModal opens
     │ PKR ≥ 1000
     ▼
"Request manager" button
     │
     ▼
ManagerApprovalModal opens
     │ manager PIN entered ≥ 4 digits
     ▼
"Approve" enabled → tapped
     │
     ▼
Action committed + audit logged
Modal dismissed
```

### Checkout to payment

```
Item scan / tap → cart updates → totals recalculate
     │
     ▼
Cash Received entered → Change auto-calculated
     │
     ▼
"Pay & Complete Order" button (green, full-width 48px)
     │
     ▼
PaymentScreen (tender selection)
     │ Complete Payment
     ▼
ReceiptScreen → "New Order" → CheckoutScreen
```

### Cash lifecycle

```
POS-01 Cash Drawer
     │  Drawer → Vault transfer (manager required)
     ▼
Main Branch Vault (CashAccount type=Vault)
     │  Vault → Bank deposit (recorded manually)
     ▼
HBL Collection Account (CashAccount type=Bank)
```

---

## 9. Accessibility & Touch

- **Minimum touch target:** 44×44px on all interactive elements
- **Contrast:** Primary text `#2C3E50` on white = 8.9:1 (WCAG AAA)
- **Focus rings:** All buttons and inputs show 3px blue ring on focus
- **Font minimum:** 11px (metadata only); body text 14–16px minimum
- **PIN keypad:** Keys 64px tall for comfortable finger tap on terminal glass
- **Category chips:** 36px height, 14px+ horizontal padding
- **Cart remove button:** 22px tap target with 6px padding around `✕`

---

## 10. Responsive Behavior

This prototype targets **1440×900** (standard POS terminal) and **1920×1080** (widescreen). It does not implement true responsive breakpoints — it is desktop-first by design.

For the real Blazor/WPF implementation:
- Sidebar collapses to icon-only at `< 1200px`
- Cart panel minimum width `360px` before scrolling the catalog
- Touch-only mode (no hover states) available via CSS class on root

---

## 11. Motion & Animation

| Animation | Duration | Easing | Used on |
|-----------|----------|--------|---------|
| Button press scale | 60ms | ease | All buttons (`scale(0.94)`) |
| Hover transitions | 120–150ms | ease | Borders, backgrounds, shadows |
| PIN shake (wrong PIN) | 400ms | ease | PIN box row (`translateX`) |
| Card hover lift | 150ms | ease | Item tiles (`translateY(-2px)`) |
| Status bar time | 1000ms | — | Clock tick via `setInterval` |

No entrance animations, no page transitions — instant screen switches to match fast cashier workflow expectations.

---

## 12. Brand Integration

### imogyn Technologies logo
- Used at full width inside a **white elevated card** on dark backgrounds
- On light backgrounds (sidebar): same white card treatment
- Never placed directly on dark backgrounds without the white card container
- `filter: brightness(1.05)` for slight boost on dark panels

### Gold accent color `#C9922B` / `#F39C12`
- Used for: active sidebar indicator bar, operator avatar background
- Sourced from the gold elements in the imogyn Technologies logo
- Never used for large fills — accent use only

### Company name display
- "R Technologies POS" in status bar — bold, `#2C3E50`
- "imogyn Technologies" as logo image — never as text replacement

---

## 13. Terminology (per project spec)

| Term used in UI | Not used |
|-----------------|----------|
| **Location** | Store, Branch, Outlet |
| **Item** | Product, SKU, Article |
| **Order** | Sale, Bill, Transaction |
| **OrderLine** | Sale item, Line item, Bill line |
| **CashAccount** | Cash register, Vault account |
| **CashAccountMovement** | Deposit, Transfer record |
| **Shift** | Session, Day |

---

## 14. File → Screen Mapping

| File | Screens |
|------|---------|
| `screens-auth.jsx` | Account login, PIN login, Shift open, Shift close |
| `screens-checkout.jsx` | Checkout, Payment (×4 states), Receipt, Manager approval modal, Void item modal |
| `screens-cash.jsx` | Cash drawer, Drawer→Vault, Vault→Bank |
| `screens-reports.jsx` | Z-Report, Daily sales, Employee-wise |
| `screens-admin.jsx` | Item management, People & devices, Admin dashboard |
| `screens-extra.jsx` | Returns & refunds, Held orders |
| `app.jsx` | Shell (sidebar, status bar, hardware strip), router, all modals |
| `ui.jsx` | No screens — primitive components only |
| `data.jsx` | No screens — mock data only |
