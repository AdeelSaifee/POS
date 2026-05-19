using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Api.Data.Migrations.Central
{
    /// <inheritdoc />
    public partial class Central_CompletePosOperationalSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SizeText",
                table: "ItemVariants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeightUnitOfMeasureId",
                table: "ItemVariants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightValue",
                table: "ItemVariants",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandName",
                table: "Items",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManufacturerName",
                table: "Items",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ManagerActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PerformedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    AuthorizedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ReasonCodeId = table.Column<int>(type: "int", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TerminalSequence = table.Column<long>(type: "bigint", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
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
                    table.PrimaryKey("PK_ManagerActions", x => x.Id);
                    table.UniqueConstraint("AK_ManagerActions_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ManagerActions_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerActions_Employees_TenantId_AuthorizedByEmployeeId",
                        columns: x => new { x.TenantId, x.AuthorizedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerActions_Employees_TenantId_PerformedByEmployeeId",
                        columns: x => new { x.TenantId, x.PerformedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerActions_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerActions_OrderLines_TenantId_OrderLineId",
                        columns: x => new { x.TenantId, x.OrderLineId },
                        principalTable: "OrderLines",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerActions_Orders_TenantId_OrderId",
                        columns: x => new { x.TenantId, x.OrderId },
                        principalTable: "Orders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerActions_ReasonCodes_TenantId_ReasonCodeId",
                        columns: x => new { x.TenantId, x.ReasonCodeId },
                        principalTable: "ReasonCodes",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerActions_Shifts_TenantId_ShiftId",
                        columns: x => new { x.TenantId, x.ShiftId },
                        principalTable: "Shifts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerActions_Terminals_TenantId_TerminalId",
                        columns: x => new { x.TenantId, x.TerminalId },
                        principalTable: "Terminals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TerminalSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TerminalSequence = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LoggedInOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LoggedOutOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerminalSessions", x => x.Id);
                    table.UniqueConstraint("AK_TerminalSessions_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_TerminalSessions_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TerminalSessions_Employees_TenantId_EmployeeId",
                        columns: x => new { x.TenantId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TerminalSessions_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TerminalSessions_Shifts_TenantId_ShiftId",
                        columns: x => new { x.TenantId, x.ShiftId },
                        principalTable: "Shifts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TerminalSessions_Terminals_TenantId_TerminalId",
                        columns: x => new { x.TenantId, x.TerminalId },
                        principalTable: "Terminals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemVariants_Tenant_WeightUnit",
                table: "ItemVariants",
                columns: new[] { "TenantId", "WeightUnitOfMeasureId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerActions_Tenant_Action_Date",
                table: "ManagerActions",
                columns: new[] { "TenantId", "ActionType", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerActions_Tenant_AuthorizedBy_Date",
                table: "ManagerActions",
                columns: new[] { "TenantId", "AuthorizedByEmployeeId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerActions_Tenant_CorrelationId",
                table: "ManagerActions",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerActions_Tenant_Order",
                table: "ManagerActions",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerActions_Tenant_OrderLine",
                table: "ManagerActions",
                columns: new[] { "TenantId", "OrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerActions_Tenant_ReasonCode",
                table: "ManagerActions",
                columns: new[] { "TenantId", "ReasonCodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerActions_TenantId_LocationId",
                table: "ManagerActions",
                columns: new[] { "TenantId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerActions_TenantId_PerformedByEmployeeId",
                table: "ManagerActions",
                columns: new[] { "TenantId", "PerformedByEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerActions_TenantId_ShiftId",
                table: "ManagerActions",
                columns: new[] { "TenantId", "ShiftId" });

            migrationBuilder.CreateIndex(
                name: "UX_ManagerActions_Tenant_IdempotencyKey",
                table: "ManagerActions",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ManagerActions_Tenant_Terminal_Sequence",
                table: "ManagerActions",
                columns: new[] { "TenantId", "TerminalId", "TerminalSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TerminalSessions_Tenant_Employee_Date",
                table: "TerminalSessions",
                columns: new[] { "TenantId", "EmployeeId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TerminalSessions_Tenant_Location_Date",
                table: "TerminalSessions",
                columns: new[] { "TenantId", "LocationId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TerminalSessions_Tenant_Shift",
                table: "TerminalSessions",
                columns: new[] { "TenantId", "ShiftId" });

            migrationBuilder.CreateIndex(
                name: "IX_TerminalSessions_Tenant_Status",
                table: "TerminalSessions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_TerminalSessions_Tenant_Terminal_Sequence",
                table: "TerminalSessions",
                columns: new[] { "TenantId", "TerminalId", "TerminalSequence" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemVariants_UnitsOfMeasure_TenantId_WeightUnitOfMeasureId",
                table: "ItemVariants",
                columns: new[] { "TenantId", "WeightUnitOfMeasureId" },
                principalTable: "UnitsOfMeasure",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemVariants_UnitsOfMeasure_TenantId_WeightUnitOfMeasureId",
                table: "ItemVariants");

            migrationBuilder.DropTable(
                name: "ManagerActions");

            migrationBuilder.DropTable(
                name: "TerminalSessions");

            migrationBuilder.DropIndex(
                name: "IX_ItemVariants_Tenant_WeightUnit",
                table: "ItemVariants");

            migrationBuilder.DropColumn(
                name: "SizeText",
                table: "ItemVariants");

            migrationBuilder.DropColumn(
                name: "WeightUnitOfMeasureId",
                table: "ItemVariants");

            migrationBuilder.DropColumn(
                name: "WeightValue",
                table: "ItemVariants");

            migrationBuilder.DropColumn(
                name: "BrandName",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ManufacturerName",
                table: "Items");
        }
    }
}
