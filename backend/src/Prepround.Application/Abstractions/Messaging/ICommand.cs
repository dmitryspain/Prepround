namespace Prepround.Application.Abstractions.Messaging;

/// <summary>State-changing request; the type parameter ties it to its handler's response.</summary>
public interface ICommand<TResponse>
{
}
