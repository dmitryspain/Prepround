using Microsoft.EntityFrameworkCore;
using Prepround.Domain.Catalog;

namespace Prepround.Infrastructure;

public class PreproundContext : DbContext
{
    public PreproundContext(DbContextOptions<PreproundContext> options)
        : base(options)
    {
    }

    public DbSet<Field> Fields => Set<Field>();

    public DbSet<Level> Levels => Set<Level>();

    public DbSet<Direction> Directions => Set<Direction>();

    public DbSet<Stack> Stacks => Set<Stack>();

    public DbSet<StackTopic> StackTopics => Set<StackTopic>();

    public DbSet<Topic> Topics => Set<Topic>();

    public DbSet<Question> Questions => Set<Question>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PreproundContext).Assembly);
}
