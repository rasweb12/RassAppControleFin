using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RassAppControleFin.Domain.Entities;

namespace RassAppControleFin.Infrastructure.Persistence.Configurations;

internal sealed class PlanDefinitionConfiguration : IEntityTypeConfiguration<PlanDefinition>
{
    public void Configure(EntityTypeBuilder<PlanDefinition> builder)
    {
        builder.ToTable("PlanDefinitions", table => table.HasCheckConstraint("CK_PlanDefinitions_Price", "\"ReferencePrice\" >= 0"));
        builder.HasKey(x => x.Plan); builder.Property(x => x.Plan).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.DisplayName).HasMaxLength(120).IsRequired(); builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ReferencePrice).HasPrecision(18, 2); builder.Property(x => x.StoreProductId).HasMaxLength(160);
    }
}

internal sealed class PlanEntitlementConfiguration : IEntityTypeConfiguration<PlanEntitlement>
{
    public void Configure(EntityTypeBuilder<PlanEntitlement> builder)
    {
        builder.ToTable("PlanEntitlements", table => table.HasCheckConstraint("CK_PlanEntitlements_Limit", "\"Limit\" IS NULL OR \"Limit\" >= 0"));
        builder.HasKey(x => new { x.Plan, x.Feature }); builder.Property(x => x.Plan).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Feature).HasMaxLength(120); builder.HasIndex(x => x.Feature);
    }
}

internal sealed class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("UserSubscriptions"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).HasMaxLength(256).IsRequired(); builder.Property(x => x.Plan).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32); builder.Property(x => x.Provider).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ExternalPurchaseId).HasMaxLength(512); builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => new { x.Provider, x.ExternalPurchaseId }).IsUnique().HasFilter("\"ExternalPurchaseId\" IS NOT NULL");
    }
}

internal sealed class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
{
    public void Configure(EntityTypeBuilder<PaymentEvent> builder)
    {
        builder.ToTable("PaymentEvents"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Provider).HasConversion<string>().HasMaxLength(32); builder.Property(x => x.ProviderEventId).HasMaxLength(512).IsRequired();
        builder.Property(x => x.PurchaseFingerprint).HasMaxLength(64).IsRequired(); builder.Property(x => x.ProductId).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32); builder.HasIndex(x => new { x.Provider, x.ProviderEventId }).IsUnique();
        builder.HasIndex(x => new { x.Provider, x.PurchaseFingerprint }).IsUnique();
    }
}
