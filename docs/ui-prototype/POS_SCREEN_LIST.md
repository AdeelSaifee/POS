# POS Screen List

This screen list is intended for a Google Stitch and Figma prototype. Use desktop-first layouts, large touch targets, clear totals, and fast operational interactions. The screens should feel suitable for enterprise retail operations across multiple business types, not only grocery or supermarket use.

| # | Screen | Purpose | Primary User | Main Fields / Components | Primary Actions | Related Tables |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Terminal Login / Employee PIN Login | Let an employee start a terminal session on an assigned POS terminal. | Cashier | Company, Location, Terminal code, employee number, PIN pad, login status. | Login, switch employee, request help. | Companies, Locations, Terminals, Employees, TerminalSessions |
| 2 | Shift Open | Start the business shift and declare opening cash. | Cashier | Terminal, employee, business date, opening cash amount, notes. | Open shift, cancel, request manager. | Shifts, TerminalSessions, Employees, Terminals |
| 3 | Main POS Checkout | Main transaction workspace for creating an Order. | Cashier | Item search, category grid, item grid, cart, totals, customer, payment button. | Scan/add Item, edit quantity, remove Item, select customer, take payment. | Orders, OrderLines, Items, ItemVariants, ItemIdentifiers, Customers |
| 4 | Product Search / Barcode Scan | Find Items quickly by name, SKU, barcode, QR code, or identifier. | Cashier | Search input, scan status, result list, Item image, price, stock hint. | Search, scan, add selected Item. | Items, ItemVariants, ItemIdentifiers, ItemPrices, ItemStocks |
| 5 | Product Grid / Category Grid | Touch-friendly catalog browsing for common Items. | Cashier | Category tiles, Item tiles, item images, price, unit, quick quantity controls. | Browse category, add Item, switch view. | Categories, Items, ItemVariants, ItemPrices, UnitsOfMeasure |
| 6 | Cart / Bill Summary | Show current Order details and totals. | Cashier | OrderLines, quantities, unit prices, subtotal, tax, discount, total, change due. | Update quantity, discount, remove line, hold Order, checkout. | Orders, OrderLines, TaxRules, PriceLists |
| 7 | Customer Selection / Guest Customer | Attach customer details when available. | Cashier | Customer search, phone, display name, guest name, guest phone. | Select customer, use guest customer, clear customer. | Customers, Orders |
| 8 | Payment Screen | Capture tender and finish the Order. | Cashier | Amount due, tender buttons, cash received, change, payment status. | Accept cash, split payment, complete Order, return to cart. | Payments, Orders, TenderMethods, CashDrawerMovements |
| 9 | Receipt / Sale Complete | Confirm payment and show receipt actions. | Cashier | Receipt number, total paid, change, customer, line summary, print status. | Print receipt, send receipt, start new Order, reprint. | Orders, OrderLines, Payments, ReceiptTemplates |
| 10 | Manager Approval Modal | Approve protected operational actions. | Manager | Action type, employee, reason, manager approval, comment. | Approve, reject, add comment. | ManagerActions, ReasonCodes, Employees, Orders, OrderLines |
| 11 | Remove Item / Void Item Flow | Remove or void a line with reason and authorization. | Cashier / Manager | Selected OrderLine, reason code, comment, approval status. | Request approval, void line, cancel. | OrderLines, ReasonCodes, ManagerActions |
| 12 | Cash Drawer Screen | Show current drawer activity and expected cash. | Cashier / Manager | Opening cash, cash in, refunds, payouts, drops, expected drawer cash. | Add drawer movement, payout, correction, view history. | Shifts, CashDrawerMovements, Payments |
| 13 | Drawer to Vault Transfer | Move cash from terminal drawer into a Vault account. | Manager | Source terminal, shift, destination Vault, amount, reason, authorization. | Create transfer, print slip, cancel. | CashAccountMovements, CashAccounts, CashDrawerMovements, Shifts, Terminals |
| 14 | Vault to Bank Deposit | Record physical cash deposit into a Bank account. | Manager | Source Vault, destination Bank account, amount, reference number, verified by. | Record deposit, mark verified, attach comment. | CashAccounts, CashAccountMovements, Employees |
| 15 | Shift Close | Count drawer cash and close shift. | Cashier / Manager | Expected cash, counted cash, variance, shift Orders, payment totals. | Close shift, request approval, print close summary. | Shifts, CashDrawerMovements, Payments, ZReports |
| 16 | Z-Report / Till Report | Summarize terminal, shift, or business-day totals. | Manager | Gross sales, net sales, tax, discounts, refunds, expected cash, counted cash, variance. | Generate report, print report, export. | ZReports, Shifts, Orders, Payments, CashDrawerMovements |
| 17 | Daily Sales Report | Review business-day performance by Location and tender. | Manager / Admin | Date, Location, gross/net sales, tax, discount, refunds, payment mix. | Filter, export, drill into Orders. | Orders, OrderLines, Payments, ZReports |
| 18 | Employee-wise Report | Review cashier activity and performance. | Manager / Admin | Employee, Orders, sales amount, voids, approvals, shift totals. | Filter by date/Location, export, view employee detail. | Employees, Orders, Payments, Shifts, ManagerActions |
| 19 | Product Management | Manage catalog data used at checkout. | Admin | Categories, Items, variants, barcode/QR, unit, price, stock, image URL. | Add Item, edit Item, update price, manage identifier, view stock. | Categories, Items, ItemVariants, ItemIdentifiers, UnitsOfMeasure, ItemPrices, ItemStocks |
| 20 | Employee / Terminal Management Dashboard | Manage employees, roles, and terminal assignments. | Admin | Employees, roles, Locations, terminals, device status, last seen. | Add employee, assign role, provision terminal, deactivate terminal. | Employees, EmployeeLocationRoles, Locations, Terminals, TerminalSessions |

## Prototype Navigation

Recommended main areas:

- Checkout
- Shift
- Cash
- Reports
- Catalog
- Admin

Recommended persistent layout:

- Left sidebar navigation.
- Top status bar with Company, Location, Terminal, employee, shift status, and business date.
- Main content area optimized for scanning, tapping, keyboard use, and quick decisions.
- Role-aware actions so cashier, manager, and admin workflows feel distinct but consistent.
