# POS Business Flow

This document describes the main business flows for the UI prototype. It uses the current POS project naming so that the design stays aligned with the active database model and architecture.

The flows are intentionally generic. They should work for a supermarket, pharmacy, electronics store, general retail branch, or future restaurant/cafe module.

## Company / Location / Terminal Flow

```text
Company
-> Locations
-> Employees + Roles
-> Terminals
-> TerminalSession
-> Shift Open
-> Checkout
-> Shift Close / Z-Report
```

Business meaning:

- A Company is the tenant or enterprise account.
- Locations are branches, outlets, counters, or operating sites.
- Employees can have company-level or Location-level roles.
- Terminals are POS machines assigned to a Location.
- TerminalSession records which employee logged into which terminal.
- A Shift is opened before checkout starts.
- A ZReport summarizes shift or business-day totals at close.

## Order Flow

```text
Customer
-> Order
-> OrderLines
-> Payment
-> Receipt
-> Reports
```

Business meaning:

- Customer can be selected or treated as a guest customer.
- Order is the bill header, sale summary, or transaction header.
- OrderLines are the Items sold in that Order.
- Payment records cash or other tender.
- Receipt is generated after successful payment.
- Reports are derived from Orders, OrderLines, Payments, Shifts, CashDrawerMovements, and ZReports.

## Catalog Flow

```text
Category
-> Item
-> ItemVariant
-> ItemIdentifier
-> UnitOfMeasure
-> ItemPrice
-> ItemStock
```

Business meaning:

- Category groups Items for browsing and management.
- Item is the main sellable catalog record.
- ItemVariant handles sellable variations such as pack size, SKU, model, formulation, or variant label.
- ItemIdentifier stores barcode, QR code, SKU alias, or other scan identifiers.
- UnitOfMeasure supports kg, liter, piece, unit, pack, and similar units.
- ItemPrice stores price-list-based pricing.
- ItemStock tracks inventory by Item and Location.

## Cash Lifecycle

```text
Customer pays cash
-> Drawer cash increases
-> Shift expected cash changes
-> Drawer to Vault
-> Vault to Bank
-> ledger-derived cash balances
```

Business meaning:

- Cash payments are linked to the current Location, Terminal, and Shift.
- Drawer-local cash activity is represented by CashDrawerMovements.
- Shift expected cash is calculated from opening cash, payments, refunds, and drawer movements.
- Post-drawer cash is represented by CashAccounts and CashAccountMovements.
- CashAccount supports Vault and Bank account types.
- CashAccountMovement records DrawerToVault, VaultToBank, and VaultAdjustment movements.
- Cash balances should be displayed as derived values, not manually edited balances.
