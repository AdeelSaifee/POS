# POS UI Brief

## Product Summary

This POS is an enterprise multi-tenant, multi-location point-of-sale platform. It is designed for organizations that operate multiple branches, outlets, counters, or service locations and need a consistent checkout, cash, employee, catalog, and reporting workflow.

The current runtime target is a desktop POS app / POS terminal interface backed by a central API and database. The design should feel appropriate for professional retail operations, not tied to one specific business type.

Suitable business types include:

- Supermarket or grocery chain
- Retail store
- Pharmacy
- Electronics store
- General multi-branch business
- Restaurant or cafe if future modules are added

Optional sample tenant for mockups:

- Company: Imtiaz
- Locations: Tariq Road, Bahadurabad
- Terminals: POS-01, POS-02

## Current Project Naming

Use the project naming in the UI prototype:

- Stores, branches, or outlets are called Locations.
- Products, services, or sellable catalog entries are called Items.
- Sales or bills are called Orders.
- Sale lines or bill lines are called OrderLines.
- Vault and bank cash storage is modeled through CashAccounts.
- Vault and bank cash movements are modeled through CashAccountMovements.

## Target Users

- Cashier: runs checkout, scans or searches Items, takes payment, opens/closes shifts.
- Manager: approves protected actions, verifies cash movements, reviews shift and performance reports.
- Admin: manages company/location setup, employees, terminals, catalog, and operational configuration.

## Main Product Goals

- Fast and reliable checkout.
- Clear shift open and shift close workflow.
- Terminal cash drawer handling.
- Drawer to Vault and Vault to Bank cash lifecycle.
- Daily sales, till, and employee-wise reporting.
- Touch-friendly desktop experience with large readable controls.
- Adaptable interface that can support multiple retail or service formats.

## Prototype Focus

The prototype should feel like a modern enterprise POS used by a serious multi-location operator. It should prioritize speed, clarity, role-aware controls, and operational confidence. Every screen should make the next cashier, manager, or admin action obvious without needing extra explanation.
