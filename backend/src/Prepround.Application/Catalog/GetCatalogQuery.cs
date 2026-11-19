using Prepround.Application.Abstractions.Messaging;

namespace Prepround.Application.Catalog;

/// <summary>The whole active catalog for the test setup screen (spec 5.1).</summary>
public sealed record GetCatalogQuery : IQuery<CatalogResponse>;
