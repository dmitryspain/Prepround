using System.Collections.Frozen;
using Prepround.Application.Content;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Prepround.Infrastructure.Content;

/// <summary>
/// Reads <c>&lt;root&gt;/&lt;field&gt;/field.yaml</c> and <c>&lt;root&gt;/&lt;field&gt;/topics/*.yaml</c> into the content model.
/// Walks the YAML node tree instead of using a deserializer, so every unknown key (rule 9 of ADR 002) and every
/// malformed value becomes a <see cref="ContentError"/> with its line, and one bad file does not hide the others.
/// Scalar values are trimmed; code keeps its indentation and loses only trailing whitespace.
/// Translations (ADR 006) are read as <c>key_L</c> next to their base key, for every language of
/// <see cref="ContentLanguages.Translations"/>; whether they are complete is the validator's rule 11.
/// Any other .yaml or .yml file in the tree is an error too: skipping it silently would hide a topic from validate
/// and let import deactivate its questions.
/// </summary>
public sealed class YamlContentReader : IContentReader
{
    private const string FieldFileName = "field.yaml";
    private const string TopicsFolderName = "topics";
    private const string TopicFileExtension = ".yaml";

    private static readonly string[] YamlExtensions = [".yaml", ".yml"];

    private static readonly MappingKeys FieldKeys = new(["id", "title", "levels", "directions"], translated: ["title"]);
    private static readonly MappingKeys LevelKeys = new(["id", "title"], translated: ["title"]);
    private static readonly MappingKeys DirectionKeys = new(["id", "title", "stacks"], translated: ["title"]);
    private static readonly MappingKeys StackKeys =
        new(["id", "title", "icon", "symbol", "topics"], translated: ["title"]);
    private static readonly MappingKeys TopicKeys =
        new(["id", "title", "description", "icon", "draft", "questions"], translated: ["title", "description", "draft"]);
    private static readonly MappingKeys QuestionKeys = new(
        ["id", "level", "type", "text", "code", "options", "explanation", "sources"], translated: ["text", "explanation"]);
    private static readonly MappingKeys CodeKeys = new(["lang", "source"], translated: []);
    private static readonly MappingKeys OptionKeys = new(["text", "correct"], translated: ["text"]);

    /// <param name="rootDirectory">Existing content root; a missing directory is the caller's usage error, not content.</param>
    public ContentReadResult Read(string rootDirectory)
    {
        var errors = new List<ContentError>();
        var fields = new List<FieldDefinition>();

        var fieldDirectories = Directory.GetDirectories(rootDirectory)
            .Where(directory => !IsHidden(directory))
            .Order(StringComparer.Ordinal)
            .ToList();

        // An empty root is almost always a wrong path, and importing it would deactivate everything.
        if (fieldDirectories.Count == 0)
        {
            errors.Add(new ContentError(".", string.Empty,
                "Немає жодної галузі: очікуються теки виду <галузь>/field.yaml."));
        }

        foreach (var directory in fieldDirectories)
        {
            if (ReadField(directory, errors) is { } field)
            {
                fields.Add(field);
            }
        }

        ReportUnreadYaml(rootDirectory, string.Empty, errors);

        return new ContentReadResult(fields, errors);
    }

    /// <summary>
    /// Rule 9 of ADR 002 applied to files: every YAML file outside the layout read above, in any letter case
    /// and at any depth, is reported, so a misnamed or misplaced topic cannot vanish without a word.
    /// </summary>
    private static void ReportUnreadYaml(string directory, string relativeDirectory, List<ContentError> errors)
    {
        var files = Directory.GetFiles(directory)
            .Where(path => !IsHidden(path))
            .Select(path => Path.GetFileName(path))
            .Order(StringComparer.Ordinal);

        foreach (var name in files)
        {
            var file = RelativePath(relativeDirectory, name);
            if (YamlExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase) && !IsReadFile(file))
            {
                errors.Add(new ContentError(file, string.Empty,
                    "Файл не буде прочитано: контент має лежати лише в <галузь>/field.yaml і " +
                    "<галузь>/topics/<id>.yaml (розширення .yaml малими літерами, без підтек)."));
            }
        }

        var subdirectories = Directory.GetDirectories(directory)
            .Where(path => !IsHidden(path))
            .Order(StringComparer.Ordinal);

        foreach (var subdirectory in subdirectories)
        {
            ReportUnreadYaml(subdirectory, RelativePath(relativeDirectory, Path.GetFileName(subdirectory)), errors);
        }
    }

    /// <summary>
    /// Whether <see cref="Read"/> reads this file. Names are compared exactly: on a case-insensitive disk
    /// "Field.yaml" or "Topics/" passes the existence checks above, yet is still reported.
    /// </summary>
    private static bool IsReadFile(string relativeFile) => relativeFile.Split('/') switch
    {
        [_, FieldFileName] => true,
        [_, TopicsFolderName, var name] => Path.GetExtension(name) == TopicFileExtension,
        _ => false,
    };

    private static string RelativePath(string relativeDirectory, string name) =>
        relativeDirectory.Length == 0 ? name : $"{relativeDirectory}/{name}";

    private static FieldDefinition? ReadField(string directory, List<ContentError> errors)
    {
        var folder = Path.GetFileName(directory);
        var file = $"{folder}/{FieldFileName}";

        // Topics are read even when field.yaml is broken, so their own errors surface in the same run.
        var topics = ReadTopics(directory, folder, errors);

        var path = Path.Combine(directory, FieldFileName);
        if (!File.Exists(path))
        {
            errors.Add(new ContentError(file, folder,
                "Немає файлу field.yaml: кожна тека в корені контенту — це галузь."));
            return null;
        }

        var yaml = new YamlFile(file, errors);
        if (yaml.LoadRoot(path, folder) is not { } root)
        {
            return null;
        }

        var map = yaml.Map(root, folder);
        map.RejectUnknownKeys(FieldKeys);

        var levels = map.List("levels")
            .Select((node, index) => yaml.Item(node, "levels", map.Id, $"рівень №{index + 1}"))
            .OfType<MappingReader>()
            .Select(ReadLevel)
            .ToList();

        var directions = map.List("directions")
            .Select((node, index) => yaml.Item(node, "directions", map.Id, $"напрям №{index + 1}"))
            .OfType<MappingReader>()
            .Select(direction => ReadDirection(direction, yaml))
            .ToList();

        return new FieldDefinition(file, map.Text("id"), map.Text("title"), levels, directions, topics)
        {
            TitleTranslations = map.Translations("title"),
        };
    }

    private static LevelDefinition ReadLevel(MappingReader map)
    {
        map.RejectUnknownKeys(LevelKeys);
        return new LevelDefinition(map.Text("id"), map.Text("title"))
        {
            TitleTranslations = map.Translations("title"),
        };
    }

    private static DirectionDefinition ReadDirection(MappingReader map, YamlFile yaml)
    {
        map.RejectUnknownKeys(DirectionKeys);

        var stacks = map.List("stacks")
            .Select((node, index) => yaml.Item(node, "stacks", map.Id, $"{map.Id}, стек №{index + 1}"))
            .OfType<MappingReader>()
            .Select(stack => ReadStack(stack, yaml))
            .ToList();

        return new DirectionDefinition(map.Text("id"), map.Text("title"), stacks)
        {
            TitleTranslations = map.Translations("title"),
        };
    }

    private static StackDefinition ReadStack(MappingReader map, YamlFile yaml)
    {
        map.RejectUnknownKeys(StackKeys);

        var topicIds = map.List("topics")
            .Select(node => yaml.ItemText(node, "topics", map.Id))
            .ToList();

        return new StackDefinition(map.Text("id"), map.Text("title"), topicIds)
        {
            TitleTranslations = map.Translations("title"),
            Icon = map.OptionalText("icon"),
            Symbol = map.OptionalText("symbol"),
        };
    }

    private static List<TopicDefinition> ReadTopics(string fieldDirectory, string folder, List<ContentError> errors)
    {
        var topicsDirectory = Path.Combine(fieldDirectory, TopicsFolderName);
        if (!Directory.Exists(topicsDirectory))
        {
            return [];
        }

        return Directory.GetFiles(topicsDirectory)
            .Where(path => !IsHidden(path) && Path.GetExtension(path) == TopicFileExtension)
            .Order(StringComparer.Ordinal)
            .Select(path => ReadTopic(path, $"{folder}/{TopicsFolderName}/{Path.GetFileName(path)}", errors))
            .OfType<TopicDefinition>()
            .ToList();
    }

    private static TopicDefinition? ReadTopic(string path, string file, List<ContentError> errors)
    {
        var fileStem = Path.GetFileNameWithoutExtension(path);
        var yaml = new YamlFile(file, errors);
        if (yaml.LoadRoot(path, fileStem) is not { } root)
        {
            return null;
        }

        var map = yaml.Map(root, fileStem);
        map.RejectUnknownKeys(TopicKeys);

        // Read before the questions, so a bad value is reported in file order, above any question errors.
        var isDraft = map.Flag("draft");
        var draftLanguages = map.Languages("draft");

        var questions = map.List("questions")
            .Select((node, index) => yaml.Item(node, "questions", map.Id, $"питання №{index + 1}"))
            .OfType<MappingReader>()
            .Select(question => ReadQuestion(question, yaml))
            .ToList();

        return new TopicDefinition(file, map.Text("id"), map.Text("title"), map.Text("description"), questions, isDraft)
        {
            TitleTranslations = map.Translations("title"),
            DescriptionTranslations = map.Translations("description"),
            Icon = map.OptionalText("icon"),
            DraftLanguages = draftLanguages,
        };
    }

    private static QuestionDefinition ReadQuestion(MappingReader map, YamlFile yaml)
    {
        map.RejectUnknownKeys(QuestionKeys);

        CodeBlock? code = null;
        if (map.Has("code"))
        {
            // An empty "code:" still means the author meant a block, so the validator reports it as empty.
            code = map.Mapping("code") is { } codeMap
                ? ReadCode(codeMap)
                : new CodeBlock(string.Empty, string.Empty);
        }

        var options = map.List("options")
            .Select(node => yaml.Item(node, "options", map.Id, map.Id))
            .OfType<MappingReader>()
            .Select(ReadOption)
            .ToList();

        // Null only when the key is absent: "sources:" or "sources: []" reach the validator as an empty list,
        // because the author meant to give a source (rule 10).
        List<string>? sources = null;
        if (map.Has("sources"))
        {
            sources = map.List("sources")
                .Select(node => yaml.ItemText(node, "sources", map.Id))
                .ToList();
        }

        return new QuestionDefinition(
            map.Text("id"),
            map.Text("level"),
            map.Text("type"),
            map.Text("text"),
            code,
            options,
            map.Text("explanation"),
            sources)
        {
            TextTranslations = map.Translations("text"),
            ExplanationTranslations = map.Translations("explanation"),
        };
    }

    private static CodeBlock ReadCode(MappingReader map)
    {
        map.RejectUnknownKeys(CodeKeys);
        return new CodeBlock(map.Text("lang"), map.Code("source"));
    }

    private static OptionDefinition ReadOption(MappingReader map)
    {
        map.RejectUnknownKeys(OptionKeys);
        return new OptionDefinition(map.Text("text"), map.Flag("correct"))
        {
            TextTranslations = map.Translations("text"),
        };
    }

    private static bool IsHidden(string path) => Path.GetFileName(path).StartsWith('.');

    /// <summary>
    /// Keys allowed in one mapping. Each translated key also comes as <c>key_L</c> for every translation language;
    /// those are derived here rather than listed, so a new language needs no edit in the reader.
    /// </summary>
    private sealed class MappingKeys
    {
        private readonly HashSet<string> _allowed;

        public MappingKeys(string[] keys, string[] translated)
        {
            Keys = keys;
            Translated = translated;
            _allowed = new HashSet<string>(keys, StringComparer.Ordinal);
            foreach (var key in translated)
            {
                _allowed.UnionWith(
                    ContentLanguages.Translations.Select(language => ContentLanguages.TranslationKey(key, language)));
            }
        }

        /// <summary>The base keys: listing every translation key too would double each unknown-key message.</summary>
        public string[] Keys { get; }

        /// <summary>Base keys that take a language suffix.</summary>
        public string[] Translated { get; }

        public bool Allows(string key) => _allowed.Contains(key);
    }

    /// <summary>One YAML file: parses it and reports problems in it.</summary>
    private sealed class YamlFile(string file, List<ContentError> errors)
    {
        public YamlMappingNode? LoadRoot(string path, string fallbackId)
        {
            // I/O failures are exceptional and propagate; only the parse below is about the author's text.
            var text = File.ReadAllText(path);
            var parser = new Parser(new StringReader(text));
            var stream = new YamlStream();

            try
            {
                stream.Load(parser);
            }
            catch (YamlException exception)
            {
                Report(fallbackId,
                    $"Синтаксична помилка YAML у рядку {exception.Start.Line}, стовпці {exception.Start.Column}: " +
                    $"{exception.Message}");
                return null;
            }
            catch (InvalidOperationException)
            {
                // YamlDotNet 18 throws this instead of a YamlException for an unclosed "[" or "{".
                Report(fallbackId,
                    $"Синтаксична помилка YAML біля рядка {parser.Current?.Start.Line ?? 1}: " +
                    "перевірте, чи закрито дужки [ ], { } і лапки.");
                return null;
            }

            switch (stream.Documents.Count)
            {
                case 0:
                    Report(fallbackId, "Файл порожній.");
                    return null;
                case > 1:
                    Report(fallbackId, "У файлі кілька YAML-документів (---), а має бути один.");
                    return null;
            }

            if (stream.Documents[0].RootNode is YamlMappingNode root)
            {
                return root;
            }

            Report(fallbackId, "На верхньому рівні файлу мають бути пари «ключ: значення».");
            return null;
        }

        public MappingReader Map(YamlMappingNode node, string fallbackId) => new(node, fallbackId, this);

        /// <summary>A list element that must be a mapping; null (already reported) when it is not.</summary>
        public MappingReader? Item(YamlNode node, string listKey, string ownerId, string fallbackId)
        {
            if (node is YamlMappingNode mapping)
            {
                return Map(mapping, fallbackId);
            }

            Report(ownerId, $"Елемент списку «{listKey}» у рядку {node.Start.Line} має бути набором пар «ключ: значення».");
            return null;
        }

        /// <summary>A list element that must be text, such as a topic id in a stack.</summary>
        public string ItemText(YamlNode node, string listKey, string ownerId)
        {
            if (node is YamlScalarNode scalar)
            {
                return IsNull(scalar) ? string.Empty : (scalar.Value ?? string.Empty).Trim();
            }

            Report(ownerId, $"Елемент списку «{listKey}» у рядку {node.Start.Line} має бути текстом.");
            return string.Empty;
        }

        public void Report(string id, string message) => errors.Add(new ContentError(file, id, message));

        /// <summary>YAML null: an empty plain value, "~" or "null" (core schema), as opposed to a quoted string.</summary>
        public static bool IsNull(YamlScalarNode scalar) =>
            scalar.Style == ScalarStyle.Plain
            && scalar.Value is null or "" or "~" or "null" or "Null" or "NULL";
    }

    /// <summary>Values of one YAML mapping; problems are reported against the entity id once it is known.</summary>
    private sealed class MappingReader
    {
        private readonly YamlMappingNode _node;
        private readonly YamlFile _yaml;
        private readonly string _fallbackId;
        private readonly Dictionary<string, YamlNode> _values = new(StringComparer.Ordinal);

        public MappingReader(YamlMappingNode node, string fallbackId, YamlFile yaml)
        {
            _node = node;
            _fallbackId = fallbackId;
            _yaml = yaml;

            foreach (var (key, value) in node.Children)
            {
                if (key is YamlScalarNode { Value: { } name })
                {
                    _values[name] = value;
                }
            }
        }

        /// <summary>The entity's own id when it is readable, otherwise the positional hint from the caller.</summary>
        public string Id => _values.TryGetValue("id", out var node) && node is YamlScalarNode scalar
                            && !YamlFile.IsNull(scalar) && !string.IsNullOrWhiteSpace(scalar.Value)
            ? scalar.Value.Trim()
            : _fallbackId;

        public void RejectUnknownKeys(MappingKeys allowedKeys)
        {
            foreach (var key in _node.Children.Keys)
            {
                var name = key is YamlScalarNode { Value: { } value } ? value : key.ToString();
                if (allowedKeys.Allows(name))
                {
                    continue;
                }

                // "text_de" or "title_EN": the author meant a translation, so the hint is how one is written.
                var translated = allowedKeys.Translated
                    .FirstOrDefault(baseKey => name.StartsWith($"{baseKey}_", StringComparison.Ordinal));
                var hint = translated is null
                    ? $"Допустимі ключі: {string.Join(", ", allowedKeys.Keys)}."
                    : $"Ключ перекладу пишеться як {translated}_<мова>, мови перекладу: " +
                      $"{string.Join(", ", ContentLanguages.Translations)}.";
                _yaml.Report(Id, $"Невідомий ключ «{name}» у рядку {key.Start.Line}. {hint}");
            }
        }

        public bool Has(string key) => _values.ContainsKey(key);

        public string Text(string key) => Scalar(key)?.Trim() ?? string.Empty;

        /// <summary>
        /// Null only when the key is absent: "icon:" reaches the validator as an empty text, because the author meant
        /// to give a value.
        /// </summary>
        public string? OptionalText(string key) => Has(key) ? Text(key) : null;

        /// <summary>Code keeps its leading indentation; only the trailing newline of a "|" block is dropped.</summary>
        public string Code(string key) => Scalar(key)?.TrimEnd() ?? string.Empty;

        public bool Flag(string key)
        {
            var value = Scalar(key);
            if (value is null)
            {
                return false;
            }

            if (bool.TryParse(value.Trim(), out var flag))
            {
                return flag;
            }

            _yaml.Report(Id, $"Значення ключа «{key}» у рядку {_values[key].Start.Line} має бути true або false.");
            return false;
        }

        /// <summary>
        /// The key's text in every translation language that has <c>key_L</c>. A blank value stays as an empty text,
        /// so the validator reports it instead of the translation silently vanishing.
        /// </summary>
        public TranslatedTexts Translations(string key)
        {
            var texts = ContentLanguages.Translations
                .Where(language => Has(ContentLanguages.TranslationKey(key, language)))
                .ToDictionary(language => language, language => Text(ContentLanguages.TranslationKey(key, language)));

            return texts.Count == 0 ? TranslatedTexts.None : new TranslatedTexts(texts);
        }

        /// <summary>Translation languages whose flag <c>key_L</c> is true; a value other than true or false is reported.</summary>
        public IReadOnlySet<string> Languages(string key) => ContentLanguages.Translations
            .Where(language => Flag(ContentLanguages.TranslationKey(key, language)))
            .ToFrozenSet(StringComparer.Ordinal);

        public IReadOnlyList<YamlNode> List(string key)
        {
            if (!_values.TryGetValue(key, out var node) || node is YamlScalarNode scalar && YamlFile.IsNull(scalar))
            {
                return [];
            }

            if (node is YamlSequenceNode sequence)
            {
                return sequence.Children.ToList();
            }

            _yaml.Report(Id, $"Значення ключа «{key}» у рядку {node.Start.Line} має бути списком.");
            return [];
        }

        public MappingReader? Mapping(string key)
        {
            if (!_values.TryGetValue(key, out var node) || node is YamlScalarNode scalar && YamlFile.IsNull(scalar))
            {
                return null;
            }

            if (node is YamlMappingNode mapping)
            {
                return _yaml.Map(mapping, Id);
            }

            _yaml.Report(Id, $"Значення ключа «{key}» у рядку {node.Start.Line} має бути набором пар «ключ: значення».");
            return null;
        }

        /// <summary>Raw text of a scalar value; null when the key is absent or YAML null.</summary>
        private string? Scalar(string key)
        {
            if (!_values.TryGetValue(key, out var node))
            {
                return null;
            }

            if (node is YamlScalarNode scalar)
            {
                return YamlFile.IsNull(scalar) ? null : scalar.Value;
            }

            _yaml.Report(Id, $"Значення ключа «{key}» у рядку {node.Start.Line} має бути текстом.");
            return null;
        }
    }
}
