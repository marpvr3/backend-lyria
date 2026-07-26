using Lyria.Domain.Establishments.Branches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class BranchSpecialScheduleConfiguration
    : IEntityTypeConfiguration<BranchSpecialSchedule>
{
    public void Configure(EntityTypeBuilder<BranchSpecialSchedule> builder)
    {
        builder.ToTable("HorariosEspecialesSede");

        builder.HasKey(e => e.Id)
            .HasName("PK_HorariosEspecialesSede");

        builder.Property(e => e.Id)
            .HasColumnName("HorarioEspecialSedeId")
            .HasConversion(
                id => id.Value,
                value => new BranchSpecialScheduleId(value))
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
            .HasConstraintName("FK_HorariosEspecialesSede_Sedes_SedeId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Date)
            .HasColumnName("Fecha")
            .HasColumnType("date")
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

        builder.Property(e => e.Reason)
            .HasColumnName("Motivo")
            .HasMaxLength(BranchSpecialSchedule.ReasonMaxLength)
            .IsRequired(false);

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
            .HasDatabaseName("IX_HorariosEspecialesSede_SedeId");

        builder.HasIndex(e => new { e.BranchId, e.Date })
            .HasDatabaseName("IX_HorariosEspecialesSede_SedeId_Fecha");

        builder.HasIndex(e => new { e.BranchId, e.Date, e.IsActive })
            .HasDatabaseName("IX_HorariosEspecialesSede_SedeId_Fecha_EsActivo");
    }
}
