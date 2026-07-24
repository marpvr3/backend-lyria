using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class BranchScheduleConfiguration
    : IEntityTypeConfiguration<BranchSchedule>
{
    public void Configure(EntityTypeBuilder<BranchSchedule> builder)
    {
        builder.ToTable("HorariosSede");

        builder.HasKey(e => e.Id)
            .HasName("PK_HorariosSede");

        builder.Property(e => e.Id)
            .HasColumnName("HorarioSedeId")
            .HasConversion(
                id => id.Value,
                value => new BranchScheduleId(value))
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
            .HasConstraintName("FK_HorariosSede_Sedes_SedeId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.DayOfWeek)
            .HasColumnName("DiaSemana")
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(e => e.OpeningTime)
            .HasColumnName("HoraApertura")
            .HasColumnType("time")
            .IsRequired(false);

        builder.Property(e => e.ClosingTime)
            .HasColumnName("HoraCierre")
            .HasColumnType("time")
            .IsRequired(false);

        builder.Property(e => e.CrossesMidnight)
            .HasColumnName("CruzaMedianoche")
            .IsRequired();

        builder.Property(e => e.IsClosed)
            .HasColumnName("Cerrado")
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
            .HasDatabaseName("IX_HorariosSede_SedeId");

        builder.HasIndex(e => new { e.BranchId, e.DayOfWeek })
            .HasDatabaseName("IX_HorariosSede_SedeId_DiaSemana");

        builder.HasIndex(e => new { e.BranchId, e.DayOfWeek, e.IsActive })
            .HasDatabaseName("IX_HorariosSede_SedeId_DiaSemana_EsActivo");
    }
}
