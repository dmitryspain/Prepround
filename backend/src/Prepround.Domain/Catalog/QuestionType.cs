namespace Prepround.Domain.Catalog;

public enum QuestionType
{
    /// <summary>Exactly one correct option.</summary>
    Single,

    /// <summary>At least two correct options; only a fully correct answer counts.</summary>
    Multiple,
}
