namespace Prepround.Application.Content;

/// <summary>
/// Languages of the content (ADR 006): the base one is mandatory and complete, a translation sits next to the base
/// key as <c>key_L</c>. The only place in the backend that names a language, so adding one is a single edit here.
/// Languages are not catalog data: a new one needs an interface dictionary too, which is code anyway.
/// </summary>
public static class ContentLanguages
{
    public const string Base = "uk";

    public static readonly IReadOnlyList<string> Translations = ["en"];

    /// <summary>The base language first, then the translations.</summary>
    public static readonly IReadOnlyList<string> All = [Base, .. Translations];

    /// <summary>The YAML key that holds <paramref name="key"/> in a translation language, such as <c>text_en</c>.</summary>
    public static string TranslationKey(string key, string language) => $"{key}_{language}";
}
