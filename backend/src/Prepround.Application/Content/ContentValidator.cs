using System.Text.RegularExpressions;

namespace Prepround.Application.Content;

/// <summary>
/// Rules 1–8 and 10 of ADR 002 over content that has already been read. Collects every problem instead of stopping
/// at the first one, so an author fixes a whole batch of questions in one pass. Never throws on bad content.
/// Rule 9 (unknown keys) belongs to the reader: once YAML becomes this model, unknown keys are gone.
/// </summary>
public sealed partial class ContentValidator
{
    private const string SingleType = "single";
    private const string MultipleType = "multiple";
    private const int MinOptionCount = 2;
    private const int MaxOptionCount = 8;

    public IReadOnlyList<ContentError> Validate(IReadOnlyList<FieldDefinition> fields)
    {
        var errors = new List<ContentError>();

        // Question ids are unique across all fields (rule 2): id → file where it was seen first.
        var questionFiles = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var field in fields)
        {
            ValidateField(field, questionFiles, errors);
        }

        return errors;
    }

    private static void ValidateField(
        FieldDefinition field, Dictionary<string, string> questionFiles, List<ContentError> errors)
    {
        var file = field.File;
        var folder = Path.GetFileName(Path.GetDirectoryName(file)) ?? string.Empty;
        var fieldId = Shown(field.Id, folder);

        CheckId(field.Id, "галузі", file, fieldId, errors);
        CheckTitle(field.Title, "галузі", file, fieldId, errors);
        if (field.Id.Length > 0 && field.Id != folder)
        {
            errors.Add(new ContentError(file, fieldId,
                $"Назва теки галузі «{folder}» не збігається з id «{field.Id}» у field.yaml."));
        }

        var levelIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < field.Levels.Count; i++)
        {
            var level = field.Levels[i];
            var shown = Shown(level.Id, $"рівень №{i + 1}");
            CheckId(level.Id, "рівня", file, shown, errors);
            CheckTitle(level.Title, "рівня", file, shown, errors);
            CheckUnique(levelIds, level.Id, "рівня", file, shown, errors);
        }

        var topicIds = field.Topics.Select(topic => topic.Id).ToHashSet(StringComparer.Ordinal);
        ValidateDirections(field, topicIds, errors);

        var levelScale = string.Join(", ", field.Levels.Select(level => level.Id).Distinct(StringComparer.Ordinal));
        var seenTopicIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var topic in field.Topics)
        {
            ValidateTopic(topic, levelIds, levelScale, seenTopicIds, questionFiles, errors);
        }
    }

    private static void ValidateDirections(FieldDefinition field, HashSet<string> topicIds, List<ContentError> errors)
    {
        var file = field.File;
        var directionIds = new HashSet<string>(StringComparer.Ordinal);
        var stackIds = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < field.Directions.Count; i++)
        {
            var direction = field.Directions[i];
            var directionShown = Shown(direction.Id, $"напрям №{i + 1}");
            CheckId(direction.Id, "напряму", file, directionShown, errors);
            CheckTitle(direction.Title, "напряму", file, directionShown, errors);
            CheckUnique(directionIds, direction.Id, "напряму", file, directionShown, errors);

            for (var j = 0; j < direction.Stacks.Count; j++)
            {
                var stack = direction.Stacks[j];
                var shown = Shown(stack.Id, $"{directionShown}, стек №{j + 1}");
                CheckId(stack.Id, "стеку", file, shown, errors);
                CheckTitle(stack.Title, "стеку", file, shown, errors);
                CheckUnique(stackIds, stack.Id, "стеку", file, shown, errors);

                var stackTopicIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var topicId in stack.TopicIds)
                {
                    if (string.IsNullOrWhiteSpace(topicId))
                    {
                        errors.Add(new ContentError(file, shown, "Порожній id теми в списку topics стеку."));
                    }
                    else if (!stackTopicIds.Add(topicId))
                    {
                        errors.Add(new ContentError(file, shown, $"Тема «{topicId}» повторюється в стеку."));
                    }
                    else if (!topicIds.Contains(topicId))
                    {
                        errors.Add(new ContentError(file, shown,
                            $"Теми «{topicId}» немає в галузі: очікується файл topics/{topicId}.yaml з таким id."));
                    }
                }
            }
        }
    }

    private static void ValidateTopic(
        TopicDefinition topic,
        HashSet<string> levelIds,
        string levelScale,
        HashSet<string> seenTopicIds,
        Dictionary<string, string> questionFiles,
        List<ContentError> errors)
    {
        var file = topic.File;
        var fileStem = Path.GetFileNameWithoutExtension(file);
        var shown = Shown(topic.Id, fileStem);

        CheckId(topic.Id, "теми", file, shown, errors);
        CheckTitle(topic.Title, "теми", file, shown, errors);
        CheckUnique(seenTopicIds, topic.Id, "теми", file, shown, errors);
        if (topic.Id.Length > 0 && topic.Id != fileStem)
        {
            errors.Add(new ContentError(file, shown,
                $"Ім'я файлу «{Path.GetFileName(file)}» не збігається з id теми «{topic.Id}»."));
        }

        for (var i = 0; i < topic.Questions.Count; i++)
        {
            ValidateQuestion(topic.Questions[i], i, file, levelIds, levelScale, questionFiles, errors);
        }
    }

    private static void ValidateQuestion(
        QuestionDefinition question,
        int index,
        string file,
        HashSet<string> levelIds,
        string levelScale,
        Dictionary<string, string> questionFiles,
        List<ContentError> errors)
    {
        var shown = Shown(question.Id, $"питання №{index + 1}");

        // Rules 1 and 2.
        CheckId(question.Id, "питання", file, shown, errors);
        if (question.Id.Length > 0 && !questionFiles.TryAdd(question.Id, file))
        {
            errors.Add(new ContentError(file, shown,
                $"Id питання «{question.Id}» уже використано в {questionFiles[question.Id]}."));
        }

        // Rule 5.
        if (string.IsNullOrWhiteSpace(question.LevelId))
        {
            errors.Add(new ContentError(file, shown, "Не вказано рівень питання (level)."));
        }
        else if (!levelIds.Contains(question.LevelId))
        {
            errors.Add(new ContentError(file, shown,
                $"Рівня «{question.LevelId}» немає в шкалі рівнів галузі. Допустимі рівні: {levelScale}."));
        }

        // Rule 6.
        var correctCount = question.Options.Count(option => option.IsCorrect);
        switch (question.Type)
        {
            case SingleType:
                if (correctCount != 1)
                {
                    errors.Add(new ContentError(file, shown,
                        $"Питання типу single має мати рівно 1 правильний варіант, а позначено {correctCount}."));
                }

                break;
            case MultipleType:
                if (correctCount < 2)
                {
                    errors.Add(new ContentError(file, shown,
                        $"Питання типу multiple має мати щонайменше 2 правильні варіанти, а позначено {correctCount}."));
                }

                if (question.Options.Count - correctCount < 1)
                {
                    errors.Add(new ContentError(file, shown,
                        "Питання типу multiple має мати щонайменше 1 неправильний варіант."));
                }

                break;
            case "":
                errors.Add(new ContentError(file, shown, "Не вказано тип питання (type): single або multiple."));
                break;
            default:
                errors.Add(new ContentError(file, shown,
                    $"Невідомий тип питання «{question.Type}»: допустимі single і multiple."));
                break;
        }

        // Rule 7.
        if (question.Options.Count is < MinOptionCount or > MaxOptionCount)
        {
            errors.Add(new ContentError(file, shown,
                $"Варіантів має бути від {MinOptionCount} до {MaxOptionCount}, а їх {question.Options.Count}."));
        }

        var optionTexts = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < question.Options.Count; i++)
        {
            var text = question.Options[i].Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                errors.Add(new ContentError(file, shown, $"Варіант №{i + 1} порожній."));
            }
            else if (!optionTexts.Add(text.Trim()))
            {
                errors.Add(new ContentError(file, shown, $"Варіант «{text.Trim()}» повторюється."));
            }
        }

        // Rule 8.
        if (string.IsNullOrWhiteSpace(question.Text))
        {
            errors.Add(new ContentError(file, shown, "Порожній текст питання (text)."));
        }

        if (string.IsNullOrWhiteSpace(question.Explanation))
        {
            errors.Add(new ContentError(file, shown, "Порожнє пояснення (explanation)."));
        }

        if (question.Code is { } code)
        {
            if (string.IsNullOrWhiteSpace(code.Source))
            {
                errors.Add(new ContentError(file, shown, "Порожній код (code.source)."));
            }

            if (string.IsNullOrWhiteSpace(code.Lang))
            {
                errors.Add(new ContentError(file, shown, "Не вказано мову коду (code.lang)."));
            }
            else if (!KebabCase().IsMatch(code.Lang))
            {
                errors.Add(new ContentError(file, shown,
                    $"Мова коду «{code.Lang}» має бути в kebab-case: малі латинські літери й цифри, слова через дефіс."));
            }
        }

        // Rule 10: the key is optional, but when present it must hold real links (ADR 004).
        // No network here: whether the pages are alive is checked by the tester, not by validate.
        if (question.Sources is { } sources)
        {
            ValidateSources(sources, file, shown, errors);
        }
    }

    private static void ValidateSources(
        IReadOnlyList<string> sources, string file, string shown, List<ContentError> errors)
    {
        if (sources.Count == 0)
        {
            errors.Add(new ContentError(file, shown,
                "Порожній список джерел (sources): додайте посилання або приберіть ключ."));
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            if (string.IsNullOrWhiteSpace(source))
            {
                errors.Add(new ContentError(file, shown, $"Джерело №{i + 1} порожнє."));
            }
            else if (!IsHttpsUrl(source))
            {
                errors.Add(new ContentError(file, shown,
                    $"Джерело «{source}» має бути абсолютним посиланням https://… без пробілів."));
            }
            else if (!seen.Add(source))
            {
                errors.Add(new ContentError(file, shown, $"Джерело «{source}» повторюється."));
            }
        }
    }

    // Uri alone is not enough: it accepts spaces (escaping them), and on Unix it reads "/docs/18/"
    // as an absolute file URI, which the scheme check then rejects.
    private static bool IsHttpsUrl(string source) =>
        !source.Any(char.IsWhiteSpace)
        && Uri.TryCreate(source, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;

    private static void CheckId(string id, string entity, string file, string shown, List<ContentError> errors)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            errors.Add(new ContentError(file, shown, $"Не вказано id {entity}."));
        }
        else if (!KebabCase().IsMatch(id))
        {
            errors.Add(new ContentError(file, shown,
                $"Id {entity} «{id}» має бути в kebab-case: малі латинські літери й цифри, слова через дефіс."));
        }
    }

    private static void CheckTitle(string title, string entity, string file, string shown, List<ContentError> errors)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(new ContentError(file, shown, $"Не вказано назву {entity} (title)."));
        }
    }

    private static void CheckUnique(
        HashSet<string> seen, string id, string entity, string file, string shown, List<ContentError> errors)
    {
        if (id.Length > 0 && !seen.Add(id))
        {
            errors.Add(new ContentError(file, shown, $"Id {entity} «{id}» повторюється в галузі."));
        }
    }

    /// <summary>Id for the message; a positional hint when the id is missing, so the author still finds the place.</summary>
    private static string Shown(string id, string fallback) => string.IsNullOrWhiteSpace(id) ? fallback : id;

    // \z rather than $: in .NET, $ also matches before a trailing newline, which would let "abc\n" through.
    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*\z")]
    private static partial Regex KebabCase();
}
