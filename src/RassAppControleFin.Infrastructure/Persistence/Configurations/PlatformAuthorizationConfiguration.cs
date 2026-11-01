using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RassAppControleFin.Domain.Entities;

namespace RassAppControleFin.Infrastructure.Persistence.Configurations;

internal sealed class PlatformAuthorizationConfiguration : IEntityTypeConfiguration<PlatformRoleAssignment>
{
    public void Configure(EntityTypeBuilder<PlatformRoleAssignment> builder)
    {
        builder.ToTable("PlatformRoleAssignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.GrantedBy).HasMaxLength(256).IsRequired();
        builder.Property(x => x.RevokedBy).HasMaxLength(256);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Role }).HasFilter("\"IsActive\"").IsUnique();
        builder.HasIndex(x => new { x.Role, x.IsActive });
    }
}
