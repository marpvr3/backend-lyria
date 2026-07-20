using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Restrictions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class EstablishmentBranchRestrictionConfiguration
    : IEntityTypeConfiguration<EstablishmentBranchRestriction>
{
    public void Configure(EntityTypeBuilder<EstablishmentBranchRestriction> builder)
    {
        builder.ToTable("SedesRestricciones");

        builder.HasKey(x => new { x.BranchId, x.RestrictionId })
            .HasName("PK_SedesRestricciones");

        builder.Property(x => x.BranchId)
            .HasColumnName("SedeId")
            .HasConversion(
                id => id.Value,
                value => new EstablishmentBranchId(value));

        builder.Property(x => x.RestrictionId)
            .HasColumnName("RestriccionId")
            .HasConversion(
                id => id.Value,
                value => new RestrictionId(value));

        builder.HasOne<EstablishmentBranch>()
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_SedesRestricciones_Sedes_SedeId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Restriction>()
            .WithMany()
            .HasForeignKey(x => x.RestrictionId)
            .HasConstraintName("FK_SedesRestricciones_Restricciones_RestriccionId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ComplianceLevel)
            .HasColumnName("NivelCumplimiento")
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(x => x.IsCertified)
            .HasColumnName("Certificado")
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

        builder.HasIndex(x => x.RestrictionId)
            .HasDatabaseName("IX_SedesRestricciones_RestriccionId");

        builder.HasIndex(x => new { x.BranchId, x.IsActive })
            .HasDatabaseName("IX_SedesRestricciones_SedeId_Activo");

        builder.HasIndex(x => new { x.RestrictionId, x.IsActive })
            .HasDatabaseName("IX_SedesRestricciones_RestriccionId_Activo");

        builder.HasIndex(x => new { x.BranchId, x.ComplianceLevel })
            .HasDatabaseName("IX_SedesRestricciones_SedeId_NivelCumplimiento");
    }
}
