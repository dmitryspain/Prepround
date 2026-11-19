using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prepround.Domain.Catalog;

namespace Prepround.Infrastructure.Configurations;

internal sealed class StackTopicConfiguration : IEntityTypeConfiguration<StackTopic>
{
    public void Configure(EntityTypeBuilder<StackTopic> builder)
    {
        builder.HasKey(link => new { link.FieldId, link.StackId, link.TopicId });

        // Both keys share FieldId: a stack can list only topics of its own field.
        builder.HasOne<Stack>()
            .WithMany()
            .HasForeignKey(link => new { link.FieldId, link.StackId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Topic>()
            .WithMany()
            .HasForeignKey(link => new { link.FieldId, link.TopicId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
