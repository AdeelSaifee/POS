using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Desktop.Data.Migrations.Local
{
    /// <inheritdoc />
    public partial class AddLocalCatalogSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentCategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalCategories", x => x.Id);
                    table.CheckConstraint("CK_LocalCategory_Code_NotEmpty", "Code <> ''");
                    table.CheckConstraint("CK_LocalCategory_Name_NotEmpty", "Name <> ''");
                    table.CheckConstraint("CK_LocalCategory_TenantId", "TenantId > 0");
                });

            migrationBuilder.CreateTable(
                name: "LocalItemIdentifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemVariantId = table.Column<int>(type: "INTEGER", nullable: true),
                    IdentifierType = table.Column<string>(type: "TEXT", nullable: false),
                    IdentifierValue = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalItemIdentifiers", x => x.Id);
                    table.CheckConstraint("CK_LocalItemIdentifier_TenantId", "TenantId > 0");
                    table.CheckConstraint("CK_LocalItemIdentifier_Value_NotEmpty", "IdentifierValue <> ''");
                });

            migrationBuilder.CreateTable(
                name: "LocalItemPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    PriceListId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemVariantId = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitOfMeasureId = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    CompareAtPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    IsTaxIncluded = table.Column<bool>(type: "INTEGER", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalItemPrices", x => x.Id);
                    table.CheckConstraint("CK_LocalItemPrice_TenantId", "TenantId > 0");
                    table.CheckConstraint("CK_LocalItemPrice_UnitPrice", "UnitPrice >= 0");
                });

            migrationBuilder.CreateTable(
                name: "LocalItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ItemType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsTrackedInventory = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultUnitOfMeasureId = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultTaxRuleId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CatalogVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalItems", x => x.Id);
                    table.CheckConstraint("CK_LocalItem_Code_NotEmpty", "ItemCode <> ''");
                    table.CheckConstraint("CK_LocalItem_Name_NotEmpty", "Name <> ''");
                    table.CheckConstraint("CK_LocalItem_TenantId", "TenantId > 0");
                });

            migrationBuilder.CreateTable(
                name: "LocalItemVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    VariantCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SKU = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UnitOfMeasureId = table.Column<int>(type: "INTEGER", nullable: false),
                    TaxRuleId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSellable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CatalogVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalItemVariants", x => x.Id);
                    table.CheckConstraint("CK_LocalItemVariant_Code_NotEmpty", "VariantCode <> ''");
                    table.CheckConstraint("CK_LocalItemVariant_TenantId", "TenantId > 0");
                });

            migrationBuilder.CreateTable(
                name: "LocalReasonCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ReasonCategory = table.Column<string>(type: "TEXT", nullable: false),
                    RequiresManagerApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalReasonCodes", x => x.Id);
                    table.CheckConstraint("CK_LocalReasonCode_Code_NotEmpty", "Code <> ''");
                    table.CheckConstraint("CK_LocalReasonCode_Name_NotEmpty", "Name <> ''");
                    table.CheckConstraint("CK_LocalReasonCode_TenantId", "TenantId > 0");
                });

            migrationBuilder.CreateTable(
                name: "LocalTaxRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", nullable: false),
                    CalculationMode = table.Column<int>(type: "INTEGER", nullable: false),
                    RuleVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalTaxRules", x => x.Id);
                    table.CheckConstraint("CK_LocalTaxRule_Code_NotEmpty", "Code <> ''");
                    table.CheckConstraint("CK_LocalTaxRule_Name_NotEmpty", "Name <> ''");
                    table.CheckConstraint("CK_LocalTaxRule_TenantId", "TenantId > 0");
                });

            migrationBuilder.CreateTable(
                name: "LocalTenderMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TenderType = table.Column<string>(type: "TEXT", nullable: false),
                    AllowsChange = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresExternalReference = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalTenderMethods", x => x.Id);
                    table.CheckConstraint("CK_LocalTenderMethod_Code_NotEmpty", "Code <> ''");
                    table.CheckConstraint("CK_LocalTenderMethod_Name_NotEmpty", "Name <> ''");
                    table.CheckConstraint("CK_LocalTenderMethod_TenantId", "TenantId > 0");
                });

            migrationBuilder.CreateTable(
                name: "LocalUnitsOfMeasure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MeasurementType = table.Column<int>(type: "INTEGER", nullable: false),
                    DecimalPlaces = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowsFractionalQuantity = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalUnitsOfMeasure", x => x.Id);
                    table.CheckConstraint("CK_LocalUom_Code_NotEmpty", "Code <> ''");
                    table.CheckConstraint("CK_LocalUom_Name_NotEmpty", "Name <> ''");
                    table.CheckConstraint("CK_LocalUom_TenantId", "TenantId > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalCategories_TenantId_Code",
                table: "LocalCategories",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalItemIdentifiers_TenantId_IdentifierValue",
                table: "LocalItemIdentifiers",
                columns: new[] { "TenantId", "IdentifierValue" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalItems_TenantId_ItemCode",
                table: "LocalItems",
                columns: new[] { "TenantId", "ItemCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalItemVariants_TenantId_VariantCode",
                table: "LocalItemVariants",
                columns: new[] { "TenantId", "VariantCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalReasonCodes_TenantId_Code",
                table: "LocalReasonCodes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalTaxRules_TenantId_Code",
                table: "LocalTaxRules",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalTenderMethods_TenantId_Code",
                table: "LocalTenderMethods",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalUnitsOfMeasure_TenantId_Code",
                table: "LocalUnitsOfMeasure",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalCategories");

            migrationBuilder.DropTable(
                name: "LocalItemIdentifiers");

            migrationBuilder.DropTable(
                name: "LocalItemPrices");

            migrationBuilder.DropTable(
                name: "LocalItems");

            migrationBuilder.DropTable(
                name: "LocalItemVariants");

            migrationBuilder.DropTable(
                name: "LocalReasonCodes");

            migrationBuilder.DropTable(
                name: "LocalTaxRules");

            migrationBuilder.DropTable(
                name: "LocalTenderMethods");

            migrationBuilder.DropTable(
                name: "LocalUnitsOfMeasure");
        }
    }
}
