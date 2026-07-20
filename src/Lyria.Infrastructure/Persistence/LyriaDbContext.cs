using Lyria.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Lyria.Infrastructure.Persistence;

internal sealed class LyriaDbContext(
    DbContextOptions<LyriaDbContext> options,
    TimeProvider timeProvider) : DbContext(options)
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInformation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LyriaDbContext).Assembly);
    }

    private void ApplyAuditInformation()
    {
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        foreach (EntityEntry<IAuditableEntity> entry
                 in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreatedAtUtc(utcNow);
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditableEntity.CreatedAtUtc))
                    .IsModified = false;

                entry.Entity.SetUpdatedAtUtc(utcNow);
            }
        }
    }
}
