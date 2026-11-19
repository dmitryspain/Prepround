using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prepround.Domain.Catalog;

namespace Prepround.Infrastructure.Configurations;

internal sealed class StackConfiguration : IEntityTypeConfiguration<Stack>
{
    public void Configure(EntityTypeBuilder<Stack> builder)
    {
        builder.HasKey(stack => new { stack.FieldId, stack.Id });

        // The composite key keeps a stack inside its direction's field.
        builder.HasOne<Direction>()
            .WithMany()
            .HasForeignKey(stack => new { stack.FieldId, stack.DirectionId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
