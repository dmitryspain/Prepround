using Prepround.Application.Abstractions.Messaging;

namespace Prepround.Application.Content.Import;

/// <param name="RootDirectory">Existing content root; a missing directory is the caller's usage error, not content.</param>
/// <param name="IncludeDrafts">
/// Also import the questions of draft topics (ADR 002, "draft topics"). Off by default, so an import that forgets
/// the flag leaves unchecked questions out instead of publishing them (ADR 005, section 6).
/// </param>
public sealed record ImportContentCommand(string RootDirectory, bool IncludeDrafts = false) : ICommand<ImportSummary>;
