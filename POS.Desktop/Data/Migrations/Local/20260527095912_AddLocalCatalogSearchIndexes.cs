using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Desktop.Data.Migrations.Local
{
    /// <inheritdoc />
    public partial class AddLocalCatalogSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LocalItemVariants_TenantId_ItemId",
                table: "LocalItemVariants",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalItemVariants_TenantId_SKU",
                table: "LocalItemVariants",
                columns: new[] { "TenantId", "SKU" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalItems_TenantId_CategoryId",
                table: "LocalItems",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalItems_TenantId_Name",
                table: "LocalItems",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalItemPrices_TenantId_ItemVariantId_PriceListId",
                table: "LocalItemPrices",
                columns: new[] { "TenantId", "ItemVariantId", "PriceListId" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalItemIdentifiers_TenantId_ItemVariantId",
                table: "LocalItemIdentifiers",
                columns: new[] { "TenantId", "ItemVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalCategories_TenantId_SortOrder",
                table: "LocalCategories",
                columns: new[] { "TenantId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocalItemVariants_TenantId_ItemId",
                table: "LocalItemVariants");

            migrationBuilder.DropIndex(
                name: "IX_LocalItemVariants_TenantId_SKU",
                table: "LocalItemVariants");

            migrationBuilder.DropIndex(
                name: "IX_LocalItems_TenantId_CategoryId",
                table: "LocalItems");

            migrationBuilder.DropIndex(
                name: "IX_LocalItems_TenantId_Name",
                table: "LocalItems");

            migrationBuilder.DropIndex(
                name: "IX_LocalItemPrices_TenantId_ItemVariantId_PriceListId",
                table: "LocalItemPrices");

            migrationBuilder.DropIndex(
                name: "IX_LocalItemIdentifiers_TenantId_ItemVariantId",
                table: "LocalItemIdentifiers");

            migrationBuilder.DropIndex(
                name: "IX_LocalCategories_TenantId_SortOrder",
                table: "LocalCategories");
        }
    }
}
