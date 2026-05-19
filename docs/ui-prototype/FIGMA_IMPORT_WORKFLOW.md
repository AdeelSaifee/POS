# Figma Import Workflow

Use this workflow to move from Google Stitch output to a polished Figma prototype for an enterprise POS platform.

## Workflow

1. Use Google Stitch to generate the POS UI screens from `STITCH_PROMPT.md`.
2. Review the generated screens for desktop layout, readability, adaptability, and flow coverage.
3. Export, copy, or paste the generated UI into Figma if Stitch provides direct design export.
4. If direct Figma export is not available, use the generated HTML/code and import it with an HTML-to-Figma workflow or plugin.
5. In Figma, polish spacing, typography, component consistency, colors, icons, responsive desktop states, and interaction states.
6. Convert repeated UI pieces into reusable components:
   - top status bar
   - left sidebar navigation
   - Item tile
   - cart row
   - total summary panel
   - payment button
   - manager approval modal
   - report metric card
   - cash movement form
7. Create a clickable prototype for the main cashier, manager, admin, reporting, and cash lifecycle flows.
8. Review the prototype with the senior team and adjust based on feedback.

## Clickable Prototype Checklist

- Login flow clickable.
- Shift open clickable.
- Checkout clickable.
- Product search and barcode/QR scan path clickable.
- Cart and bill summary clickable.
- Payment clickable.
- Receipt / sale complete clickable.
- Manager approval modal clickable.
- Remove Item / void Item clickable.
- Cash drawer screen clickable.
- Drawer to Vault clickable.
- Vault to Bank clickable.
- Shift close clickable.
- Reports dashboard clickable.
- Product management path clickable.
- Employee / terminal dashboard path clickable.

## Design Review Checklist

- The UI feels like a modern enterprise POS, not a single-industry template.
- The layout can adapt to supermarket, pharmacy, electronics, general retail, and future restaurant/cafe workflows.
- Cashier can complete an Order without confusion.
- Totals, cash received, and change are highly visible.
- Remove/void actions use danger styling.
- Manager approval uses warning styling.
- Payment success uses success styling.
- Top status bar clearly shows Company, Location, Terminal, employee, shift status, and business date.
- The prototype uses current system naming: Locations, Items, Orders, OrderLines, CashAccounts, CashAccountMovements.

## Security and Data Rules

- Do not include real customer data.
- Do not include database connection strings.
- Do not include JWT keys.
- Do not include passwords or PINs.
- Do not include `.bak` files or database dumps.
- These files are only for UI/design/prototype planning.
