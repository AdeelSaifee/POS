# Designer-Friendly DB Flow Summary

This is a simple explanation of the current database flow for UI and prototype work. It avoids implementation details and focuses on what each group means in real business operations.

The same structure can support supermarkets, pharmacies, electronics stores, general retail chains, and other multi-location operators.

## Company / Tenant

Tables:

- Companies

What it means:

- The Company is the tenant, enterprise account, or business owner.

How it appears in UI:

- Company name/logo in the header, login screen, reports, and admin dashboards.

Short example:

- A multi-location business with PKR as its default currency and a shared catalog policy.

## Locations

Tables:

- Locations

What it means:

- A Location is a branch, outlet, store, counter, or operating site.

How it appears in UI:

- Location selector, terminal header, report filters, employee assignment screens.

Short example:

- A business may have Tariq Road, Bahadurabad, Warehouse, or Service Counter as Locations.

## Employees / Roles

Tables:

- Employees
- EmployeeLocationRoles

What it means:

- Employees are users of the system. Roles can be company-level or Location-level.

How it appears in UI:

- Login PIN screen, cashier name in top bar, manager approval modal, employee management dashboard.

Short example:

- A cashier works at one Location. A manager can approve protected actions across one or more Locations.

## Terminals / Login

Tables:

- Terminals
- TerminalSessions

What it means:

- A Terminal is a POS machine assigned to a Location. A TerminalSession records who logged into which terminal.

How it appears in UI:

- Terminal login screen, terminal status badge, shift header, device management screen.

Short example:

- POS-01 at a Location is logged in by a cashier for the current shift.

## Product / Catalog

Tables:

- Categories
- Items
- ItemVariants
- ItemIdentifiers
- UnitsOfMeasure
- ItemPrices
- ItemStocks

What it means:

- Catalog data controls what employees can scan, browse, price, and sell.

How it appears in UI:

- Category grid, Item grid, barcode/QR scan, search, stock hints, product management.

Short example:

- An Item can be a retail product, pharmacy item, electronics accessory, service item, or future menu item. Its identifier can be a barcode, QR code, SKU, or other scannable value.

## Orders

Tables:

- Orders
- OrderLines
- Payments
- Customers

What it means:

- An Order is the bill or transaction header. OrderLines are the Items in the bill. Payments record how the customer paid.

How it appears in UI:

- Cart, bill summary, payment screen, receipt, daily sales report.

Short example:

- A customer buys several Items. The Order stores the total and payment state. OrderLines store each sold Item.

## Cash / Shift

Tables:

- Shifts
- CashDrawerMovements
- ZReports

What it means:

- A Shift tracks terminal work for a business date. CashDrawerMovements track drawer-local cash activity. ZReports summarize shift or day totals.

How it appears in UI:

- Shift open, cash drawer screen, shift close, till report, Z-report.

Short example:

- A cashier opens a terminal with an opening cash amount. During checkout, cash payments and drawer movements change expected drawer cash.

## Vault / Bank

Tables:

- CashAccounts
- CashAccountMovements

What it means:

- CashAccounts represent Vault and Bank destinations. CashAccountMovements record movement from drawer to Vault and from Vault to Bank.

How it appears in UI:

- Drawer to Vault transfer, Vault to Bank deposit, cash balances, deposit reference history.

Short example:

- Cash is moved from a terminal drawer to a Vault account, then later deposited from that Vault account into a Bank account.

## Manager

Tables:

- ReasonCodes
- ManagerActions

What it means:

- ReasonCodes explain why an action happened. ManagerActions record approvals and overrides.

How it appears in UI:

- Manager approval modal, remove item flow, void item flow, cash correction, approval history.

Short example:

- A protected action requires a reason and manager approval, then the approval is recorded as a ManagerAction.
