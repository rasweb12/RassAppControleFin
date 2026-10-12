using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RassAppControleFin.Domain.Entities;

namespace RassAppControleFin.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", table => table.HasCheckConstraint("CK_Tenants_Name",
            "length(btrim(\"Name\")) BETWEEN 1 AND 120"));
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Id).ValueGeneratedNever();
        builder.Property(tenant => tenant.Name).HasMaxLength(120).IsRequired();
        builder.Property(tenant => tenant.OwnerUserId).HasMaxLength(256).IsRequired()
            .HasDefaultValueSql("COALESCE(NULLIF(current_setting('app.user_id', true), ''), 'legacy-unassigned')");
        builder.Property(tenant => tenant.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Ignore(tenant => tenant.DomainEvents);
    }
}

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles", table =>
        {
            table.HasCheckConstraint("CK_UserProfiles_UserId", "length(btrim(\"UserId\")) BETWEEN 1 AND 256");
            table.HasCheckConstraint("CK_UserProfiles_DisplayName", "length(btrim(\"DisplayName\")) BETWEEN 1 AND 120");
            table.HasCheckConstraint("CK_UserProfiles_Version", "\"Version\" > 0");
        });
        builder.HasKey(profile => new { profile.TenantId, profile.Id });
        builder.Property(profile => profile.Id).ValueGeneratedNever();
        builder.Property(profile => profile.UserId).HasMaxLength(256).IsRequired();
        builder.Property(profile => profile.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(profile => profile.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(profile => profile.Version).IsConcurrencyToken();
        builder.HasIndex(profile => new { profile.TenantId, profile.UserId }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(profile => profile.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(profile => profile.DomainEvents);
    }
}
