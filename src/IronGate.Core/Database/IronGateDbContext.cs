using IronGate.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace IronGate.Core.Database;
public class IronGateDbContext(DbContextOptions<IronGateDbContext> options) : DbContext(options) {
    public DbSet<User> Users => Set<User>();
    public DbSet<HashProfile> HashProfiles => Set<HashProfile>();
    public DbSet<DefenseProfile> DefenseProfiles => Set<DefenseProfile>();
    public DbSet<ExperimentRun> ExperimentRuns => Set<ExperimentRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IronGateDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}