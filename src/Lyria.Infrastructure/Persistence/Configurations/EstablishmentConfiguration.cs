using Lyria.Domain.Establishments;
using Lyria.Domain.Establishments.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyria.Infrastructure.Persistence.Configurations;

internal sealed class EstablishmentConfiguration
    : IEntityTypeConfiguration<Establishment>
{
    public void Configure(EntityTypeBuilder<Establishment> builder)
    {
        builder.ToTable("Establecimientos");

        builder.HasKey(e => e.Id)
            .HasName("PK_Establecimientos");

        builder.Property(e => e.Id)
            .HasColumnName("EstablecimientoId")
            .HasConversion(
                id => id.Value,
                value => new EstablishmentId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.CategoryId)
            .HasColumnName("CategoriaId")
            .HasConversion(
                id => id.Value,
                value => new EstablishmentCategoryId(value))
            .IsRequired();

        builder.HasOne<EstablishmentCategory>()
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .HasConstraintName("FK_Establecimientos_CategoriasEstablecimiento_CategoriaId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.CategoryId)
            .HasDatabaseName("IX_Establecimientos_CategoriaId");

        builder.Property(e => e.Name)
            .HasColumnName("Nombre")
            .HasMaxLength(Establishment.NameMaxLength)
            .IsRequired();

        builder.Property(e => e.Slug)
            .HasColumnName("Slug")
            .HasColumnType("varchar(160)")
            .HasMaxLength(Establishment.SlugMaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(e => e.Slug)
            .IsUnique()
            .HasDatabaseName("UX_Establecimientos_Slug");

        builder.Property(e => e.Description)
            .HasColumnName("Descripcion")
            .HasMaxLength(Establishment.DescriptionMaxLength)
            .IsRequired(false);

        builder.Property(e => e.Website)
            .HasColumnName("SitioWeb")
            .HasMaxLength(Establishment.WebsiteMaxLength)
            .IsRequired(false);

        builder.Property(e => e.Instagram)
            .HasColumnName("Instagram")
            .HasMaxLength(Establishment.InstagramMaxLength)
            .IsRequired(false);

        builder.Property(e => e.LogoUrl)
            .HasColumnName("LogoUrl")
            .HasMaxLength(Establishment.LogoUrlMaxLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.ContactEmail)
            .HasColumnName("EmailContacto")
            .HasMaxLength(Establishment.ContactEmailMaxLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.ContactPhone)
            .HasColumnName("TelefonoContacto")
            .HasMaxLength(Establishment.ContactPhoneMaxLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(e => e.IsVerified)
            .HasColumnName("Verificado")
            .IsRequired();

        builder.Property(e => e.VerifiedAtUtc)
            .HasColumnName("FechaVerificacion")
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Property(e => e.IsActive)
            .HasColumnName("Activo")
            .IsRequired();

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Establecimientos_Activo");

        builder.HasIndex(e => e.IsVerified)
            .HasDatabaseName("IX_Establecimientos_Verificado");

        builder.Property(e => e.CreatedAtUtc)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(e => e.UpdatedAtUtc)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Ignore(e => e.DomainEvents);
    }
}
