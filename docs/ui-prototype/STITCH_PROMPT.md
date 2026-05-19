# Google Stitch Prompt

Copy and paste the prompt below into Google Stitch.

```text
Design a modern enterprise POS UI prototype for a desktop web app / POS terminal interface.

Product context:
This is an offline-first, multi-tenant, multi-location POS platform for enterprise retail operations. It should support a professional cashier, manager, admin, reporting, and cash-control workflow. The design must be adaptable and not supermarket-only. It should feel suitable for supermarket, retail store, pharmacy, electronics store, general multi-branch business, and future restaurant/cafe modules.

Current system naming:
- Use Locations, not Stores.
- Use Items, not Products.
- Use Orders and OrderLines, not Sales and SaleItems.
- Use CashAccounts and CashAccountMovements for Vault and Bank lifecycle.

Target users:
- Cashier: fast checkout, Item search, barcode/QR scanning, cart editing, payment, receipts, shift open/close.
- Manager: approvals, remove/void Item actions, cash drawer review, Drawer to Vault transfer, Vault to Bank deposit, reports.
- Admin: catalog management, employees, roles, Locations, Terminals, and reporting dashboards.

Visual style:
Create a desktop-first, touch-friendly, modern enterprise POS interface. Use a clean professional layout with left sidebar navigation, a top status bar, light main content area, large readable text, clear totals, strong action buttons, and clear success/warning/danger states. Use blue or dark navy as the primary color, green for payment/success, red for remove/void/danger, amber for manager approval/warning, and light gray/white neutral backgrounds. The design should feel enterprise-level and adaptable, not like a single-industry template.

Required screens:
1. Terminal Login / Employee PIN Login
2. Shift Open
3. Main POS Checkout
4. Product Search / Barcode Scan
5. Product Grid / Category Grid
6. Cart / Bill Summary
7. Customer Selection / Guest Customer
8. Payment Screen
9. Receipt / Sale Complete
10. Manager Approval Modal
11. Remove Item / Void Item Flow
12. Cash Drawer Screen
13. Drawer to Vault Transfer
14. Vault to Bank Deposit
15. Shift Close
16. Z-Report / Till Report
17. Daily Sales Report
18. Employee-wise Report
19. Product Management
20. Employee / Terminal Management Dashboard

Optional sample data for mockups only:
Company: Imtiaz
Locations: Tariq Road, Bahadurabad
Users: Adeel as Cashier, Fahad as Manager, Store Admin as Admin
Terminals: POS-01, POS-02
Currency: PKR
Items:
- Milk 1L
- Rice 5kg
- Coca Cola 1.5L
- Apple 1kg
- Cooking Oil 3L
- Sugar 1kg
- Tea Pack 500g

Optional cross-industry design cues:
- For pharmacy, the same Item grid could show medicine, health devices, and prescription-adjacent items.
- For electronics, it could show accessories, devices, warranties, and serial-number-aware Items.
- For general retail, it could show apparel, household goods, stationery, or service Items.
- For restaurant/cafe future modules, the design should be adaptable to menu Items and quick service flows.

Cash accounts:
- Tariq Road Main Vault
- Bahadurabad Main Vault
- HBL Collection Account
- Meezan Deposit Account

Example Order:
Cart has Milk 1L x2, Rice 5kg x1, Coca Cola 1.5L x2, Tea Pack 500g x1.
Subtotal PKR 3,800, discount PKR 100, tax PKR 185, total PKR 3,885, cash received PKR 4,000, change PKR 115.

Cash lifecycle to represent:
Customer pays cash -> drawer cash increases -> shift expected cash changes -> Drawer to Vault movement -> Vault to Bank movement -> balances are derived from ledger movements. Do not show manual editing of bank or vault balances.

Manager approval flow:
Show a modal for manager approval when removing or voiding an Item, checkout approval, cash correction, or protected cash movement. Include reason code, manager identity, comment, approve, and reject actions.

Reports:
Include a professional reports dashboard with Z-Report / till report, daily sales report, and employee-wise report. Reports should show totals, filters by Location and date, and drill-in style rows.

Product management:
Include Items, Categories, ItemVariants, barcode/QR identifiers, unit of measure, price, stock, item image URL, brand, manufacturer, size, and weight. Keep the UI generic enough for multiple industries.

Do not include secrets, connection strings, JWT keys, passwords, PIN values, real customer data, database dumps, or production data.
```
