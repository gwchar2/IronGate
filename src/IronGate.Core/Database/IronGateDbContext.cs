using IronGate.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace IronGate.Core.Database;

/*
 * This class represents the application's database context.
 */
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
    public DbSet<DbConfigProfile> ConfigProfile => Set<DbConfigProfile>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserHash> UserHashes => Set<UserHash>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
