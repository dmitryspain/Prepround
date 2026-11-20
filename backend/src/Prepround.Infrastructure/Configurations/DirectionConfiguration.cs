using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prepround.Domain.Catalog;

namespace Prepround.Infrastructure.Configurations;

internal sealed class DirectionConfiguration : IEntityTypeConfiguration<Direction>
{
    public void Configure(EntityTypeBuilder<Direction> builder)
    {
        builder.HasKey(direction => new { direction.FieldId, direction.Id });

        builder.HasOne<Field>().WithMany().HasForeignKey(direction => direction.FieldId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(direction => direction.TitleTranslations).HasTranslationsColumn();
    }
}
