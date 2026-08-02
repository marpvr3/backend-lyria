using Lyria.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration
    : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id)
            .HasName("PK_Roles");

        builder.Property(r => r.Id)
            .HasColumnName("RolId")
            .HasConversion(
                id => id.Value,
                value => new RoleId(value))
            .ValueGeneratedNever();

        builder.Property(r => r.Code)
            .HasColumnName("Codigo")
            .HasMaxLength(Role.CodeMaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(r => r.Code)
            .IsUnique()
            .HasDatabaseName("UX_Roles_Codigo");

        builder.Property(r => r.Name)
            .HasColumnName("Nombre")
            .HasMaxLength(Role.NameMaxLength)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("Descripcion")
            .HasMaxLength(Role.DescriptionMaxLength)
            .IsRequired(false);

        builder.Property(r => r.IsActive)
            .HasColumnName("Activo")
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Ignore(r => r.DomainEvents);
    }
}
