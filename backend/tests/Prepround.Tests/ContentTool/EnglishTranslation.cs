using System.Text.RegularExpressions;

namespace Prepround.Tests.ContentTool;

/// <summary>
/// Gives a content tree a complete English translation (ADR 006): every <c>title</c>, <c>description</c>,
/// <c>text</c> and <c>explanation</c> gets a <c>key_en</c> sibling with a text of its own, and every topic gets
/// <c>draft_en: true</c>. Works on lines rather than through a YAML serializer, so every base value stays byte for
/// byte as it was; the lines of block scalars (code, folded explanations) are never touched.
/// </summary>
/// <remarks>
/// The real content carries English of its own since PRP-030 (topic titles and descriptions), and translation waves
/// keep adding more. <see cref="AddTo"/> therefore replaces it rather than adding a second key, and tests that need
/// the content "as before any translation" start from <see cref="RemoveFrom"/>.
/// </remarks>
internal static partial class EnglishTranslation
{
    public static void AddTo(string root)
    {
        foreach (var path in Directory.GetFiles(root, "*.yaml", SearchOption.AllDirectories))
        {
            var isTopic = Path.GetFileName(Path.GetDirectoryName(path)) == "topics";
            File.WriteAllText(path, Translate(Strip(File.ReadAllText(path)), isTopic));
        }
    }

    /// <summary>
    /// Drops every <c>*_en</c> key (with the lines of its value) and every <c>draft_en</c> from a content tree or a
    /// single file, leaving the base lines byte for byte: what the content looked like before it had any English.
    /// </summary>
    public static void RemoveFrom(string path)
    {
        var files = File.Exists(path) ? [path] : Directory.GetFiles(path, "*.yaml", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            File.WriteAllText(file, Strip(File.ReadAllText(file)));
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

    private static string Strip(string yaml)
    {
        var output = new List<string>();
        int? blockColumn = null;
        int? droppedColumn = null;
        // A dropped "- key_en:" opened its sequence item: the next sibling key takes over the dash.
        string? dashIndent = null;

        // Blank lines after a dropped key belong to its value only if a deeper line follows them.
        var blanks = new List<string>();

        foreach (var line in yaml.Split('\n'))
        {
            // The value of a dropped key: deeper lines (block scalar, continued flow scalar) go with it.
            if (droppedColumn is { } dropped)
            {
                if (line.Trim().Length == 0)
                {
                    blanks.Add(line);
                    continue;
                }

                if (Indent(line) > dropped)
                {
                    blanks.Clear();
                    continue;
                }

                droppedColumn = null;
                output.AddRange(blanks);
                blanks.Clear();
            }

            if (blockColumn is { } column)
            {
                if (line.Trim().Length == 0 || Indent(line) > column)
                {
                    output.Add(line);
                    continue;
                }

                blockColumn = null;
            }

            if (EnglishKey().Match(line) is { Success: true } english)
            {
                var indent = english.Groups["indent"].Value;
                var dash = english.Groups["dash"].Success;
                droppedColumn = indent.Length + (dash ? 2 : 0);
                dashIndent = dash ? indent : dashIndent;
                continue;
            }

            var kept = dashIndent is { } item && Indent(line) == item.Length + 2
                ? $"{item}- {line[(item.Length + 2)..]}"
                : line;
            dashIndent = null;
            output.Add(kept);

            if (BlockStart().Match(kept) is { Success: true } block)
            {
                blockColumn = block.Groups["indent"].Length + (block.Groups["dash"].Success ? 2 : 0);
            }
        }

        output.AddRange(blanks);
        return string.Join('\n', output);
    }

    private static int Indent(string line) => line.Length - line.TrimStart(' ').Length;

    [GeneratedRegex("^(?<indent> *)(?<dash>- )?(?<key>title|description|text|explanation):(?<rest>( .*)?)$")]
    private static partial Regex TranslatedKey();

    [GeneratedRegex("^(?<indent> *)(?<dash>- )?[A-Za-z]+_en:( .*)?$")]
    private static partial Regex EnglishKey();

    [GeneratedRegex(@"^(?<indent> *)(?<dash>- )?[A-Za-z_]+: *[|>][-+0-9]*\s*(#.*)?$")]
    private static partial Regex BlockStart();
}
