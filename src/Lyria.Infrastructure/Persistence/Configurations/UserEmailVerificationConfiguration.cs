using Lyria.Domain.Users;
using Lyria.Domain.Users.EmailVerifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class UserEmailVerificationConfiguration
    : IEntityTypeConfiguration<UserEmailVerification>
{
    public void Configure(EntityTypeBuilder<UserEmailVerification> builder)
    {
        builder.ToTable("UsuarioVerificacionesCorreo");

        builder.HasKey(v => v.Id)
            .HasName("PK_UsuarioVerificacionesCorreo");

        builder.Property(v => v.Id)
            .HasColumnName("VerificacionCorreoId")
            .HasConversion(
                id => id.Value,
                value => new UserEmailVerificationId(value))
            .ValueGeneratedNever();

        builder.Property(v => v.UserId)
            .HasColumnName("UsuarioId")
            .HasConversion(
                id => id.Value,
                value => new UserId(value))
            .IsRequired();

        // Sin borrado en cascada: eliminar un usuario no puede borrar en silencio su
        // historial de verificaciones. La relación es uno a muchos porque cada reenvío
        // deja una fila más.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .HasConstraintName("FK_UsuarioVerificacionesCorreo_Usuarios_UsuarioId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.UserId)
            .HasDatabaseName("IX_UsuarioVerificacionesCorreo_UsuarioId");

        // Solo el HMAC-SHA256 en hexadecimal. El código de seis dígitos nunca se persiste.
        builder.Property(v => v.CodeHash)
            .HasColumnName("CodigoHash")
            .HasMaxLength(UserEmailVerification.CodeHashLength)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(v => v.CreatedAtUtc)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(v => v.ExpiresAtUtc)
            .HasColumnName("FechaExpiracion")
            .HasColumnType("datetime2")
            .IsRequired();

        // FechaUso actúa además como token de concurrencia: EF incluye su valor original
        // en el WHERE de cualquier UPDATE de la fila, de modo que dos confirmaciones
        // simultáneas no pueden canjear el mismo código —la segunda no afecta ninguna
        // fila y se resuelve como intento no válido—. No requiere ninguna columna
        // adicional a las autorizadas.
        builder.Property(v => v.UsedAtUtc)
            .HasColumnName("FechaUso")
            .HasColumnType("datetime2")
            .IsConcurrencyToken()
            .IsRequired(false);

        builder.Property(v => v.RevokedAtUtc)
            .HasColumnName("FechaRevocacion")
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Property(v => v.FailedAttempts)
            .HasColumnName("IntentosFallidos")
            .IsRequired();
    }
}
