# Sample Data for UI Prototype

This sample data is fake and intended only for UI design and prototype screens. It is optional seed data, not product positioning. Replace it freely when designing for pharmacy, electronics, general retail, restaurant/cafe, or another enterprise business type.

Do not use real customer data, passwords, PINs, connection strings, JWT keys, database backups, or production exports.

## Optional Example Company

| Field | Value |
| --- | --- |
| Company | Imtiaz |
| Currency | PKR |
| Timezone | Pakistan Standard Time |
| Business Type | Example multi-location retail operator |

## Optional Example Locations

| Code | Name | Type |
| --- | --- | --- |
| TR | Tariq Road | Branch / Location |
| BD | Bahadurabad | Branch / Location |

## Users

| Role | Name | Example UI Label |
| --- | --- | --- |
| Cashier | Adeel | Adeel - Cashier |
| Manager | Fahad | Fahad - Manager |
| Admin | Store Admin | Store Admin |

## Terminals

| Terminal | Location | Status |
| --- | --- | --- |
| POS-01 | Tariq Road | Online |
| POS-02 | Bahadurabad | Online |

## Items

These sample Items use retail-style data only to make the prototype concrete. The same UI should also work for pharmacy, electronics, services, or future restaurant/cafe Items.

| Item | Category | Unit | Size / Weight | Example Price |
| --- | --- | --- | --- | --- |
| Milk 1L | Dairy | Liter | 1L | PKR 280 |
| Rice 5kg | Grocery | Kg | 5kg | PKR 1,850 |
| Coca Cola 1.5L | Beverages | Liter | 1.5L | PKR 220 |
| Apple 1kg | Fresh Produce | Kg | 1kg | PKR 450 |
| Cooking Oil 3L | Grocery | Liter | 3L | PKR 1,650 |
| Sugar 1kg | Grocery | Kg | 1kg | PKR 180 |
| Tea Pack 500g | Grocery | Gram | 500g | PKR 950 |

## Cross-Industry Item Examples

Use these only if a more generic prototype variant is needed.

| Business Type | Example Items |
| --- | --- |
| Pharmacy | Vitamin C 500mg, Pain Relief Tablets, Digital Thermometer |
| Electronics | USB-C Cable, Wireless Mouse, Bluetooth Speaker |
| General retail | Notebook Pack, Cleaning Spray, Gift Card |
| Restaurant/cafe future module | Latte, Sandwich, Combo Meal |

## Cash Accounts

| Account | Account Type | Location |
| --- | --- | --- |
| Tariq Road Main Vault | Vault | Tariq Road |
| Bahadurabad Main Vault | Vault | Bahadurabad |
| HBL Collection Account | Bank | Company-level |
| Meezan Deposit Account | Bank | Company-level |

## Example Order

| Item | Quantity | Unit Price | Line Total |
| --- | ---: | ---: | ---: |
| Milk 1L | 2 | PKR 280 | PKR 560 |
| Rice 5kg | 1 | PKR 1,850 | PKR 1,850 |
| Coca Cola 1.5L | 2 | PKR 220 | PKR 440 |
| Tea Pack 500g | 1 | PKR 950 | PKR 950 |

| Summary Field | Value |
| --- | ---: |
| Subtotal | PKR 3,800 |
| Discount | PKR 100 |
| Tax | PKR 185 |
| Total | PKR 3,885 |
| Cash Received | PKR 4,000 |
| Change | PKR 115 |

## Example Cash Movements

| Movement | Amount | Notes |
| --- | ---: | --- |
| DrawerToVault | PKR 75,000 | POS-01 drawer cash moved to Tariq Road Main Vault |
| VaultToBank | PKR 50,000 | Deposit recorded to HBL Collection Account |
| VaultAdjustment | PKR -500 | Shortage correction with manager approval |
