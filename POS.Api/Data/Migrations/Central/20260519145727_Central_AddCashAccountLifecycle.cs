using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Api.Data.Migrations.Central
{
    /// <inheritdoc />
    public partial class Central_AddCashAccountLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AccountType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AccountNumberMasked = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashAccounts", x => x.Id);
                    table.UniqueConstraint("AK_CashAccounts_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CashAccounts_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashAccounts_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashAccountMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: true),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TerminalSequence = table.Column<long>(type: "bigint", nullable: true),
                    MovementType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    SourceCashAccountId = table.Column<int>(type: "int", nullable: true),
                    DestinationCashAccountId = table.Column<int>(type: "int", nullable: true),
                    PerformedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    AuthorizedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    VerifiedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    VerifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReasonCodeId = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashAccountMovements", x => x.Id);
                    table.UniqueConstraint("AK_CashAccountMovements_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CashAccountMovements_CashAccounts_TenantId_DestinationCashAccountId",
                        columns: x => new { x.TenantId, x.DestinationCashAccountId },
                        principalTable: "CashAccounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashAccountMovements_CashAccounts_TenantId_SourceCashAccountId",
                        columns: x => new { x.TenantId, x.SourceCashAccountId },
                        principalTable: "CashAccounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashAccountMovements_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashAccountMovements_Employees_TenantId_AuthorizedByEmployeeId",
                        columns: x => new { x.TenantId, x.AuthorizedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashAccountMovements_Employees_TenantId_PerformedByEmployeeId",
                        columns: x => new { x.TenantId, x.PerformedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashAccountMovements_Employees_TenantId_VerifiedByEmployeeId",
                        columns: x => new { x.TenantId, x.VerifiedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashAccountMovements_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashAccountMovements_ReasonCodes_TenantId_ReasonCodeId",
                        columns: x => new { x.TenantId, x.ReasonCodeId },
                        principalTable: "ReasonCodes",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashAccountMovements_Shifts_TenantId_ShiftId",
                        columns: x => new { x.TenantId, x.ShiftId },
                        principalTable: "Shifts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashAccountMovements_Terminals_TenantId_TerminalId",
                        columns: x => new { x.TenantId, x.TerminalId },
                        principalTable: "Terminals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountMovements_Tenant_AuthorizedBy_Date",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "AuthorizedByEmployeeId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountMovements_Tenant_BusinessDate",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountMovements_Tenant_CorrelationId",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountMovements_Tenant_DestinationAccount",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "DestinationCashAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountMovements_Tenant_ReasonCode",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "ReasonCodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountMovements_Tenant_SourceAccount",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "SourceCashAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountMovements_Tenant_Status",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountMovements_Tenant_Type_Date",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "MovementType", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountMovements_Tenant_VerifiedBy_Date",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "VerifiedByEmployeeId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountMovements_TenantId_LocationId",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountMovements_TenantId_PerformedByEmployeeId",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "PerformedByEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccountMovements_TenantId_ShiftId",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "ShiftId" });

            migrationBuilder.CreateIndex(
                name: "UX_CashAccountMovements_Tenant_IdempotencyKey",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CashAccountMovements_Tenant_Terminal_Sequence",
                table: "CashAccountMovements",
                columns: new[] { "TenantId", "TerminalId", "TerminalSequence" },
                unique: true,
                filter: "[TerminalId] IS NOT NULL AND [TerminalSequence] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashAccounts_Tenant_Active_Type",
                table: "CashAccounts",
                columns: new[] { "TenantId", "IsActive", "AccountType" });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccounts_Tenant_Location_Type",
                table: "CashAccounts",
                columns: new[] { "TenantId", "LocationId", "AccountType" });

            migrationBuilder.CreateIndex(
                name: "UX_CashAccounts_Tenant_Code",
                table: "CashAccounts",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashAccountMovements");

            migrationBuilder.DropTable(
                name: "CashAccounts");
        }
    }
}
