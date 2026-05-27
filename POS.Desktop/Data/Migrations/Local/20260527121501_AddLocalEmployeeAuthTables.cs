using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Desktop.Data.Migrations.Local
{
    /// <inheritdoc />
    public partial class AddLocalEmployeeAuthTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalEmployeeLocationRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: true),
                    Role = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    PermissionSetCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StartsOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndsOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalEmployeeLocationRoles", x => x.Id);
                    table.CheckConstraint("CK_LocalEmployeeLocationRole_EmployeeId", "EmployeeId > 0");
                    table.CheckConstraint("CK_LocalEmployeeLocationRole_Role_NotEmpty", "Role <> ''");
                    table.CheckConstraint("CK_LocalEmployeeLocationRole_TenantId", "TenantId > 0");
                });

            migrationBuilder.CreateTable(
                name: "LocalEmployees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    PinHash = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    PinSalt = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PinHashAlgorithm = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    MustChangePin = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalEmployees", x => x.Id);
                    table.CheckConstraint("CK_LocalEmployee_DisplayName_NotEmpty", "DisplayName <> ''");
                    table.CheckConstraint("CK_LocalEmployee_EmployeeNumber_NotEmpty", "EmployeeNumber <> ''");
                    table.CheckConstraint("CK_LocalEmployee_TenantId", "TenantId > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalEmployeeLocationRoles_Tenant_Location_Role",
                table: "LocalEmployeeLocationRoles",
                columns: new[] { "TenantId", "LocationId", "Role" });

            migrationBuilder.CreateIndex(
                name: "UX_LocalEmployeeLocationRoles_Scoped",
                table: "LocalEmployeeLocationRoles",
                columns: new[] { "TenantId", "EmployeeId", "LocationId", "Role" },
                unique: true,
                filter: "LocationId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_LocalEmployeeLocationRoles_TenantWide",
                table: "LocalEmployeeLocationRoles",
                columns: new[] { "TenantId", "EmployeeId", "Role" },
                unique: true,
                filter: "LocationId IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalEmployees_Tenant_Status",
                table: "LocalEmployees",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_LocalEmployees_Tenant_EmployeeNumber",
                table: "LocalEmployees",
                columns: new[] { "TenantId", "EmployeeNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalEmployeeLocationRoles");

            migrationBuilder.DropTable(
                name: "LocalEmployees");
        }
    }
}
