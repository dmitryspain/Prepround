using Microsoft.EntityFrameworkCore;
using Prepround.Tests.Infrastructure;
using static Prepround.Tests.ContentTool.ContentToolProcess;

namespace Prepround.Tests.ContentTool;

/// <summary>import of the stack and topic icons and the stack symbols (PRP-033, rule 13).</summary>
public partial class ImportCommandTests
{
    /// <summary>The card's table, written out independently of field.yaml.</summary>
    private static readonly (string Stack, string Icon, string Symbol)[] CardStacks =
    [
        ("dotnet", "dotnet", "Nt"), ("java", "openjdk", "Jv"), ("python", "python", "Py"),
        ("nodejs", "nodedotjs", "Nd"), ("go", "go", "Go"), ("php", "php", "Ph"),
        ("react", "react", "Re"), ("angular", "angular", "Ng"), ("vue", "vuedotjs", "Vu"),
    ];

    [Fact]
    public async Task Icons_and_symbols_are_imported_changed_in_place_cleared_and_a_repeat_changes_nothing()
    {
        var db = await postgres.CreateDatabaseAsync();
        using var content = new RealContentCopy();
        await ImportOkAsync(db, content.Root);

        var stacks = await StacksAsync(db);
        Assert.Equal(CardStacks.Select(row => (row.Stack, (string?)row.Icon, (string?)row.Symbol)).Order(),
            stacks.Select(stack => (stack.Key, stack.Value.Icon, stack.Value.Symbol)).Order());
        var topics = await TopicIconsAsync(db);
        Assert.Equal("postgresql", topics["sql"]);
        Assert.Null(topics["csharp"]);

        // One topic icon changes, one stack symbol disappears: each row is updated, not re-added.
        content.Replace(SqlFile, "icon: postgresql", "icon: mysql");
        content.Replace(FieldFile, "        symbol: Nt\n", "");
        var changed = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(changed.ExitCode == SuccessExitCode, changed.Output);
        Assert.Equal(Summary(stacks: (0, 1, 0), topics: (0, 1, 0)), changed.StdoutLines);
        Assert.Equal("mysql", (await TopicIconsAsync(db))["sql"]);
        Assert.Equal(("dotnet", (string?)null), (await StacksAsync(db))["dotnet"]);

        var repeat = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(repeat.ExitCode == SuccessExitCode, repeat.Output);
        Assert.Equal(NothingChanged, repeat.StdoutLines);
    }

    private static async Task<Dictionary<string, (string? Icon, string? Symbol)>> StacksAsync(string connectionString)
    {
        await using var context = PostgresFixture.CreateContext(connectionString);
        return await context.Stacks.AsNoTracking().ToDictionaryAsync(
            stack => stack.Id, stack => (stack.Icon, stack.Symbol), TestContext.Current.CancellationToken);
    }

    private static async Task<Dictionary<string, string?>> TopicIconsAsync(string connectionString)
    {
        await using var context = PostgresFixture.CreateContext(connectionString);
        return await context.Topics.AsNoTracking().ToDictionaryAsync(
            topic => topic.Id, topic => topic.Icon, TestContext.Current.CancellationToken);
    }
}
