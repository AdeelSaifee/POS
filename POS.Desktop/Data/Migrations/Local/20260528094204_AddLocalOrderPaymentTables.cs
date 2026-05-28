using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Desktop.Data.Migrations.Local
{
    /// <inheritdoc />
    public partial class AddLocalOrderPaymentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemVariantId = table.Column<int>(type: "INTEGER", nullable: true),
                    OriginalOrderLineId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReasonCodeId = table.Column<int>(type: "INTEGER", nullable: true),
                    AuthorizedByEmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    LineType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    SKU = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Barcode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    VariantName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UnitOfMeasureCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    NetAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxRuleId = table.Column<int>(type: "INTEGER", nullable: true),
                    TaxRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    PriceListId = table.Column<int>(type: "INTEGER", nullable: true),
                    CatalogVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalOrderLines", x => x.Id);
                    table.CheckConstraint("CK_LocalOrderLine_GrossAmount", "GrossAmount >= 0");
                    table.CheckConstraint("CK_LocalOrderLine_LineNumber", "LineNumber > 0");
                    table.CheckConstraint("CK_LocalOrderLine_LocationId", "LocationId > 0");
                    table.CheckConstraint("CK_LocalOrderLine_NetAmount", "NetAmount >= 0");
                    table.CheckConstraint("CK_LocalOrderLine_Quantity", "Quantity > 0");
                    table.CheckConstraint("CK_LocalOrderLine_TenantId", "TenantId > 0");
                    table.CheckConstraint("CK_LocalOrderLine_TerminalId", "TerminalId > 0");
                    table.CheckConstraint("CK_LocalOrderLine_UnitPrice", "UnitPrice >= 0");
                });

            migrationBuilder.CreateTable(
                name: "LocalOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OriginalOrderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TerminalSequence = table.Column<long>(type: "INTEGER", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OrderType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PaymentStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FulfillmentStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CatalogVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    PriceListId = table.Column<int>(type: "INTEGER", nullable: true),
                    RuleVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    ReceiptTemplateId = table.Column<int>(type: "INTEGER", nullable: true),
                    SubtotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    GuestName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    GuestPhone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CompletedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    VoidedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SyncedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalOrders", x => x.Id);
                    table.CheckConstraint("CK_LocalOrder_ChangeAmount", "ChangeAmount >= 0");
                    table.CheckConstraint("CK_LocalOrder_EmployeeId", "EmployeeId > 0");
                    table.CheckConstraint("CK_LocalOrder_LocationId", "LocationId > 0");
                    table.CheckConstraint("CK_LocalOrder_PaidAmount", "PaidAmount >= 0");
                    table.CheckConstraint("CK_LocalOrder_SubtotalAmount", "SubtotalAmount >= 0");
                    table.CheckConstraint("CK_LocalOrder_TenantId", "TenantId > 0");
                    table.CheckConstraint("CK_LocalOrder_TerminalId", "TerminalId > 0");
                    table.CheckConstraint("CK_LocalOrder_TotalAmount", "TotalAmount >= 0");
                });

            migrationBuilder.CreateTable(
                name: "LocalPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TerminalId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenderMethodId = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalPaymentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TerminalSequence = table.Column<long>(type: "INTEGER", nullable: false),
                    PaymentType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    AuthorizedAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    CapturedAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    PaymentToken = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ExternalPaymentReference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AuthorizationCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CardBrand = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CardLast4 = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    FailureCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RequiresReconciliation = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReconciledOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ProcessedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SyncedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalPayments", x => x.Id);
                    table.CheckConstraint("CK_LocalPayment_Amount", "Amount > 0");
                    table.CheckConstraint("CK_LocalPayment_LocationId", "LocationId > 0");
                    table.CheckConstraint("CK_LocalPayment_TenantId", "TenantId > 0");
                    table.CheckConstraint("CK_LocalPayment_TenderMethodId", "TenderMethodId > 0");
                    table.CheckConstraint("CK_LocalPayment_TerminalId", "TerminalId > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalOrderLines_OrderId",
                table: "LocalOrderLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "UX_LocalOrderLines_Tenant_OrderId_LineNumber",
                table: "LocalOrderLines",
                columns: new[] { "TenantId", "OrderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalOrders_Tenant_Terminal_Status",
                table: "LocalOrders",
                columns: new[] { "TenantId", "TerminalId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_LocalOrders_Tenant_ReceiptNumber",
                table: "LocalOrders",
                columns: new[] { "TenantId", "ReceiptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LocalOrders_Tenant_Terminal_Sequence",
                table: "LocalOrders",
                columns: new[] { "TenantId", "TerminalId", "TerminalSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalPayments_OrderId",
                table: "LocalPayments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalPayments_Tenant_OrderId_TenderMethod",
                table: "LocalPayments",
                columns: new[] { "TenantId", "OrderId", "TenderMethodId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalOrderLines");

            migrationBuilder.DropTable(
                name: "LocalOrders");

            migrationBuilder.DropTable(
                name: "LocalPayments");
        }
    }
}
