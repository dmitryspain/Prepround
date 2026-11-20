using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prepround.Domain.Catalog;

namespace Prepround.Infrastructure.Configurations;

internal sealed class QuestionTranslationConfiguration : IEntityTypeConfiguration<QuestionTranslation>
{
    private static readonly JsonSerializerOptions OptionTextsJson = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<QuestionTranslation> builder)
    {
        // At most one translation of a question per language (ADR 006, section 2).
        builder.HasKey(translation => new { translation.QuestionId, translation.Language });

        // Questions are never deleted, and a translation is deleted on its own, so nothing cascades.
        builder.HasOne<Question>()
            .WithMany()
            .HasForeignKey(translation => translation.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // A jsonb array of strings in the order of Questions.Options; the comparer compares by value, as for Options.
        builder.Property(translation => translation.OptionTexts)
            .HasColumnType("jsonb")
            .HasConversion(
                texts => JsonSerializer.Serialize(texts, OptionTextsJson),
                json => JsonSerializer.Deserialize<List<string>>(json, OptionTextsJson)!,
                new ValueComparer<IReadOnlyList<string>>(
                    (left, right) => left!.SequenceEqual(right!),
                    texts => texts.Aggregate(0, (hash, text) => HashCode.Combine(hash, text)),
                    texts => texts.ToList()));
    }
}
