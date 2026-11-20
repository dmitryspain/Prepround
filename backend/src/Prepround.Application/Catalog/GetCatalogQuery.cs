using Prepround.Application.Abstractions.Messaging;

namespace Prepround.Application.Catalog;

/// <summary>The whole active catalog for the test setup screen (spec 5.1).</summary>
/// <param name="Language"><c>?lang=</c> as sent; missing or empty gives the base language (ADR 006).</param>
public sealed record GetCatalogQuery(string? Language = null) : IQuery<CatalogResponse>;
