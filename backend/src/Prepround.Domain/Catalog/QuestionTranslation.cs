namespace Prepround.Domain.Catalog;

/// <summary>
/// The texts of a <see cref="Question"/> in one translation language (ADR 006). Level, type, code and which options
/// are correct live only in the question, so a translation cannot change the answer.
/// </summary>
/// <remarks>
/// Not a <see cref="CatalogEntity"/>: clients keep no id of a translation, so one that is gone from the files is
/// deleted rather than deactivated (ADR 006, section 2).
/// </remarks>
public sealed class QuestionTranslation
{
    public QuestionTranslation(
        string questionId,
        string language,
        string text,
        IReadOnlyList<string> optionTexts,
        string explanation)
    {
        QuestionId = questionId;
        Language = language;
        Text = text;
        OptionTexts = optionTexts;
        Explanation = explanation;
    }

    public string QuestionId { get; private set; }

    public string Language { get; private set; }

    public string Text { get; private set; }

    /// <summary>One text per option of <see cref="Question.Options"/>, in the same file order: the index is the key.</summary>
    public IReadOnlyList<string> OptionTexts { get; private set; }

    public string Explanation { get; private set; }

    /// <summary>Takes the texts of <paramref name="source"/>, built from the files.</summary>
    /// <returns>True when a text changed.</returns>
    public bool UpdateFrom(QuestionTranslation source)
    {
        var changed = Text != source.Text
                      | !OptionTexts.SequenceEqual(source.OptionTexts)
                      | Explanation != source.Explanation;
        Text = source.Text;
        OptionTexts = source.OptionTexts;
        Explanation = source.Explanation;
        return changed;
    }
}
