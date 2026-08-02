using Lyria.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id)
            .HasName("PK_Usuarios");

        builder.Property(u => u.Id)
            .HasColumnName("UsuarioId")
            .HasConversion(
                id => id.Value,
                value => new UserId(value))
            .ValueGeneratedNever();

        builder.Property(u => u.Name)
            .HasColumnName("Nombre")
            .HasMaxLength(User.NameMaxLength)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasColumnName("Apellido")
            .HasMaxLength(User.LastNameMaxLength)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("Email")
            .HasMaxLength(User.EmailMaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("UX_Usuarios_Email");

        builder.Property(u => u.PasswordHash)
            .HasColumnName("HashContrasena")
            .HasMaxLength(User.PasswordHashMaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(u => u.Phone)
            .HasColumnName("Telefono")
            .HasMaxLength(User.PhoneMaxLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(u => u.BirthDate)
            .HasColumnName("FechaNacimiento")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(u => u.PhotoUrl)
            .HasColumnName("FotoUrl")
            .HasMaxLength(User.PhotoUrlMaxLength)
            .IsRequired(false);

        builder.Property(u => u.Status)
            .HasColumnName("Estado")
            .HasMaxLength(10)
            .IsUnicode(false)
            .HasConversion(
                status => status.ToString(),
                value => Enum.Parse<UserStatus>(value))
            .IsRequired();

        builder.Property(u => u.IsEmailVerified)
            .HasColumnName("EmailVerificado")
            .IsRequired();

        builder.Property(u => u.LastLoginAtUtc)
            .HasColumnName("UltimaConexion")
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Property(u => u.CreatedAtUtc)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(u => u.UpdatedAtUtc)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Ignore(u => u.DomainEvents);
    }
}
