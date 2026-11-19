namespace Prepround.Domain.Catalog;

/// <summary>A question of a topic. Its id is global, never changes and is never reused (ADR 002).</summary>
public sealed class Question : CatalogEntity
{
    public Question(
        string id,
        string fieldId,
        string topicId,
        string levelId,
        QuestionType type,
        string text,
        string? codeLang,
        string? codeSource,
        IReadOnlyList<QuestionOption> options,
        string explanation,
        DateTimeOffset updatedAt)
    {
        Id = id;
        FieldId = fieldId;
        TopicId = topicId;
        LevelId = levelId;
        Type = type;
        Text = text;
        CodeLang = codeLang;
        CodeSource = codeSource;
        Options = options;
        Explanation = explanation;
        UpdatedAt = updatedAt;
    }

    public string Id { get; private set; }

    public string FieldId { get; private set; }

    public string TopicId { get; private set; }

    public string LevelId { get; private set; }

    public QuestionType Type { get; private set; }

    public string Text { get; private set; }

    /// <summary>Null together with <see cref="CodeSource"/> when the question has no code block.</summary>
    public string? CodeLang { get; private set; }

    public string? CodeSource { get; private set; }

    /// <summary>In file order: the index of an option is its key (spec 5.2).</summary>
    public IReadOnlyList<QuestionOption> Options { get; private set; }

    public string Explanation { get; private set; }

    /// <summary>UTC time of the last change of the content; activation alone does not move it.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Takes the content of <paramref name="source"/>, built from the files, and reactivates the question.
    /// <see cref="UpdatedAt"/> moves to the source's value only when the content differs.
    /// </summary>
    /// <returns>True when the row changed.</returns>
    public bool UpdateFrom(Question source)
    {
        var contentChanged = FieldId != source.FieldId
                             | TopicId != source.TopicId
                             | LevelId != source.LevelId
                             | Type != source.Type
                             | Text != source.Text
                             | CodeLang != source.CodeLang
                             | CodeSource != source.CodeSource
                             | !Options.SequenceEqual(source.Options)
                             | Explanation != source.Explanation;
        var activated = Activate();

        if (contentChanged)
        {
            FieldId = source.FieldId;
            TopicId = source.TopicId;
            LevelId = source.LevelId;
            Type = source.Type;
            Text = source.Text;
            CodeLang = source.CodeLang;
            CodeSource = source.CodeSource;
            Options = source.Options;
            Explanation = source.Explanation;
            UpdatedAt = source.UpdatedAt;
        }

        return contentChanged || activated;
    }
}
