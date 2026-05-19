using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Api.Data.Migrations.Central
{
    /// <inheritdoc />
    public partial class Central_RemoveAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [dbo].[AuditLogs];
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AuthorizedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    NewValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OldValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasonCodeId = table.Column<int>(type: "int", nullable: true),
                    ReceivedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.UniqueConstraint("AK_AuditLogs_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AuditLogs_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Employees_TenantId_AuthorizedByEmployeeId",
                        columns: x => new { x.TenantId, x.AuthorizedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Employees_TenantId_EmployeeId",
                        columns: x => new { x.TenantId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditLogs_ReasonCodes_TenantId_ReasonCodeId",
                        columns: x => new { x.TenantId, x.ReasonCodeId },
                        principalTable: "ReasonCodes",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Terminals_TenantId_TerminalId",
                        columns: x => new { x.TenantId, x.TerminalId },
                        principalTable: "Terminals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Tenant_Action_Date",
                table: "AuditLogs",
                columns: new[] { "TenantId", "ActionType", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Tenant_CorrelationId",
                table: "AuditLogs",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Tenant_Entity",
                table: "AuditLogs",
                columns: new[] { "TenantId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_AuthorizedByEmployeeId",
                table: "AuditLogs",
                columns: new[] { "TenantId", "AuthorizedByEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_EmployeeId",
                table: "AuditLogs",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_LocationId",
                table: "AuditLogs",
                columns: new[] { "TenantId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_ReasonCodeId",
                table: "AuditLogs",
                columns: new[] { "TenantId", "ReasonCodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_TerminalId",
                table: "AuditLogs",
                columns: new[] { "TenantId", "TerminalId" });

            migrationBuilder.CreateIndex(
                name: "UX_AuditLogs_Tenant_IdempotencyKey",
                table: "AuditLogs",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);
        }
    }
}
