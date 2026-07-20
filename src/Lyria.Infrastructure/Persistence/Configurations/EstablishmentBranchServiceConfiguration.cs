using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class EstablishmentBranchServiceConfiguration
    : IEntityTypeConfiguration<EstablishmentBranchService>
{
    public void Configure(EntityTypeBuilder<EstablishmentBranchService> builder)
    {
        builder.ToTable("SedesServicios");

        builder.HasKey(x => new { x.BranchId, x.ServiceId })
            .HasName("PK_SedesServicios");

        builder.Property(x => x.BranchId)
            .HasColumnName("SedeId")
            .HasConversion(
                id => id.Value,
                value => new EstablishmentBranchId(value));

        builder.Property(x => x.ServiceId)
            .HasColumnName("ServicioId")
            .HasConversion(
                id => id.Value,
                value => new ServiceId(value));

        builder.HasOne<EstablishmentBranch>()
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_SedesServicios_Sedes_SedeId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Service>()
            .WithMany()
            .HasForeignKey(x => x.ServiceId)
            .HasConstraintName("FK_SedesServicios_Servicios_ServicioId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.IsAvailable)
            .HasColumnName("Disponible")
            .IsRequired();

        builder.Property(x => x.Observation)
            .HasColumnName("Observacion")
            .HasMaxLength(500)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasColumnName("Activo")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.HasIndex(x => x.ServiceId)
            .HasDatabaseName("IX_SedesServicios_ServicioId");

        builder.HasIndex(x => new { x.BranchId, x.IsActive })
            .HasDatabaseName("IX_SedesServicios_SedeId_Activo");

        builder.HasIndex(x => new { x.ServiceId, x.IsActive })
            .HasDatabaseName("IX_SedesServicios_ServicioId_Activo");
    }
}
