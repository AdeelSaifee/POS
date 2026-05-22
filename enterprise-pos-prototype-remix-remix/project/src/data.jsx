// Mock data — multi-tenant POS. Generic across business types.

const TENANT = {
  id: "ten_eretail",
  name: "Enterprise Retail Co.",
  currency: "PKR",
  timezone: "Pakistan Standard Time",
  industry: "Multi-format retail",
};

const LOCATIONS = [
  { id: "L01", code: "MB", name: "Main Branch", terminals: 4, openShifts: 3, status: "open" },
  { id: "L02", code: "CB", name: "City Branch", terminals: 3, openShifts: 2, status: "open" },
  { id: "L03", code: "AB", name: "Airport Branch", terminals: 2, openShifts: 0, status: "closed" },
];

const TERMINALS = [
  { id: "POS-01", location: "Main Branch", status: "online", lastSync: "12s ago", outbox: 0, operator: "Adeel", shift: "S-4218" },
  { id: "POS-02", location: "City Branch",  status: "online", lastSync: "9s ago",  outbox: 0, operator: "Sana",  shift: "S-4219" },
  { id: "POS-03", location: "Main Branch", status: "online", lastSync: "1m ago",  outbox: 2, operator: "Bilal", shift: "S-4220" },
  { id: "POS-04", location: "City Branch",  status: "offline", lastSync: "14m ago", outbox: 9, operator: "—",     shift: "—" },
  { id: "POS-05", location: "Airport Branch", status: "deprovisioning", lastSync: "2h ago", outbox: 0, operator: "—", shift: "—" },
];

const EMPLOYEES = [
  { id: "E1042", name: "Adeel",  role: "Cashier", location: "Main Branch", status: "active", shifts: 142, lastActive: "now" },
  { id: "E1043", name: "Fahad",  role: "Manager", location: "Main Branch", status: "active", shifts: 28,  lastActive: "5m" },
  { id: "E1044", name: "Sana",   role: "Cashier", location: "City Branch",  status: "active", shifts: 96,  lastActive: "now" },
  { id: "E1045", name: "Bilal",  role: "Cashier", location: "Main Branch", status: "active", shifts: 71,  lastActive: "12m" },
  { id: "E1046", name: "Hira",   role: "Manager", location: "City Branch",  status: "active", shifts: 22,  lastActive: "2h" },
  { id: "E1047", name: "Store Admin", role: "Admin",   location: "All",        status: "active", shifts: 0,   lastActive: "now" },
  { id: "E1048", name: "Usman",  role: "Cashier", location: "Airport Branch", status: "suspended", shifts: 12, lastActive: "3d" },
];

const CATEGORIES = [
  { id: "all",      name: "All Items", count: 412 },
  { id: "grocery",  name: "Grocery",   count: 168 },
  { id: "dairy",    name: "Dairy",     count: 24 },
  { id: "beverage", name: "Beverages", count: 56 },
  { id: "personal", name: "Personal Care", count: 41 },
  { id: "pharmacy", name: "Pharmacy",  count: 38 },
  { id: "electron", name: "Electronics", count: 49 },
  { id: "stationery", name: "Stationery", count: 24 },
  { id: "service",  name: "Services",  count: 12 },
];

const ITEMS = [
  { id: "I1001", sku: "MLK-1L",   name: "Milk 1L",            cat: "dairy",     unit: "L",   price: 280, tax: 5, stock: 84,  variants: 1 },
  { id: "I1002", sku: "RIC-5KG",  name: "Rice 5kg",           cat: "grocery",   unit: "kg",  price: 1850, tax: 0, stock: 26, variants: 2 },
  { id: "I1003", sku: "USB-C-1M", name: "USB-C Cable 1m",     cat: "electron",  unit: "ea",  price: 650, tax: 18, stock: 41, variants: 3 },
  { id: "I1004", sku: "THM-DIG",  name: "Digital Thermometer", cat: "pharmacy", unit: "ea", price: 1200, tax: 0, stock: 12, variants: 1 },
  { id: "I1005", sku: "NB-PK5",   name: "Notebook Pack ×5",   cat: "stationery", unit: "pk", price: 480, tax: 12, stock: 58, variants: 1 },
  { id: "I1006", sku: "MSE-WL",   name: "Wireless Mouse",     cat: "electron",  unit: "ea",  price: 1450, tax: 18, stock: 19, variants: 4 },
  { id: "I1007", sku: "BIS-200",  name: "Biscuit 200g",       cat: "grocery",   unit: "g",   price: 120, tax: 0, stock: 124, variants: 1 },
  { id: "I1008", sku: "COL-15L",  name: "Cola 1.5L",          cat: "beverage",  unit: "L",   price: 220, tax: 12, stock: 96, variants: 1 },
  { id: "I1009", sku: "SHM-400",  name: "Shampoo 400ml",      cat: "personal",  unit: "ml",  price: 720, tax: 18, stock: 33, variants: 2 },
  { id: "I1010", sku: "PNK-500",  name: "Paracetamol 500mg",  cat: "pharmacy",  unit: "pk",  price: 95,  tax: 0, stock: 210, variants: 1 },
  { id: "I1011", sku: "BAT-AA4",  name: "Batteries AA ×4",    cat: "electron",  unit: "pk",  price: 320, tax: 18, stock: 47, variants: 1 },
  { id: "I1012", sku: "OIL-3L",   name: "Cooking Oil 3L",     cat: "grocery",   unit: "L",   price: 1650, tax: 0, stock: 22, variants: 1 },
  { id: "I1013", sku: "PEN-BLK",  name: "Pen Black",          cat: "stationery", unit: "ea", price: 45,  tax: 12, stock: 280, variants: 3 },
  { id: "I1014", sku: "SOA-150",  name: "Soap Bar 150g",      cat: "personal",  unit: "g",   price: 110, tax: 18, stock: 156, variants: 2 },
  { id: "I1015", sku: "TEA-500",  name: "Tea Pack 500g",      cat: "grocery",   unit: "g",   price: 950, tax: 0, stock: 38, variants: 1 },
  { id: "I1016", sku: "WAT-15L",  name: "Mineral Water 1.5L", cat: "beverage",  unit: "L",   price: 90,  tax: 0, stock: 412, variants: 1 },
  { id: "I1017", sku: "ENG-CRT",  name: "Engine Oil 1L",      cat: "electron",  unit: "L",   price: 1850, tax: 18, stock: 8, variants: 2 },
  { id: "I1018", sku: "GFT-CRD",  name: "Gift Card PKR 1000", cat: "service",   unit: "ea",  price: 1000, tax: 0, stock: 999, variants: 4 },
  { id: "I1019", sku: "DET-2KG",  name: "Detergent 2kg",      cat: "personal",  unit: "kg",  price: 980, tax: 18, stock: 64, variants: 1 },
  { id: "I1020", sku: "BRD-WHT",  name: "Bread White",        cat: "grocery",   unit: "ea",  price: 180, tax: 0, stock: 31, variants: 1 },
];

const CASH_ACCOUNTS = [
  { id: "CA01", name: "Main Branch Vault",     type: "Vault", location: "Main Branch", balance: 482500, lastMovement: "2h ago" },
  { id: "CA02", name: "City Branch Vault",     type: "Vault", location: "City Branch",  balance: 318750, lastMovement: "4h ago" },
  { id: "CA03", name: "Airport Branch Vault",  type: "Vault", location: "Airport Branch", balance: 76200, lastMovement: "1d ago" },
  { id: "CA04", name: "HBL Collection Account", type: "Bank",  location: "Company-level", balance: 4280000, lastMovement: "today" },
  { id: "CA05", name: "Meezan Deposit Account", type: "Bank",  location: "Company-level", balance: 1950000, lastMovement: "yesterday" },
];

// Example active order
const EXAMPLE_CART = [
  { id: "I1001", name: "Milk 1L",          qty: 2, unit: "L",  price: 280 },
  { id: "I1002", name: "Rice 5kg",         qty: 1, unit: "kg", price: 1850 },
  { id: "I1008", name: "Cola 1.5L",        qty: 2, unit: "L",  price: 220 },
  { id: "I1015", name: "Tea Pack 500g",    qty: 1, unit: "g",  price: 950 },
];

const RECENT_ORDERS = [
  { id: "ORD-MB-20260519-00482", time: "14:38", terminal: "POS-01", cashier: "Adeel", items: 7, total: 4280, tender: "Cash", status: "completed" },
  { id: "ORD-MB-20260519-00481", time: "14:31", terminal: "POS-01", cashier: "Adeel", items: 3, total: 1240, tender: "Card", status: "completed" },
  { id: "ORD-MB-20260519-00480", time: "14:24", terminal: "POS-03", cashier: "Bilal", items: 12, total: 8950, tender: "Split", status: "completed" },
  { id: "ORD-MB-20260519-00479", time: "14:18", terminal: "POS-01", cashier: "Adeel", items: 2, total: 530, tender: "Cash", status: "voided" },
  { id: "ORD-MB-20260519-00478", time: "14:12", terminal: "POS-03", cashier: "Bilal", items: 5, total: 2410, tender: "Card", status: "completed" },
];

const REASON_CODES = [
  "Customer changed mind",
  "Wrong item scanned",
  "Price discrepancy",
  "Item damaged",
  "Manager discount",
  "Promotional adjustment",
  "Cash float correction",
];

Object.assign(window, {
  TENANT, LOCATIONS, TERMINALS, EMPLOYEES, CATEGORIES, ITEMS, CASH_ACCOUNTS,
  EXAMPLE_CART, RECENT_ORDERS, REASON_CODES,
});
