using Lyria.Domain.Restrictions;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRestrictions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class UserRestrictionConfiguration
    : IEntityTypeConfiguration<UserRestriction>
{
    public void Configure(EntityTypeBuilder<UserRestriction> builder)
    {
        builder.ToTable("UsuarioRestricciones", "dbo");

        builder.HasKey(x => new { x.UserId, x.RestrictionId })
            .HasName("PK_UsuarioRestricciones");

        builder.Property(x => x.UserId)
            .HasColumnName("UsuarioId")
            .HasConversion(
                id => id.Value,
                value => new UserId(value));

        builder.Property(x => x.RestrictionId)
            .HasColumnName("RestriccionId")
            .HasConversion(
                id => id.Value,
                value => new RestrictionId(value));

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_UsuarioRestricciones_Usuarios_UsuarioId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Restriction>()
            .WithMany()
            .HasForeignKey(x => x.RestrictionId)
            .HasConstraintName("FK_UsuarioRestricciones_Restricciones_RestriccionId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ImportanceLevel)
            .HasColumnName("NivelImportancia")
            .HasMaxLength(UserRestrictionImportanceLevels.MaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2")
            .IsRequired();
    }
}
