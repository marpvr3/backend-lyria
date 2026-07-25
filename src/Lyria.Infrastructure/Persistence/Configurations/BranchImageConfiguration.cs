using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class BranchImageConfiguration
    : IEntityTypeConfiguration<BranchImage>
{
    public void Configure(EntityTypeBuilder<BranchImage> builder)
    {
        builder.ToTable("ImagenesSede");

        builder.HasKey(e => e.Id)
            .HasName("PK_ImagenesSede");

        builder.Property(e => e.Id)
            .HasColumnName("ImagenSedeId")
            .HasConversion(
                id => id.Value,
                value => new BranchImageId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.BranchId)
            .HasColumnName("SedeId")
            .HasConversion(
                id => id.Value,
                value => new EstablishmentBranchId(value))
            .IsRequired();

        builder.HasOne<EstablishmentBranch>()
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .HasConstraintName("FK_ImagenesSede_Sedes_SedeId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Url)
            .HasColumnName("Url")
            .HasMaxLength(BranchImage.UrlMaxLength)
            .IsRequired();

        builder.Property(e => e.FileName)
            .HasColumnName("NombreArchivo")
            .HasMaxLength(BranchImage.FileNameMaxLength)
            .IsRequired();

        builder.Property(e => e.AlternativeText)
            .HasColumnName("TextoAlternativo")
            .HasMaxLength(BranchImage.AlternativeTextMaxLength)
            .IsRequired(false);

        builder.Property(e => e.IsPrimary)
            .HasColumnName("EsPrincipal")
            .IsRequired();

        builder.Property(e => e.SortOrder)
            .HasColumnName("Orden")
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("EsActivo")
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .HasColumnName("FechaCreacionUtc")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(e => e.UpdatedAtUtc)
            .HasColumnName("FechaActualizacionUtc")
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.HasIndex(e => e.BranchId)
            .HasDatabaseName("IX_ImagenesSede_SedeId");

        builder.HasIndex(e => new { e.BranchId, e.IsActive, e.SortOrder })
            .HasDatabaseName("IX_ImagenesSede_SedeId_EsActivo_Orden");

        builder.HasIndex(e => new { e.BranchId, e.IsPrimary })
            .HasDatabaseName("IX_ImagenesSede_SedeId_EsPrincipal");
    }
}
