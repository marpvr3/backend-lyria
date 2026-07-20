using Lyria.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class ServiceConfiguration
    : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Servicios");

        builder.HasKey(s => s.Id)
            .HasName("PK_Servicios");

        builder.Property(s => s.Id)
            .HasColumnName("ServicioId")
            .HasConversion(
                id => id.Value,
                value => new ServiceId(value))
            .ValueGeneratedNever();

        builder.Property(s => s.Name)
            .HasColumnName("Nombre")
            .HasColumnType("varchar(100)")
            .HasMaxLength(Service.NameMaxLength)
            .IsUnicode(false)
            .UseCollation("SQL_Latin1_General_CP1_CI_AS")
            .IsRequired();

        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("UX_Servicios_Nombre");

        builder.Property(s => s.Description)
            .HasColumnName("Descripcion")
            .HasColumnType("varchar(500)")
            .HasMaxLength(Service.DescriptionMaxLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(s => s.IconUrl)
            .HasColumnName("IconoUrl")
            .HasColumnType("varchar(500)")
            .HasMaxLength(Service.IconUrlMaxLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(s => s.IsActive)
            .HasColumnName("Activo")
            .IsRequired();

        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_Servicios_Activo");

        builder.Property(s => s.CreatedAtUtc)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Ignore(s => s.DomainEvents);
    }
}
