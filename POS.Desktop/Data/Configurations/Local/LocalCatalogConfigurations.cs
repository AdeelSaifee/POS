using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Data.Configurations.Local;

public class LocalCategoryConfiguration : IEntityTypeConfiguration<LocalCategory>
{
    public void Configure(EntityTypeBuilder<LocalCategory> builder)
    {
        builder.ToTable("LocalCategories", t =>
        {
            t.HasCheckConstraint("CK_LocalCategory_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalCategory_Code_NotEmpty", "Code <> ''");
            t.HasCheckConstraint("CK_LocalCategory_Name_NotEmpty", "Name <> ''");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.SortOrder });
    }
}

public class LocalItemConfiguration : IEntityTypeConfiguration<LocalItem>
{
    public void Configure(EntityTypeBuilder<LocalItem> builder)
    {
        builder.ToTable("LocalItems", t =>
        {
            t.HasCheckConstraint("CK_LocalItem_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalItem_Code_NotEmpty", "ItemCode <> ''");
            t.HasCheckConstraint("CK_LocalItem_Name_NotEmpty", "Name <> ''");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ItemCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.ItemCode }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CategoryId });
        builder.HasIndex(x => new { x.TenantId, x.Name });
    }
}

public class LocalItemVariantConfiguration : IEntityTypeConfiguration<LocalItemVariant>
{
    public void Configure(EntityTypeBuilder<LocalItemVariant> builder)
    {
        builder.ToTable("LocalItemVariants", t =>
        {
            t.HasCheckConstraint("CK_LocalItemVariant_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalItemVariant_Code_NotEmpty", "VariantCode <> ''");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.VariantCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.VariantCode }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.SKU });
        builder.HasIndex(x => new { x.TenantId, x.ItemId });
    }
}

public class LocalItemIdentifierConfiguration : IEntityTypeConfiguration<LocalItemIdentifier>
{
    public void Configure(EntityTypeBuilder<LocalItemIdentifier> builder)
    {
        builder.ToTable("LocalItemIdentifiers", t =>
        {
            t.HasCheckConstraint("CK_LocalItemIdentifier_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalItemIdentifier_Value_NotEmpty", "IdentifierValue <> ''");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.IdentifierValue).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.IdentifierValue });
        builder.HasIndex(x => new { x.TenantId, x.ItemVariantId });
    }
}

public class LocalItemPriceConfiguration : IEntityTypeConfiguration<LocalItemPrice>
{
    public void Configure(EntityTypeBuilder<LocalItemPrice> builder)
    {
        builder.ToTable("LocalItemPrices", t =>
        {
            t.HasCheckConstraint("CK_LocalItemPrice_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalItemPrice_UnitPrice", "UnitPrice >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.TenantId, x.ItemVariantId, x.PriceListId });
    }
}

public class LocalUnitOfMeasureConfiguration : IEntityTypeConfiguration<LocalUnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<LocalUnitOfMeasure> builder)
    {
        builder.ToTable("LocalUnitsOfMeasure", t =>
        {
            t.HasCheckConstraint("CK_LocalUom_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalUom_Code_NotEmpty", "Code <> ''");
            t.HasCheckConstraint("CK_LocalUom_Name_NotEmpty", "Name <> ''");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

public class LocalTaxRuleConfiguration : IEntityTypeConfiguration<LocalTaxRule>
{
    public void Configure(EntityTypeBuilder<LocalTaxRule> builder)
    {
        builder.ToTable("LocalTaxRules", t =>
        {
            t.HasCheckConstraint("CK_LocalTaxRule_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalTaxRule_Code_NotEmpty", "Code <> ''");
            t.HasCheckConstraint("CK_LocalTaxRule_Name_NotEmpty", "Name <> ''");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

public class LocalTenderMethodConfiguration : IEntityTypeConfiguration<LocalTenderMethod>
{
    public void Configure(EntityTypeBuilder<LocalTenderMethod> builder)
    {
        builder.ToTable("LocalTenderMethods", t =>
        {
            t.HasCheckConstraint("CK_LocalTenderMethod_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalTenderMethod_Code_NotEmpty", "Code <> ''");
            t.HasCheckConstraint("CK_LocalTenderMethod_Name_NotEmpty", "Name <> ''");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

public class LocalReasonCodeConfiguration : IEntityTypeConfiguration<LocalReasonCode>
{
    public void Configure(EntityTypeBuilder<LocalReasonCode> builder)
    {
        builder.ToTable("LocalReasonCodes", t =>
        {
            t.HasCheckConstraint("CK_LocalReasonCode_TenantId", "TenantId > 0");
            t.HasCheckConstraint("CK_LocalReasonCode_Code_NotEmpty", "Code <> ''");
            t.HasCheckConstraint("CK_LocalReasonCode_Name_NotEmpty", "Name <> ''");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}
