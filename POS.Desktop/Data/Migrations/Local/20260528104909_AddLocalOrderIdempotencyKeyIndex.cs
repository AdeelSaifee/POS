using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Desktop.Data.Migrations.Local
{
    /// <inheritdoc />
    public partial class AddLocalOrderIdempotencyKeyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_LocalOrders_Tenant_IdempotencyKey",
                table: "LocalOrders",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_LocalOrders_Tenant_IdempotencyKey",
                table: "LocalOrders");
        }
    }
}
