using System;
using System.Collections.Generic;
using POS.Desktop.Data.LocalEntities;
using POS.Shared.Enums;

namespace POS.Desktop.Data.Seeding;

internal static class LocalCatalogSeedData
{
    private static readonly DateTimeOffset SeedDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<LocalUnitOfMeasure> UnitsOfMeasure(int tenantId) =>
    [
        new() { Id = 1, TenantId = tenantId, Code = "EACH",  Name = "Each",      MeasurementType = MeasurementType.Count,  DecimalPlaces = 0, AllowsFractionalQuantity = false },
        new() { Id = 2, TenantId = tenantId, Code = "KG",    Name = "Kilogram",  MeasurementType = MeasurementType.Weight, DecimalPlaces = 3, AllowsFractionalQuantity = true  },
    ];

    public static IReadOnlyList<LocalTaxRule> TaxRules(int tenantId) =>
    [
        new() { Id = 1, TenantId = tenantId, Code = "GST17", Name = "GST 17%", Rate = 0.17m, CalculationMode = TaxCalculationMode.Inclusive, RuleVersion = 1 },
    ];

    public static IReadOnlyList<LocalCategory> Categories(int tenantId) =>
    [
        new() { Id = 1, TenantId = tenantId, Code = "GROCERY",    Name = "Grocery",    SortOrder = 1 },
        new() { Id = 2, TenantId = tenantId, Code = "BEVERAGES",  Name = "Beverages",  SortOrder = 2 },
        new() { Id = 3, TenantId = tenantId, Code = "SNACKS",     Name = "Snacks",     SortOrder = 3 },
    ];

    public static IReadOnlyList<LocalItem> Items(int tenantId) =>
    [
        new() { Id = 101, TenantId = tenantId, ItemCode = "ITEM-001", Name = "Mineral Water 500ml",  CategoryId = 2, ItemType = ItemType.Stock, Status = ItemStatus.Active, IsTrackedInventory = false, DefaultUnitOfMeasureId = 1, DefaultTaxRuleId = 1, CatalogVersion = 1 },
        new() { Id = 102, TenantId = tenantId, ItemCode = "ITEM-002", Name = "Whole Milk 1L",         CategoryId = 1, ItemType = ItemType.Stock, Status = ItemStatus.Active, IsTrackedInventory = false, DefaultUnitOfMeasureId = 1, DefaultTaxRuleId = 1, CatalogVersion = 1 },
        new() { Id = 103, TenantId = tenantId, ItemCode = "ITEM-003", Name = "Salted Crackers 200g", CategoryId = 3, ItemType = ItemType.Stock, Status = ItemStatus.Active, IsTrackedInventory = false, DefaultUnitOfMeasureId = 1, DefaultTaxRuleId = 1, CatalogVersion = 1 },
    ];

    public static IReadOnlyList<LocalItemVariant> ItemVariants(int tenantId) =>
    [
        new() { Id = 201, TenantId = tenantId, ItemId = 101, VariantCode = "ITEM-001-DEFAULT", Name = "Mineral Water 500ml",  SKU = "SKU-001", UnitOfMeasureId = 1, TaxRuleId = 1, IsDefault = true, IsSellable = true, Status = ItemStatus.Active, CatalogVersion = 1 },
        new() { Id = 202, TenantId = tenantId, ItemId = 102, VariantCode = "ITEM-002-DEFAULT", Name = "Whole Milk 1L",         SKU = "SKU-002", UnitOfMeasureId = 1, TaxRuleId = 1, IsDefault = true, IsSellable = true, Status = ItemStatus.Active, CatalogVersion = 1 },
        new() { Id = 203, TenantId = tenantId, ItemId = 103, VariantCode = "ITEM-003-DEFAULT", Name = "Salted Crackers 200g", SKU = "SKU-003", UnitOfMeasureId = 1, TaxRuleId = 1, IsDefault = true, IsSellable = true, Status = ItemStatus.Active, CatalogVersion = 1 },
    ];

    public static IReadOnlyList<LocalItemIdentifier> ItemIdentifiers(int tenantId) =>
    [
        new() { Id = 301, TenantId = tenantId, ItemId = 101, ItemVariantId = 201, IdentifierType = "BARCODE", IdentifierValue = "5000001000010", IsPrimary = true },
        new() { Id = 302, TenantId = tenantId, ItemId = 102, ItemVariantId = 202, IdentifierType = "BARCODE", IdentifierValue = "5000002000010", IsPrimary = true },
        new() { Id = 303, TenantId = tenantId, ItemId = 103, ItemVariantId = 203, IdentifierType = "BARCODE", IdentifierValue = "5000003000010", IsPrimary = true },
    ];

    public static IReadOnlyList<LocalItemPrice> ItemPrices(int tenantId) =>
    [
        new() { Id = 401, TenantId = tenantId, PriceListId = 1, ItemVariantId = 201, UnitOfMeasureId = 1, UnitPrice =  50.00m, IsTaxIncluded = true, EffectiveFrom = SeedDate },
        new() { Id = 402, TenantId = tenantId, PriceListId = 1, ItemVariantId = 202, UnitOfMeasureId = 1, UnitPrice = 120.00m, IsTaxIncluded = true, EffectiveFrom = SeedDate },
        new() { Id = 403, TenantId = tenantId, PriceListId = 1, ItemVariantId = 203, UnitOfMeasureId = 1, UnitPrice =  75.00m, IsTaxIncluded = true, EffectiveFrom = SeedDate },
    ];

    public static IReadOnlyList<LocalTenderMethod> TenderMethods(int tenantId) =>
    [
        new() { Id = 1, TenantId = tenantId, Code = "CASH", Name = "Cash", TenderType = "Cash", AllowsChange = true,  RequiresExternalReference = false, SortOrder = 1 },
        new() { Id = 2, TenantId = tenantId, Code = "CARD", Name = "Card", TenderType = "Card", AllowsChange = false, RequiresExternalReference = true,  SortOrder = 2 },
    ];

    public static IReadOnlyList<LocalReasonCode> ReasonCodes(int tenantId) =>
    [
        new() { Id = 1, TenantId = tenantId, Code = "DISC-MGR",    Name = "Manager Discount",  ReasonCategory = "Discount", RequiresManagerApproval = true,  SortOrder = 1 },
        new() { Id = 2, TenantId = tenantId, Code = "VOID-ITEM",   Name = "Void Item",          ReasonCategory = "Void",     RequiresManagerApproval = false, SortOrder = 2 },
        new() { Id = 3, TenantId = tenantId, Code = "RETURN-STD",  Name = "Standard Return",    ReasonCategory = "Return",   RequiresManagerApproval = false, SortOrder = 3 },
    ];
}
