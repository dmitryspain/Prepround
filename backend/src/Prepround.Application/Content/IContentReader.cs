namespace Prepround.Application.Content;

/// <summary>
/// Turns the content tree of ADR 002 into the content model. An abstraction so that the one read-and-check step
/// (<see cref="ContentLoader"/>) lives in Application without knowing the file format.
/// </summary>
public interface IContentReader
{
    /// <param name="rootDirectory">Existing content root; a missing directory is the caller's usage error, not content.</param>
    /// <returns>Fields and structural errors; problems in the author's files are errors, not exceptions.</returns>
    ContentReadResult Read(string rootDirectory);
}
