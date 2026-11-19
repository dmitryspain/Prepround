namespace Prepround.Application.Content;

/// <summary>One content problem, reported to the author as "file: id: message" (ADR 002).</summary>
/// <param name="File">Path relative to the content root, with forward slashes.</param>
/// <param name="Id">Id of the offending entity, or a positional hint when the id itself is missing.</param>
/// <param name="Message">User-facing message in Ukrainian.</param>
public sealed record ContentError(string File, string Id, string Message)
{
    public override string ToString() =>
        string.Join(": ", new[] { File, Id, Message }.Where(part => part.Length > 0));
}
