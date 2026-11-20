namespace Prepround.Tests.Content;

/// <summary>
/// English translations (ADR 006) added to <see cref="ContentFixture"/> next to their base keys, so reader, validator
/// and validate tests start from the same translated fixture and break one thing in it.
/// </summary>
internal static class TranslatedFixture
{
    public const string FieldTitle = "IT (en)";
    public const string LevelTitle = "Junior (en)";
    public const string DirectionTitle = "Backend (en)";
    public const string StackTitle = ".NET (en)";
    public const string TopicTitle = "Alpha (en)";
    public const string TopicDescription = "The first topic.";
    public const string QuestionText = "A question with one option?";
    public const string FirstOption = "Yes";
    public const string SecondOption = "No";
    public const string Explanation = "Because.";

    /// <summary><c>title_en</c> of the field, of level junior, of direction backend and of stack dotnet.</summary>
    public static void TranslateField(ContentFixture content)
    {
        content.Replace(ContentFixture.FieldFile, "title: IT\n", $"title: IT\ntitle_en: {FieldTitle}\n");
        content.Replace(ContentFixture.FieldFile, "    title: Junior\n", $"    title: Junior\n    title_en: {LevelTitle}\n");
        content.Replace(ContentFixture.FieldFile, "    title: Backend\n", $"    title: Backend\n    title_en: {DirectionTitle}\n");
        content.Replace(ContentFixture.FieldFile, "        title: .NET (C#)\n", $"        title: .NET (C#)\n        title_en: \"{StackTitle}\"\n");
    }

    /// <summary>
    /// <c>title_en</c>, <c>description_en</c> and <c>draft_en: true</c> of topic alpha, plus the full translation of
    /// alpha-001: <c>text_en</c>, <c>explanation_en</c> and <c>text_en</c> of both options. alpha-002 stays untranslated.
    /// </summary>
    public static void TranslateAlphaAndItsFirstQuestion(ContentFixture content)
    {
        content.Replace(ContentFixture.AlphaFile, "title: Alpha\n", $"title: Alpha\ntitle_en: {TopicTitle}\n");
        content.Replace(ContentFixture.AlphaFile, "description: Перша тема.\n",
            $"description: Перша тема.\ndescription_en: {TopicDescription}\ndraft_en: true\n");
        content.Replace(ContentFixture.AlphaFile, "    text: Питання з одним варіантом?\n",
            $"    text: Питання з одним варіантом?\n    text_en: {QuestionText}\n");
        content.Replace(ContentFixture.AlphaFile, "      - text: Так\n", $"      - text: Так\n        text_en: \"{FirstOption}\"\n");
        content.Replace(ContentFixture.AlphaFile, "      - text: Ні\n", $"      - text: Ні\n        text_en: \"{SecondOption}\"\n");
        content.Replace(ContentFixture.AlphaFile, "    explanation: Бо так.", $"    explanation: Бо так.\n    explanation_en: {Explanation}");
    }
}
