using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prepround.Domain.Catalog;

namespace Prepround.Infrastructure.Configurations;

internal sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.HasKey(topic => new { topic.FieldId, topic.Id });

        builder.HasOne<Field>().WithMany().HasForeignKey(topic => topic.FieldId).OnDelete(DeleteBehavior.Restrict);
    }
}
