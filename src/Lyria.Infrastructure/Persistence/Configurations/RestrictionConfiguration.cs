using Lyria.Domain.Restrictions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class RestrictionConfiguration
    : IEntityTypeConfiguration<Restriction>
{
    public void Configure(EntityTypeBuilder<Restriction> builder)
    {
        builder.ToTable("Restricciones");

        builder.HasKey(r => r.Id)
            .HasName("PK_Restricciones");

        builder.Property(r => r.Id)
            .HasColumnName("RestriccionId")
            .HasConversion(
                id => id.Value,
                value => new RestrictionId(value))
            .ValueGeneratedNever();

        builder.Property(r => r.Name)
            .HasColumnName("Nombre")
            .HasColumnType("varchar(100)")
            .HasMaxLength(Restriction.NameMaxLength)
            .IsUnicode(false)
            .UseCollation("SQL_Latin1_General_CP1_CI_AS")
            .IsRequired();

        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("UX_Restricciones_Nombre");

        builder.Property(r => r.Description)
            .HasColumnName("Descripcion")
            .HasColumnType("varchar(500)")
            .HasMaxLength(Restriction.DescriptionMaxLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(r => r.IsActive)
            .HasColumnName("Activo")
            .IsRequired();

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("IX_Restricciones_Activo");

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
