namespace Prepround.Application.Content;

/// <summary>
/// The one "read and check" step of ADR 002. Both validate and import go through it,
/// so the author never sees "all good" from one and a different outcome from the other.
/// </summary>
public sealed class ContentLoader
{
    private readonly IContentReader _reader;
    private readonly ContentValidator _validator;

    public ContentLoader(IContentReader reader, ContentValidator validator)
    {
        _reader = reader;
        _validator = validator;
    }

    /// <param name="rootDirectory">Existing content root; a missing directory is the caller's usage error, not content.</param>
    public ContentLoadResult Load(string rootDirectory)
    {
        var content = _reader.Read(rootDirectory);

        // Rules 1–8 and 10–12 assume every file was read; checking a half-read catalog would only add misleading
        // "topic does not exist" errors on top of the reader error that caused them.
        var errors = content.Errors.Count > 0
            ? content.Errors
            : _validator.Validate(content.Fields);

        return new ContentLoadResult(content.Fields, errors);
    }
}
