using System.Text.RegularExpressions;

namespace Prepround.Tests.ContentTool;

/// <summary>
/// Gives a content tree a complete English translation (ADR 006): every <c>title</c>, <c>description</c>,
/// <c>text</c> and <c>explanation</c> gets a <c>key_en</c> sibling with a text of its own, and every topic gets
/// <c>draft_en: true</c>. Works on lines rather than through a YAML serializer, so every base value stays byte for
/// byte as it was; the lines of block scalars (code, folded explanations) are never touched.
/// </summary>
internal static partial class EnglishTranslation
{
    public static void AddTo(string root)
    {
        foreach (var path in Directory.GetFiles(root, "*.yaml", SearchOption.AllDirectories))
        {
            var isTopic = Path.GetFileName(Path.GetDirectoryName(path)) == "topics";
            File.WriteAllText(path, Translate(File.ReadAllText(path), isTopic));
        }
    }

    private static string Translate(string yaml, bool isTopic)
    {
        var output = new List<string>();
        var counter = 0;
        int? blockColumn = null;

        foreach (var line in yaml.Split('\n'))
        {
            // Inside a block scalar until a non-blank line is indented no deeper than the key that opened it.
            if (blockColumn is { } column)
            {
                if (line.Trim().Length == 0 || Indent(line) > column)
                {
                    output.Add(line);
                    continue;
                }

                blockColumn = null;
            }

            var key = TranslatedKey().Match(line);
            if (key.Success)
            {
                var indent = key.Groups["indent"].Value;
                var name = key.Groups["key"].Value;
                // Unique within the file, so translated options never repeat (rule 11).
                var english = $"\"EN {++counter}\"";
                if (key.Groups["dash"].Success)
                {
                    output.Add($"{indent}- {name}_en: {english}");
                    output.Add($"{indent}  {name}:{key.Groups["rest"].Value}");
                }
                else
                {
                    output.Add($"{indent}{name}_en: {english}");
                    output.Add(line);
                }
            }
            else if (isTopic && line.StartsWith("questions:", StringComparison.Ordinal))
            {
                output.Add("draft_en: true");
                output.Add(line);
            }
            else
            {
                output.Add(line);
            }

            if (BlockStart().Match(line) is { Success: true } block)
            {
                blockColumn = block.Groups["indent"].Length + (block.Groups["dash"].Success ? 2 : 0);
            }
        }

        return string.Join('\n', output);
    }

    private static int Indent(string line) => line.Length - line.TrimStart(' ').Length;

    [GeneratedRegex("^(?<indent> *)(?<dash>- )?(?<key>title|description|text|explanation):(?<rest>( .*)?)$")]
    private static partial Regex TranslatedKey();

    [GeneratedRegex(@"^(?<indent> *)(?<dash>- )?[A-Za-z_]+: *[|>][-+0-9]*\s*(#.*)?$")]
    private static partial Regex BlockStart();
}
