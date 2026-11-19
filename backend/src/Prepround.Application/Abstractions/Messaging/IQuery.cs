namespace Prepround.Application.Abstractions.Messaging;

/// <summary>Read-only request; the type parameter ties it to its handler's response.</summary>
public interface IQuery<TResponse>
{
}
