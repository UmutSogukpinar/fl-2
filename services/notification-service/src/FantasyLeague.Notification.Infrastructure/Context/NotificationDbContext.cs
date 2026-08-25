using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Notification.Infrastructure.Context;

public sealed class NotificationDbContext(
    DbContextOptions<NotificationDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(NotificationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
