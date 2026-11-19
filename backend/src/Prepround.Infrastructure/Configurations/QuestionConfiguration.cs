using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prepround.Domain.Catalog;

namespace Prepround.Infrastructure.Configurations;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    // camelCase gives the jsonb shape [{"text": …, "isCorrect": …}] in file order (spec 5.2).
    private static readonly JsonSerializerOptions OptionsJson = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Question> builder)
    {
        // Question ids are global (ADR 002, rule 2), unlike the other catalog ids.
        builder.HasKey(question => question.Id);

        // Composite keys keep the topic and the level inside the question's field.
        builder.HasOne<Topic>()
            .WithMany()
            .HasForeignKey(question => new { question.FieldId, question.TopicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Level>()
            .WithMany()
            .HasForeignKey(question => new { question.FieldId, question.LevelId })
            .OnDelete(DeleteBehavior.Restrict);

        // The same words as in the content files and in the API: single, multiple.
        builder.Property(question => question.Type)
            .HasConversion(
                type => type.ToString().ToLowerInvariant(),
                value => Enum.Parse<QuestionType>(value, true));

        // The comparer compares by value, so replacing the list with an equal one is not a change.
        builder.Property(question => question.Options)
            .HasColumnType("jsonb")
            .HasConversion(
                options => JsonSerializer.Serialize(options, OptionsJson),
                json => JsonSerializer.Deserialize<List<QuestionOption>>(json, OptionsJson)!,
                new ValueComparer<IReadOnlyList<QuestionOption>>(
                    (left, right) => left!.SequenceEqual(right!),
                    options => options.Aggregate(0, (hash, option) => HashCode.Combine(hash, option)),
                    options => options.ToList()));
    }
}
