namespace Prepround.Domain.Shared;

/// <summary>Expected failure of an operation. Code is "Area.Code" for clients, Message is user-facing (Ukrainian).</summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
