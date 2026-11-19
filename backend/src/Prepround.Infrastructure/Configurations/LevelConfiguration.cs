using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prepround.Domain.Catalog;

namespace Prepround.Infrastructure.Configurations;

internal sealed class LevelConfiguration : IEntityTypeConfiguration<Level>
{
    public void Configure(EntityTypeBuilder<Level> builder)
    {
        // Ids are unique only within a field (ADR 002, rule 2). Rank is not unique: a deactivated level keeps its old one.
        builder.HasKey(level => new { level.FieldId, level.Id });

        // Rows are never deleted (ADR 002), so no cascade anywhere.
        builder.HasOne<Field>().WithMany().HasForeignKey(level => level.FieldId).OnDelete(DeleteBehavior.Restrict);
    }
}
