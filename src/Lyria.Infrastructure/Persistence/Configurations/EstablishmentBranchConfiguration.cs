using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class EstablishmentBranchConfiguration
    : IEntityTypeConfiguration<EstablishmentBranch>
{
    public void Configure(EntityTypeBuilder<EstablishmentBranch> builder)
    {
        builder.ToTable("Sedes");

        builder.HasKey(e => e.Id)
            .HasName("PK_Sedes");

        builder.Property(e => e.Id)
            .HasColumnName("SedeId")
            .HasConversion(
                id => id.Value,
                value => new EstablishmentBranchId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.EstablishmentId)
            .HasColumnName("EstablecimientoId")
            .HasConversion(
                id => id.Value,
                value => new EstablishmentId(value))
            .IsRequired();

        builder.HasOne<Establishment>()
            .WithMany()
            .HasForeignKey(e => e.EstablishmentId)
            .HasConstraintName("FK_Sedes_Establecimientos_EstablecimientoId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Name)
            .HasColumnName("Nombre")
            .HasMaxLength(EstablishmentBranch.NameMaxLength)
            .IsRequired();

        builder.HasIndex(e => new { e.EstablishmentId, e.Name })
            .IsUnique()
            .HasDatabaseName("UX_Sedes_EstablecimientoId_Nombre");

        builder.HasIndex(e => new { e.EstablishmentId, e.IsActive })
            .HasDatabaseName("IX_Sedes_EstablecimientoId_Activo");

        builder.Property(e => e.Street)
            .HasColumnName("Calle")
            .HasColumnType("varchar(150)")
            .HasMaxLength(EstablishmentBranch.StreetMaxLength)
            .IsRequired();

        builder.Property(e => e.Number)
            .HasColumnName("Numero")
            .HasColumnType("varchar(20)")
            .HasMaxLength(EstablishmentBranch.NumberMaxLength)
            .IsRequired(false);

        builder.Property(e => e.AddressComplement)
            .HasColumnName("ComplementoDireccion")
            .HasColumnType("varchar(150)")
            .HasMaxLength(EstablishmentBranch.AddressComplementMaxLength)
            .IsRequired(false);

        builder.Property(e => e.Neighborhood)
            .HasColumnName("Barrio")
            .HasMaxLength(EstablishmentBranch.NeighborhoodMaxLength)
            .IsRequired(false);

        builder.Property(e => e.City)
            .HasColumnName("Ciudad")
            .HasMaxLength(EstablishmentBranch.CityMaxLength)
            .IsRequired(false);

        builder.HasIndex(e => e.City)
            .HasDatabaseName("IX_Sedes_Ciudad");

        builder.Property(e => e.Province)
            .HasColumnName("Provincia")
            .HasMaxLength(EstablishmentBranch.ProvinceMaxLength)
            .IsRequired(false);

        builder.HasIndex(e => e.Province)
            .HasDatabaseName("IX_Sedes_Provincia");

        builder.Property(e => e.PostalCode)
            .HasColumnName("CodigoPostal")
            .HasColumnType("varchar(20)")
            .HasMaxLength(EstablishmentBranch.PostalCodeMaxLength)
            .IsRequired(false);

        builder.Property(e => e.Country)
            .HasColumnName("Pais")
            .HasMaxLength(EstablishmentBranch.CountryMaxLength)
            .IsRequired(false);

        builder.Property(e => e.Latitude)
            .HasColumnName("Latitud")
            .HasColumnType("decimal(9,6)")
            .IsRequired(false);

        builder.Property(e => e.Longitude)
            .HasColumnName("Longitud")
            .HasColumnType("decimal(9,6)")
            .IsRequired(false);

        builder.Property(e => e.Phone)
            .HasColumnName("Telefono")
            .HasColumnType("varchar(30)")
            .HasMaxLength(EstablishmentBranch.PhoneMaxLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.WhatsApp)
            .HasColumnName("Whatsapp")
            .HasColumnType("varchar(30)")
            .HasMaxLength(EstablishmentBranch.WhatsAppMaxLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.Email)
            .HasColumnName("Email")
            .HasColumnType("varchar(254)")
            .HasMaxLength(EstablishmentBranch.EmailMaxLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.TimeZoneId)
            .HasColumnName("ZonaHoraria")
            .HasMaxLength(EstablishmentBranch.TimeZoneIdMaxLength)
            .IsRequired();

        builder.Property(e => e.RatingAverage)
            .HasColumnName("RatingPromedio")
            .HasColumnType("decimal(3,2)")
            .IsRequired();

        builder.Property(e => e.TotalReviews)
            .HasColumnName("TotalResenas")
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("Activo")
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(e => e.UpdatedAtUtc)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2")
            .IsRequired(false);
    }
}
