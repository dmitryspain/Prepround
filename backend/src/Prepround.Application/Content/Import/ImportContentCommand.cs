using Prepround.Application.Abstractions.Messaging;

namespace Prepround.Application.Content.Import;

/// <param name="RootDirectory">Existing content root; a missing directory is the caller's usage error, not content.</param>
public sealed record ImportContentCommand(string RootDirectory) : ICommand<ImportSummary>;
