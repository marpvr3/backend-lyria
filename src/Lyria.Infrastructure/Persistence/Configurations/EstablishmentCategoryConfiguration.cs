using Lyria.Domain.Establishments.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class EstablishmentCategoryConfiguration
    : IEntityTypeConfiguration<EstablishmentCategory>
{
    public void Configure(EntityTypeBuilder<EstablishmentCategory> builder)
    {
        builder.ToTable("CategoriasEstablecimiento");

        builder.HasKey(c => c.Id)
            .HasName("PK_CategoriasEstablecimiento");

        builder.Property(c => c.Id)
            .HasColumnName("CategoriaEstablecimientoId")
            .HasConversion(
                id => id.Value,
                value => new EstablishmentCategoryId(value))
            .ValueGeneratedNever();

        builder.Property(c => c.Name)
            .HasColumnName("Nombre")
            .HasMaxLength(EstablishmentCategory.NameMaxLength)
            .IsUnicode(false)
            .UseCollation("SQL_Latin1_General_CP1_CI_AS")
            .IsRequired();

        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("UX_CategoriasEstablecimiento_Nombre");

        builder.Property(c => c.Description)
            .HasColumnName("Descripcion")
            .HasMaxLength(EstablishmentCategory.DescriptionMaxLength)
            .IsRequired(false);

        builder.Property(c => c.IconUrl)
            .HasColumnName("IconoUrl")
            .HasMaxLength(EstablishmentCategory.IconUrlMaxLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(c => c.SortOrder)
            .HasColumnName("Orden")
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasColumnName("Activo")
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Ignore(c => c.DomainEvents);
    }
}
