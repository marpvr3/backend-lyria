using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Branches;
using Lyria.Domain.Roles;
using Lyria.Domain.Users;
using Lyria.Domain.Users.UserRoles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class UserRoleConfiguration
    : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UsuarioRoles");

        builder.HasKey(ur => ur.Id)
            .HasName("PK_UsuarioRoles");

        builder.Property(ur => ur.Id)
            .HasColumnName("UsuarioRolId")
            .HasConversion(
                id => id.Value,
                value => new UserRoleId(value))
            .ValueGeneratedNever();

        builder.Property(ur => ur.UserId)
            .HasColumnName("UsuarioId")
            .HasConversion(
                id => id.Value,
                value => new UserId(value))
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ur => ur.UserId)
            .HasConstraintName("FK_UsuarioRoles_Usuarios_UsuarioId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ur => ur.UserId)
            .HasDatabaseName("IX_UsuarioRoles_UsuarioId");

        builder.Property(ur => ur.RoleId)
            .HasColumnName("RolId")
            .HasConversion(
                id => id.Value,
                value => new RoleId(value))
            .IsRequired();

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .HasConstraintName("FK_UsuarioRoles_Roles_RolId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ur => ur.RoleId)
            .HasDatabaseName("IX_UsuarioRoles_RolId");

        builder.Property(ur => ur.ScopeType)
            .HasColumnName("AlcanceTipo")
            .HasMaxLength(13)
            .IsUnicode(false)
            .HasConversion(
                scope => scope.ToString(),
                value => Enum.Parse<ScopeType>(value))
            .IsRequired();

        builder.Property(ur => ur.EstablishmentId)
            .HasColumnName("EstablecimientoId")
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? null : new EstablishmentId(value.Value))
            .IsRequired(false);

        builder.HasOne<Establishment>()
            .WithMany()
            .HasForeignKey(ur => ur.EstablishmentId)
            .HasConstraintName("FK_UsuarioRoles_Establecimientos_EstablecimientoId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ur => ur.EstablishmentId)
            .HasDatabaseName("IX_UsuarioRoles_EstablecimientoId")
            .HasFilter("[EstablecimientoId] IS NOT NULL");

        builder.Property(ur => ur.BranchId)
            .HasColumnName("SedeId")
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? null : new EstablishmentBranchId(value.Value))
            .IsRequired(false);

        builder.HasOne<EstablishmentBranch>()
            .WithMany()
            .HasForeignKey(ur => ur.BranchId)
            .HasConstraintName("FK_UsuarioRoles_Sedes_SedeId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ur => ur.BranchId)
            .HasDatabaseName("IX_UsuarioRoles_SedeId")
            .HasFilter("[SedeId] IS NOT NULL");

        builder.Property(ur => ur.IsActive)
            .HasColumnName("Activo")
            .IsRequired();

        builder.Property(ur => ur.AssignedAtUtc)
            .HasColumnName("FechaAsignacion")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(ur => ur.EndedAtUtc)
            .HasColumnName("FechaFinalizacion")
            .HasColumnType("datetime2")
            .IsRequired(false);
    }
}
