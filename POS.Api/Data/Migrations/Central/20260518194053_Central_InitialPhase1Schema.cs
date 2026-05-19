using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Api.Data.Migrations.Central
{
    /// <inheritdoc />
    public partial class Central_InitialPhase1Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TaxRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DefaultCurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OwnershipType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsCore = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.UniqueConstraint("AK_Categories_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Categories_Categories_TenantId_ParentCategoryId",
                        columns: x => new { x.TenantId, x.ParentCategoryId },
                        principalTable: "Categories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Categories_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    TaxRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PrivacyStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.UniqueConstraint("AK_Customers_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Customers_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PinHash = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PinSalt = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PinHashAlgorithm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MustChangePin = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.UniqueConstraint("AK_Employees_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Employees_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PriceListType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    PriceListVersion = table.Column<long>(type: "bigint", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceLists", x => x.Id);
                    table.UniqueConstraint("AK_PriceLists_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_PriceLists_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReasonCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ReasonCategory = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    RequiresManagerApproval = table.Column<bool>(type: "bit", nullable: false),
                    RequiresComment = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReasonCodes", x => x.Id);
                    table.UniqueConstraint("AK_ReasonCodes_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ReasonCodes_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TemplateVersion = table.Column<long>(type: "bigint", nullable: false),
                    ContentFormat = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TemplateContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptTemplates", x => x.Id);
                    table.UniqueConstraint("AK_ReceiptTemplates_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ReceiptTemplates_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    CalculationMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    JurisdictionCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RuleVersion = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRules", x => x.Id);
                    table.UniqueConstraint("AK_TaxRules_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_TaxRules_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenderMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TenderType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequiresExternalReference = table.Column<bool>(type: "bit", nullable: false),
                    AllowsChange = table.Column<bool>(type: "bit", nullable: false),
                    AllowsRefund = table.Column<bool>(type: "bit", nullable: false),
                    RequiresOnlineAuthorization = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderMethods", x => x.Id);
                    table.UniqueConstraint("AK_TenderMethods_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_TenderMethods_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitsOfMeasure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MeasurementType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    BaseUnitId = table.Column<int>(type: "int", nullable: true),
                    ConversionFactorToBase = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    AllowsFractionalQuantity = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitsOfMeasure", x => x.Id);
                    table.UniqueConstraint("AK_UnitsOfMeasure_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_UnitsOfMeasure_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitsOfMeasure_UnitsOfMeasure_TenantId_BaseUnitId",
                        columns: x => new { x.TenantId, x.BaseUnitId },
                        principalTable: "UnitsOfMeasure",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EnabledOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DisabledOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantModules", x => x.Id);
                    table.UniqueConstraint("AK_TenantModules_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_TenantModules_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantModules_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LocationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CountryCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefaultPriceListId = table.Column<int>(type: "int", nullable: true),
                    DefaultReceiptTemplateId = table.Column<int>(type: "int", nullable: true),
                    BusinessDayStartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    AllowsNegativeStock = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.UniqueConstraint("AK_Locations_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Locations_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Locations_PriceLists_TenantId_DefaultPriceListId",
                        columns: x => new { x.TenantId, x.DefaultPriceListId },
                        principalTable: "PriceLists",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Locations_ReceiptTemplates_TenantId_DefaultReceiptTemplateId",
                        columns: x => new { x.TenantId, x.DefaultReceiptTemplateId },
                        principalTable: "ReceiptTemplates",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ItemType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsTrackedInventory = table.Column<bool>(type: "bit", nullable: false),
                    DefaultUnitOfMeasureId = table.Column<int>(type: "int", nullable: false),
                    DefaultTaxRuleId = table.Column<int>(type: "int", nullable: true),
                    CatalogVersion = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.UniqueConstraint("AK_Items_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Items_Categories_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalTable: "Categories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Items_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Items_TaxRules_TenantId_DefaultTaxRuleId",
                        columns: x => new { x.TenantId, x.DefaultTaxRuleId },
                        principalTable: "TaxRules",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Items_UnitsOfMeasure_TenantId_DefaultUnitOfMeasureId",
                        columns: x => new { x.TenantId, x.DefaultUnitOfMeasureId },
                        principalTable: "UnitsOfMeasure",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLocationRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PermissionSetCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartsOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndsOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLocationRoles", x => x.Id);
                    table.UniqueConstraint("AK_EmployeeLocationRoles_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_EmployeeLocationRoles_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLocationRoles_Employees_TenantId_EmployeeId",
                        columns: x => new { x.TenantId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLocationRoles_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Terminals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TerminalCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceSecretHash = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ProvisioningStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LastSeenOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastCatalogVersion = table.Column<long>(type: "bigint", nullable: true),
                    LastPriceListId = table.Column<int>(type: "int", nullable: true),
                    LastRuleVersion = table.Column<long>(type: "bigint", nullable: true),
                    LastReceiptTemplateId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terminals", x => x.Id);
                    table.UniqueConstraint("AK_Terminals_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Terminals_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Terminals_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Terminals_PriceLists_TenantId_LastPriceListId",
                        columns: x => new { x.TenantId, x.LastPriceListId },
                        principalTable: "PriceLists",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Terminals_ReceiptTemplates_TenantId_LastReceiptTemplateId",
                        columns: x => new { x.TenantId, x.LastReceiptTemplateId },
                        principalTable: "ReceiptTemplates",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UiLayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    LayoutCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ScreenKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LayoutVersion = table.Column<long>(type: "bigint", nullable: false),
                    LayoutJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UiLayouts", x => x.Id);
                    table.UniqueConstraint("AK_UiLayouts_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_UiLayouts_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UiLayouts_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    VariantCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitOfMeasureId = table.Column<int>(type: "int", nullable: false),
                    TaxRuleId = table.Column<int>(type: "int", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsSellable = table.Column<bool>(type: "bit", nullable: false),
                    IsPurchasable = table.Column<bool>(type: "bit", nullable: false),
                    CatalogVersion = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemVariants", x => x.Id);
                    table.UniqueConstraint("AK_ItemVariants_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ItemVariants_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemVariants_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemVariants_TaxRules_TenantId_TaxRuleId",
                        columns: x => new { x.TenantId, x.TaxRuleId },
                        principalTable: "TaxRules",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemVariants_UnitsOfMeasure_TenantId_UnitOfMeasureId",
                        columns: x => new { x.TenantId, x.UnitOfMeasureId },
                        principalTable: "UnitsOfMeasure",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    TerminalId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    AuthorizedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ReasonCodeId = table.Column<int>(type: "int", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OccurredOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceivedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: false),
                    OpenedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ClosedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TerminalSequence = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OpeningCashAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ExpectedCashAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CountedCashAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    VarianceAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    OpenedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SyncedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.UniqueConstraint("AK_Shifts_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Shifts_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shifts_Employees_TenantId_ClosedByEmployeeId",
                        columns: x => new { x.TenantId, x.ClosedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shifts_Employees_TenantId_OpenedByEmployeeId",
                        columns: x => new { x.TenantId, x.OpenedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shifts_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shifts_Terminals_TenantId_TerminalId",
                        columns: x => new { x.TenantId, x.TerminalId },
                        principalTable: "Terminals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncIngestAcks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: false),
                    ChunkSequence = table.Column<long>(type: "bigint", nullable: false),
                    ChunkIdempotencyKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventCount = table.Column<int>(type: "int", nullable: false),
                    FirstBusinessDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastBusinessDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AckPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceivedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncIngestAcks", x => x.Id);
                    table.UniqueConstraint("AK_SyncIngestAcks_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_SyncIngestAcks_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncIngestAcks_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncIngestAcks_Terminals_TenantId_TerminalId",
                        columns: x => new { x.TenantId, x.TerminalId },
                        principalTable: "Terminals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemIdentifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    ItemVariantId = table.Column<int>(type: "int", nullable: true),
                    IdentifierType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IdentifierValue = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    StartsOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndsOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemIdentifiers", x => x.Id);
                    table.UniqueConstraint("AK_ItemIdentifiers_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ItemIdentifiers_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemIdentifiers_ItemVariants_TenantId_ItemVariantId",
                        columns: x => new { x.TenantId, x.ItemVariantId },
                        principalTable: "ItemVariants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemIdentifiers_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceListId = table.Column<int>(type: "int", nullable: false),
                    ItemVariantId = table.Column<int>(type: "int", nullable: false),
                    UnitOfMeasureId = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CompareAtPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MinimumQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsTaxIncluded = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemPrices", x => x.Id);
                    table.UniqueConstraint("AK_ItemPrices_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ItemPrices_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemPrices_ItemVariants_TenantId_ItemVariantId",
                        columns: x => new { x.TenantId, x.ItemVariantId },
                        principalTable: "ItemVariants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemPrices_PriceLists_TenantId_PriceListId",
                        columns: x => new { x.TenantId, x.PriceListId },
                        principalTable: "PriceLists",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemPrices_UnitsOfMeasure_TenantId_UnitOfMeasureId",
                        columns: x => new { x.TenantId, x.UnitOfMeasureId },
                        principalTable: "UnitsOfMeasure",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    ItemVariantId = table.Column<int>(type: "int", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    LastMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastMovementOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StockStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemStocks", x => x.Id);
                    table.UniqueConstraint("AK_ItemStocks_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ItemStocks_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemStocks_ItemVariants_TenantId_ItemVariantId",
                        columns: x => new { x.TenantId, x.ItemVariantId },
                        principalTable: "ItemVariants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemStocks_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashDrawerMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AuthorizedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ReasonCodeId = table.Column<int>(type: "int", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TerminalSequence = table.Column<long>(type: "bigint", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SyncedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_CashDrawerMovements", x => x.Id);
                    table.UniqueConstraint("AK_CashDrawerMovements_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_CashDrawerMovements_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashDrawerMovements_Employees_TenantId_AuthorizedByEmployeeId",
                        columns: x => new { x.TenantId, x.AuthorizedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashDrawerMovements_Employees_TenantId_EmployeeId",
                        columns: x => new { x.TenantId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashDrawerMovements_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashDrawerMovements_ReasonCodes_TenantId_ReasonCodeId",
                        columns: x => new { x.TenantId, x.ReasonCodeId },
                        principalTable: "ReasonCodes",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashDrawerMovements_Shifts_TenantId_ShiftId",
                        columns: x => new { x.TenantId, x.ShiftId },
                        principalTable: "Shifts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashDrawerMovements_Terminals_TenantId_TerminalId",
                        columns: x => new { x.TenantId, x.TerminalId },
                        principalTable: "Terminals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginalOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TerminalSequence = table.Column<long>(type: "bigint", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FulfillmentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CatalogVersion = table.Column<long>(type: "bigint", nullable: false),
                    PriceListId = table.Column<int>(type: "int", nullable: true),
                    RuleVersion = table.Column<long>(type: "bigint", nullable: false),
                    ReceiptTemplateId = table.Column<int>(type: "int", nullable: true),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GuestPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CompletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VoidedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SyncedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.UniqueConstraint("AK_Orders_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Orders_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalTable: "Customers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Employees_TenantId_EmployeeId",
                        columns: x => new { x.TenantId, x.EmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Orders_TenantId_OriginalOrderId",
                        columns: x => new { x.TenantId, x.OriginalOrderId },
                        principalTable: "Orders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_PriceLists_TenantId_PriceListId",
                        columns: x => new { x.TenantId, x.PriceListId },
                        principalTable: "PriceLists",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_ReceiptTemplates_TenantId_ReceiptTemplateId",
                        columns: x => new { x.TenantId, x.ReceiptTemplateId },
                        principalTable: "ReceiptTemplates",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Shifts_TenantId_ShiftId",
                        columns: x => new { x.TenantId, x.ShiftId },
                        principalTable: "Shifts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Terminals_TenantId_TerminalId",
                        columns: x => new { x.TenantId, x.TerminalId },
                        principalTable: "Terminals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ZReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: true),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GeneratedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReportNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GrossSalesAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetSalesAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CashExpectedAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CashCountedAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CashVarianceAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ReportPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SyncedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_ZReports", x => x.Id);
                    table.UniqueConstraint("AK_ZReports_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ZReports_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZReports_Employees_TenantId_GeneratedByEmployeeId",
                        columns: x => new { x.TenantId, x.GeneratedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZReports_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZReports_Shifts_TenantId_ShiftId",
                        columns: x => new { x.TenantId, x.ShiftId },
                        principalTable: "Shifts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZReports_Terminals_TenantId_TerminalId",
                        columns: x => new { x.TenantId, x.TerminalId },
                        principalTable: "Terminals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    ItemVariantId = table.Column<int>(type: "int", nullable: true),
                    OriginalOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReasonCodeId = table.Column<int>(type: "int", nullable: true),
                    AuthorizedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    LineType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UnitOfMeasureCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxRuleId = table.Column<int>(type: "int", nullable: true),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    PriceListId = table.Column<int>(type: "int", nullable: true),
                    CatalogVersion = table.Column<long>(type: "bigint", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_OrderLines", x => x.Id);
                    table.UniqueConstraint("AK_OrderLines_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_OrderLines_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_Employees_TenantId_AuthorizedByEmployeeId",
                        columns: x => new { x.TenantId, x.AuthorizedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_ItemVariants_TenantId_ItemVariantId",
                        columns: x => new { x.TenantId, x.ItemVariantId },
                        principalTable: "ItemVariants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_OrderLines_TenantId_OriginalOrderLineId",
                        columns: x => new { x.TenantId, x.OriginalOrderLineId },
                        principalTable: "OrderLines",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_Orders_TenantId_OrderId",
                        columns: x => new { x.TenantId, x.OrderId },
                        principalTable: "Orders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_PriceLists_TenantId_PriceListId",
                        columns: x => new { x.TenantId, x.PriceListId },
                        principalTable: "PriceLists",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_ReasonCodes_TenantId_ReasonCodeId",
                        columns: x => new { x.TenantId, x.ReasonCodeId },
                        principalTable: "ReasonCodes",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_TaxRules_TenantId_TaxRuleId",
                        columns: x => new { x.TenantId, x.TaxRuleId },
                        principalTable: "TaxRules",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_Terminals_TenantId_TerminalId",
                        columns: x => new { x.TenantId, x.TerminalId },
                        principalTable: "Terminals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenderMethodId = table.Column<int>(type: "int", nullable: false),
                    OriginalPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TerminalSequence = table.Column<long>(type: "bigint", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    AuthorizedAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CapturedAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PaymentToken = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ExternalPaymentReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AuthorizationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CardBrand = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CardLast4 = table.Column<string>(type: "nchar(4)", fixedLength: true, maxLength: 4, nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresReconciliation = table.Column<bool>(type: "bit", nullable: false),
                    ReconciledOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProcessedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SyncedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.UniqueConstraint("AK_Payments_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Payments_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Orders_TenantId_OrderId",
                        columns: x => new { x.TenantId, x.OrderId },
                        principalTable: "Orders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Payments_TenantId_OriginalPaymentId",
                        columns: x => new { x.TenantId, x.OriginalPaymentId },
                        principalTable: "Payments",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Shifts_TenantId_ShiftId",
                        columns: x => new { x.TenantId, x.ShiftId },
                        principalTable: "Shifts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_TenderMethods_TenantId_TenderMethodId",
                        columns: x => new { x.TenantId, x.TenderMethodId },
                        principalTable: "TenderMethods",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Terminals_TenantId_TerminalId",
                        columns: x => new { x.TenantId, x.TerminalId },
                        principalTable: "Terminals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TerminalId = table.Column<int>(type: "int", nullable: true),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    ItemVariantId = table.Column<int>(type: "int", nullable: false),
                    SourceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReasonCodeId = table.Column<int>(type: "int", nullable: true),
                    AuthorizedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TerminalSequence = table.Column<long>(type: "bigint", nullable: true),
                    MovementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasureId = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    StockBefore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    StockAfter = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ExceptionStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExceptionDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AppliedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SyncedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_InventoryMovements", x => x.Id);
                    table.UniqueConstraint("AK_InventoryMovements_Tenant_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Companies_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Employees_TenantId_AuthorizedByEmployeeId",
                        columns: x => new { x.TenantId, x.AuthorizedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_ItemVariants_TenantId_ItemVariantId",
                        columns: x => new { x.TenantId, x.ItemVariantId },
                        principalTable: "ItemVariants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Locations_TenantId_LocationId",
                        columns: x => new { x.TenantId, x.LocationId },
                        principalTable: "Locations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_OrderLines_TenantId_SourceOrderLineId",
                        columns: x => new { x.TenantId, x.SourceOrderLineId },
                        principalTable: "OrderLines",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Orders_TenantId_SourceOrderId",
                        columns: x => new { x.TenantId, x.SourceOrderId },
                        principalTable: "Orders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_ReasonCodes_TenantId_ReasonCodeId",
                        columns: x => new { x.TenantId, x.ReasonCodeId },
                        principalTable: "ReasonCodes",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Shifts_TenantId_ShiftId",
                        columns: x => new { x.TenantId, x.ShiftId },
                        principalTable: "Shifts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Terminals_TenantId_TerminalId",
                        columns: x => new { x.TenantId, x.TerminalId },
                        principalTable: "Terminals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_UnitsOfMeasure_TenantId_UnitOfMeasureId",
                        columns: x => new { x.TenantId, x.UnitOfMeasureId },
                        principalTable: "UnitsOfMeasure",
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

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerMovements_Tenant_CorrelationId",
                table: "CashDrawerMovements",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerMovements_Tenant_Date",
                table: "CashDrawerMovements",
                columns: new[] { "TenantId", "LocationId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerMovements_Tenant_Shift",
                table: "CashDrawerMovements",
                columns: new[] { "TenantId", "ShiftId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerMovements_TenantId_AuthorizedByEmployeeId",
                table: "CashDrawerMovements",
                columns: new[] { "TenantId", "AuthorizedByEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerMovements_TenantId_EmployeeId",
                table: "CashDrawerMovements",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerMovements_TenantId_ReasonCodeId",
                table: "CashDrawerMovements",
                columns: new[] { "TenantId", "ReasonCodeId" });

            migrationBuilder.CreateIndex(
                name: "UX_CashDrawerMovements_Tenant_IdempotencyKey",
                table: "CashDrawerMovements",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CashDrawerMovements_Tenant_Terminal_Sequence",
                table: "CashDrawerMovements",
                columns: new[] { "TenantId", "TerminalId", "TerminalSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Tenant_Parent",
                table: "Categories",
                columns: new[] { "TenantId", "ParentCategoryId" });

            migrationBuilder.CreateIndex(
                name: "UX_Categories_Tenant_Code",
                table: "Categories",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Status",
                table: "Companies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_Companies_Code",
                table: "Companies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Tenant_DisplayName",
                table: "Customers",
                columns: new[] { "TenantId", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Tenant_NormalizedPhone",
                table: "Customers",
                columns: new[] { "TenantId", "NormalizedPhone" },
                filter: "[NormalizedPhone] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Customers_Tenant_CustomerNumber",
                table: "Customers",
                columns: new[] { "TenantId", "CustomerNumber" },
                unique: true,
                filter: "[CustomerNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Customers_Tenant_IdempotencyKey",
                table: "Customers",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLocationRoles_Tenant_Location_Role",
                table: "EmployeeLocationRoles",
                columns: new[] { "TenantId", "LocationId", "Role" });

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeLocationRoles_Scoped",
                table: "EmployeeLocationRoles",
                columns: new[] { "TenantId", "EmployeeId", "LocationId", "Role" },
                unique: true,
                filter: "[LocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeLocationRoles_TenantWide",
                table: "EmployeeLocationRoles",
                columns: new[] { "TenantId", "EmployeeId", "Role" },
                unique: true,
                filter: "[LocationId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Tenant_Status",
                table: "Employees",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_Employees_Tenant_EmployeeNumber",
                table: "Employees",
                columns: new[] { "TenantId", "EmployeeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Employees_Tenant_UserName",
                table: "Employees",
                columns: new[] { "TenantId", "UserName" },
                unique: true,
                filter: "[UserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_Tenant_CorrelationId",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_Tenant_ExceptionStatus",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "ExceptionStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_Tenant_Item_Location_Date",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "ItemVariantId", "LocationId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_Tenant_SourceOrderLine",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "SourceOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_AuthorizedByEmployeeId",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "AuthorizedByEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_ItemId",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_LocationId",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_ReasonCodeId",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "ReasonCodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_ShiftId",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "ShiftId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_SourceOrderId",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "SourceOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_TerminalId",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "TerminalId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_UnitOfMeasureId",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "UnitOfMeasureId" });

            migrationBuilder.CreateIndex(
                name: "UX_InventoryMovements_Tenant_IdempotencyKey",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemIdentifiers_Tenant_Item",
                table: "ItemIdentifiers",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemIdentifiers_Tenant_Variant",
                table: "ItemIdentifiers",
                columns: new[] { "TenantId", "ItemVariantId" });

            migrationBuilder.CreateIndex(
                name: "UX_ItemIdentifiers_Active_Type_Value",
                table: "ItemIdentifiers",
                columns: new[] { "TenantId", "IdentifierType", "IdentifierValue" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ItemPrices_Tenant_List_Effective",
                table: "ItemPrices",
                columns: new[] { "TenantId", "PriceListId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemPrices_Tenant_Variant",
                table: "ItemPrices",
                columns: new[] { "TenantId", "ItemVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemPrices_TenantId_UnitOfMeasureId",
                table: "ItemPrices",
                columns: new[] { "TenantId", "UnitOfMeasureId" });

            migrationBuilder.CreateIndex(
                name: "UX_ItemPrices_Tenant_List_Variant_Qty_From",
                table: "ItemPrices",
                columns: new[] { "TenantId", "PriceListId", "ItemVariantId", "MinimumQuantity", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Tenant_Category",
                table: "Items",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Tenant_Name",
                table: "Items",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Tenant_Status",
                table: "Items",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_TenantId_DefaultTaxRuleId",
                table: "Items",
                columns: new[] { "TenantId", "DefaultTaxRuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_TenantId_DefaultUnitOfMeasureId",
                table: "Items",
                columns: new[] { "TenantId", "DefaultUnitOfMeasureId" });

            migrationBuilder.CreateIndex(
                name: "UX_Items_Tenant_ItemCode",
                table: "Items",
                columns: new[] { "TenantId", "ItemCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemStocks_Tenant_Status",
                table: "ItemStocks",
                columns: new[] { "TenantId", "StockStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemStocks_Tenant_Variant",
                table: "ItemStocks",
                columns: new[] { "TenantId", "ItemVariantId" });

            migrationBuilder.CreateIndex(
                name: "UX_ItemStocks_Tenant_Location_Variant",
                table: "ItemStocks",
                columns: new[] { "TenantId", "LocationId", "ItemVariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemVariants_Tenant_Item",
                table: "ItemVariants",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemVariants_Tenant_Status",
                table: "ItemVariants",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemVariants_TenantId_TaxRuleId",
                table: "ItemVariants",
                columns: new[] { "TenantId", "TaxRuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemVariants_TenantId_UnitOfMeasureId",
                table: "ItemVariants",
                columns: new[] { "TenantId", "UnitOfMeasureId" });

            migrationBuilder.CreateIndex(
                name: "UX_ItemVariants_Tenant_Item_VariantCode",
                table: "ItemVariants",
                columns: new[] { "TenantId", "ItemId", "VariantCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ItemVariants_Tenant_SKU",
                table: "ItemVariants",
                columns: new[] { "TenantId", "SKU" },
                unique: true,
                filter: "[SKU] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Tenant_Type",
                table: "Locations",
                columns: new[] { "TenantId", "LocationType" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_DefaultPriceListId",
                table: "Locations",
                columns: new[] { "TenantId", "DefaultPriceListId" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_DefaultReceiptTemplateId",
                table: "Locations",
                columns: new[] { "TenantId", "DefaultReceiptTemplateId" });

            migrationBuilder.CreateIndex(
                name: "UX_Locations_Tenant_Code",
                table: "Locations",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_Category",
                table: "Modules",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "UX_Modules_ModuleKey",
                table: "Modules",
                column: "ModuleKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_Tenant_CorrelationId",
                table: "OrderLines",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_Tenant_ItemVariant",
                table: "OrderLines",
                columns: new[] { "TenantId", "ItemVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_Tenant_Order",
                table: "OrderLines",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_Tenant_OriginalLine",
                table: "OrderLines",
                columns: new[] { "TenantId", "OriginalOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_TenantId_AuthorizedByEmployeeId",
                table: "OrderLines",
                columns: new[] { "TenantId", "AuthorizedByEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_TenantId_ItemId",
                table: "OrderLines",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_TenantId_LocationId",
                table: "OrderLines",
                columns: new[] { "TenantId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_TenantId_PriceListId",
                table: "OrderLines",
                columns: new[] { "TenantId", "PriceListId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_TenantId_ReasonCodeId",
                table: "OrderLines",
                columns: new[] { "TenantId", "ReasonCodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_TenantId_TaxRuleId",
                table: "OrderLines",
                columns: new[] { "TenantId", "TaxRuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_TenantId_TerminalId",
                table: "OrderLines",
                columns: new[] { "TenantId", "TerminalId" });

            migrationBuilder.CreateIndex(
                name: "UX_OrderLines_Tenant_IdempotencyKey",
                table: "OrderLines",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_OrderLines_Tenant_Order_LineNumber",
                table: "OrderLines",
                columns: new[] { "TenantId", "OrderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Tenant_BusinessDate",
                table: "Orders",
                columns: new[] { "TenantId", "LocationId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Tenant_CorrelationId",
                table: "Orders",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Tenant_Status",
                table: "Orders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_CustomerId",
                table: "Orders",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_EmployeeId",
                table: "Orders",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_OriginalOrderId",
                table: "Orders",
                columns: new[] { "TenantId", "OriginalOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_PriceListId",
                table: "Orders",
                columns: new[] { "TenantId", "PriceListId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_ReceiptTemplateId",
                table: "Orders",
                columns: new[] { "TenantId", "ReceiptTemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_ShiftId",
                table: "Orders",
                columns: new[] { "TenantId", "ShiftId" });

            migrationBuilder.CreateIndex(
                name: "UX_Orders_Tenant_IdempotencyKey",
                table: "Orders",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Orders_Tenant_Location_ReceiptNumber",
                table: "Orders",
                columns: new[] { "TenantId", "LocationId", "ReceiptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Orders_Tenant_Terminal_Sequence",
                table: "Orders",
                columns: new[] { "TenantId", "TerminalId", "TerminalSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Tenant_CorrelationId",
                table: "Payments",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Tenant_ExternalReference",
                table: "Payments",
                columns: new[] { "TenantId", "ExternalPaymentReference" },
                filter: "[ExternalPaymentReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Tenant_Order",
                table: "Payments",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Tenant_Status",
                table: "Payments",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_LocationId",
                table: "Payments",
                columns: new[] { "TenantId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_OriginalPaymentId",
                table: "Payments",
                columns: new[] { "TenantId", "OriginalPaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_ShiftId",
                table: "Payments",
                columns: new[] { "TenantId", "ShiftId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_TenderMethodId",
                table: "Payments",
                columns: new[] { "TenantId", "TenderMethodId" });

            migrationBuilder.CreateIndex(
                name: "UX_Payments_Tenant_IdempotencyKey",
                table: "Payments",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Payments_Tenant_Terminal_Sequence",
                table: "Payments",
                columns: new[] { "TenantId", "TerminalId", "TerminalSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_Tenant_Effective",
                table: "PriceLists",
                columns: new[] { "TenantId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_Tenant_Type",
                table: "PriceLists",
                columns: new[] { "TenantId", "PriceListType" });

            migrationBuilder.CreateIndex(
                name: "UX_PriceLists_Tenant_Code_Version",
                table: "PriceLists",
                columns: new[] { "TenantId", "Code", "PriceListVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReasonCodes_Tenant_Category_Active",
                table: "ReasonCodes",
                columns: new[] { "TenantId", "ReasonCategory", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_ReasonCodes_Tenant_Category_Code",
                table: "ReasonCodes",
                columns: new[] { "TenantId", "ReasonCategory", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptTemplates_Tenant_Effective",
                table: "ReceiptTemplates",
                columns: new[] { "TenantId", "TemplateCode", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "UX_ReceiptTemplates_Tenant_Code_Version",
                table: "ReceiptTemplates",
                columns: new[] { "TenantId", "TemplateCode", "TemplateVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_Tenant_CorrelationId",
                table: "Shifts",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_Tenant_Location_Date",
                table: "Shifts",
                columns: new[] { "TenantId", "LocationId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_Tenant_Status",
                table: "Shifts",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_TenantId_ClosedByEmployeeId",
                table: "Shifts",
                columns: new[] { "TenantId", "ClosedByEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_TenantId_OpenedByEmployeeId",
                table: "Shifts",
                columns: new[] { "TenantId", "OpenedByEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "UX_Shifts_Tenant_IdempotencyKey",
                table: "Shifts",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Shifts_Tenant_Terminal_Sequence",
                table: "Shifts",
                columns: new[] { "TenantId", "TerminalId", "TerminalSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncIngestAcks_Tenant_CorrelationId",
                table: "SyncIngestAcks",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncIngestAcks_Tenant_ExpiresOn",
                table: "SyncIngestAcks",
                columns: new[] { "TenantId", "ExpiresOn" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncIngestAcks_TenantId_LocationId",
                table: "SyncIngestAcks",
                columns: new[] { "TenantId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "UX_SyncIngestAcks_Tenant_ChunkKey",
                table: "SyncIngestAcks",
                columns: new[] { "TenantId", "ChunkIdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SyncIngestAcks_Tenant_Terminal_Sequence",
                table: "SyncIngestAcks",
                columns: new[] { "TenantId", "TerminalId", "ChunkSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxRules_Tenant_Effective",
                table: "TaxRules",
                columns: new[] { "TenantId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "UX_TaxRules_Tenant_Code_Version",
                table: "TaxRules",
                columns: new[] { "TenantId", "Code", "RuleVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantModules_ModuleId",
                table: "TenantModules",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantModules_Tenant_Status",
                table: "TenantModules",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_TenantModules_Tenant_Module",
                table: "TenantModules",
                columns: new[] { "TenantId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenderMethods_Tenant_Type",
                table: "TenderMethods",
                columns: new[] { "TenantId", "TenderType" });

            migrationBuilder.CreateIndex(
                name: "UX_TenderMethods_Tenant_Code",
                table: "TenderMethods",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Terminals_Tenant_Status",
                table: "Terminals",
                columns: new[] { "TenantId", "ProvisioningStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Terminals_TenantId_LastPriceListId",
                table: "Terminals",
                columns: new[] { "TenantId", "LastPriceListId" });

            migrationBuilder.CreateIndex(
                name: "IX_Terminals_TenantId_LastReceiptTemplateId",
                table: "Terminals",
                columns: new[] { "TenantId", "LastReceiptTemplateId" });

            migrationBuilder.CreateIndex(
                name: "UX_Terminals_Tenant_DeviceId",
                table: "Terminals",
                columns: new[] { "TenantId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Terminals_Tenant_Location_Code",
                table: "Terminals",
                columns: new[] { "TenantId", "LocationId", "TerminalCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UiLayouts_Tenant_Effective",
                table: "UiLayouts",
                columns: new[] { "TenantId", "ScreenKey", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "UX_UiLayouts_Tenant_Location_Screen_Version",
                table: "UiLayouts",
                columns: new[] { "TenantId", "LocationId", "ScreenKey", "LayoutVersion" },
                unique: true,
                filter: "[LocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UnitsOfMeasure_Tenant_MeasurementType",
                table: "UnitsOfMeasure",
                columns: new[] { "TenantId", "MeasurementType" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitsOfMeasure_TenantId_BaseUnitId",
                table: "UnitsOfMeasure",
                columns: new[] { "TenantId", "BaseUnitId" });

            migrationBuilder.CreateIndex(
                name: "UX_UnitsOfMeasure_Tenant_Code",
                table: "UnitsOfMeasure",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZReports_Tenant_CorrelationId",
                table: "ZReports",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ZReports_Tenant_Date",
                table: "ZReports",
                columns: new[] { "TenantId", "LocationId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ZReports_Tenant_Status",
                table: "ZReports",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ZReports_TenantId_GeneratedByEmployeeId",
                table: "ZReports",
                columns: new[] { "TenantId", "GeneratedByEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ZReports_TenantId_ShiftId",
                table: "ZReports",
                columns: new[] { "TenantId", "ShiftId" });

            migrationBuilder.CreateIndex(
                name: "IX_ZReports_TenantId_TerminalId",
                table: "ZReports",
                columns: new[] { "TenantId", "TerminalId" });

            migrationBuilder.CreateIndex(
                name: "UX_ZReports_Tenant_IdempotencyKey",
                table: "ZReports",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ZReports_Tenant_Location_DayReportNumber",
                table: "ZReports",
                columns: new[] { "TenantId", "LocationId", "ReportNumber" },
                unique: true,
                filter: "[TerminalId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ZReports_Tenant_Location_Terminal_ReportNumber",
                table: "ZReports",
                columns: new[] { "TenantId", "LocationId", "TerminalId", "ReportNumber" },
                unique: true,
                filter: "[TerminalId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CashDrawerMovements");

            migrationBuilder.DropTable(
                name: "EmployeeLocationRoles");

            migrationBuilder.DropTable(
                name: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "ItemIdentifiers");

            migrationBuilder.DropTable(
                name: "ItemPrices");

            migrationBuilder.DropTable(
                name: "ItemStocks");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "SyncIngestAcks");

            migrationBuilder.DropTable(
                name: "TenantModules");

            migrationBuilder.DropTable(
                name: "UiLayouts");

            migrationBuilder.DropTable(
                name: "ZReports");

            migrationBuilder.DropTable(
                name: "OrderLines");

            migrationBuilder.DropTable(
                name: "TenderMethods");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "ItemVariants");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "ReasonCodes");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "TaxRules");

            migrationBuilder.DropTable(
                name: "UnitsOfMeasure");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Terminals");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "PriceLists");

            migrationBuilder.DropTable(
                name: "ReceiptTemplates");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
