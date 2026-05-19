using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Shared.Domain.Entities.Central;
using POS.Shared.Enums;

namespace POS.Tests.Integration;

internal static class ApiTestDataSeeder
{
    public static async Task SeedAsync(PosCentralDbContext dbContext)
    {
        if (dbContext.Companies.Any())
        {
            return;
        }

        var createdOn = new DateTimeOffset(2026, 5, 19, 0, 0, 0, TimeSpan.Zero);
        const string createdBy = "integration-tests";

        dbContext.Companies.AddRange(
            new Company
            {
                Id = 101,
                Code = "TENANT-101",
                Name = "Tenant 101",
                DefaultCurrencyCode = "PKR",
                TimeZoneId = "Asia/Karachi",
                Status = CompanyStatus.Active,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedOn = createdOn
            },
            new Company
            {
                Id = 202,
                Code = "TENANT-202",
                Name = "Tenant 202",
                DefaultCurrencyCode = "USD",
                TimeZoneId = "UTC",
                Status = CompanyStatus.Active,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedOn = createdOn
            });

        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Companies ON");
        await dbContext.SaveChangesAsync();
        await dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Companies OFF");
        await dbContext.Database.CloseConnectionAsync();

        dbContext.Locations.AddRange(
            new Location
            {
                TenantId = 101,
                Code = "LOC-101-A",
                Name = "Tenant 101 Store",
                LocationType = LocationType.Store,
                TimeZoneId = "Asia/Karachi",
                AllowsNegativeStock = false,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedOn = createdOn
            },
            new Location
            {
                TenantId = 202,
                Code = "LOC-202-A",
                Name = "Tenant 202 Store",
                LocationType = LocationType.Warehouse,
                TimeZoneId = "UTC",
                AllowsNegativeStock = true,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedOn = createdOn
            });

        dbContext.Categories.AddRange(
            new Category
            {
                TenantId = 101,
                Code = "CAT-101-A",
                Name = "Tenant 101 Category",
                SortOrder = 10,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedOn = createdOn
            },
            new Category
            {
                TenantId = 202,
                Code = "CAT-202-A",
                Name = "Tenant 202 Category",
                SortOrder = 20,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedOn = createdOn
            });

        dbContext.UnitsOfMeasure.AddRange(
            new UnitOfMeasure
            {
                TenantId = 101,
                Code = "EA-101",
                Name = "Each 101",
                MeasurementType = MeasurementType.Count,
                DecimalPlaces = 0,
                AllowsFractionalQuantity = false,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedOn = createdOn
            },
            new UnitOfMeasure
            {
                TenantId = 202,
                Code = "KG-202",
                Name = "Kilogram 202",
                MeasurementType = MeasurementType.Weight,
                DecimalPlaces = 3,
                AllowsFractionalQuantity = true,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedOn = createdOn
            });

        await dbContext.SaveChangesAsync();
    }
}
