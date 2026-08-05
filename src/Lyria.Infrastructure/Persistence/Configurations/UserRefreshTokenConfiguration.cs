using Lyria.Domain.Users;
using Lyria.Domain.Users.RefreshTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class UserRefreshTokenConfiguration
    : IEntityTypeConfiguration<UserRefreshToken>
{
    public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
    {
        builder.ToTable("UsuarioRefreshTokens");

        builder.HasKey(rt => rt.Id)
            .HasName("PK_UsuarioRefreshTokens");

        builder.Property(rt => rt.Id)
            .HasColumnName("RefreshTokenId")
            .HasConversion(
                id => id.Value,
                value => new UserRefreshTokenId(value))
            .ValueGeneratedNever();

        builder.Property(rt => rt.UserId)
            .HasColumnName("UsuarioId")
            .HasConversion(
                id => id.Value,
                value => new UserId(value))
            .IsRequired();

        // Sin borrado en cascada: eliminar un usuario no puede borrar en silencio su
        // historial de sesiones.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .HasConstraintName("FK_UsuarioRefreshTokens_Usuarios_UsuarioId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_UsuarioRefreshTokens_UsuarioId");

        // Solo el hash SHA-256 en hexadecimal. El token en claro nunca se persiste.
        builder.Property(rt => rt.TokenHash)
            .HasColumnName("TokenHash")
            .HasMaxLength(UserRefreshToken.TokenHashLength)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_UsuarioRefreshTokens_TokenHash");

        builder.Property(rt => rt.CreatedAtUtc)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(rt => rt.ExpiresAtUtc)
            .HasColumnName("FechaExpiracion")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(rt => rt.RevokedAtUtc)
            .HasColumnName("FechaRevocacion")
            .HasColumnType("datetime2")
            .IsRequired(false);
    }
}
