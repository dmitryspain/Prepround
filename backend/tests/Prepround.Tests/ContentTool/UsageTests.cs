using static Prepround.Tests.ContentTool.ContentToolProcess;

namespace Prepround.Tests.ContentTool;

/// <summary>Runs the real ContentTool process, because the contract is its exit code (2 = usage error).</summary>
public class UsageTests
{
    private const string NotImplementedMark = "(ще не реалізовано)";

    [Fact]
    public async Task Without_arguments_prints_usage_in_ukrainian_and_exits_with_2()
    {
        var run = await RunAsync();

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Contains("Використання", run.Output);
        Assert.Contains("validate", run.Output);
        Assert.Contains("import", run.Output);
    }

    [Fact]
    public async Task Unknown_command_names_it_prints_usage_and_exits_with_2()
    {
        var run = await RunAsync("frobnicate", "../content");

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Contains("frobnicate", run.Output);
        Assert.Contains("Використання", run.Output);
    }

    [Fact]
    public async Task Usage_lists_validate_and_import_without_any_not_implemented_mark()
    {
        // PRP-004 implemented import: the PRP-003 mark next to it is gone, and no other command has one.
        var run = await RunAsync();

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Single(run.StderrLines, line => line.StartsWith("validate ", StringComparison.Ordinal));
        Assert.Single(run.StderrLines, line => line.StartsWith("import ", StringComparison.Ordinal));
        Assert.DoesNotContain(NotImplementedMark, run.Output);
        Assert.DoesNotContain("не реалізовано", run.Output);
    }

    [Fact]
    public async Task Validate_without_a_directory_is_a_usage_error()
    {
        var run = await RunAsync("validate");

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Contains("Використання", run.Stderr);
        Assert.Empty(run.Stdout);
    }

    [Fact]
    public async Task Validate_with_two_directories_is_a_usage_error()
    {
        var run = await RunAsync("validate", RealContentDirectory(), RealContentDirectory());

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Empty(run.Stdout);
    }

    [Fact]
    public async Task Validate_of_a_missing_directory_names_it_and_exits_with_2()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"prepround-missing-{Guid.NewGuid():N}");

        var run = await RunAsync("validate", missing);

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Contains("Теку контенту не знайдено", run.Stderr);
        Assert.Contains(missing, run.Stderr);
        Assert.Empty(run.Stdout);
    }

    // PRP-004 replaced the PRP-003 "import is not implemented yet" branch: a real import of ../content is covered
    // in ImportCommandTests, and only a wrong number of arguments or a missing folder is a usage error now.
    [Fact]
    public async Task Import_without_a_directory_is_a_usage_error()
    {
        var run = await RunIsolatedAsync(null, null, "import");

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Contains("Команді import потрібна рівно одна тека з контентом.", run.Stderr);
        Assert.Contains("Використання", run.Stderr);
        Assert.DoesNotContain("Невідома команда", run.Output);
        Assert.DoesNotContain("не реалізовано", run.Output);
        Assert.Empty(run.Stdout);
    }

    [Fact]
    public async Task Import_with_two_directories_is_a_usage_error()
    {
        var run = await RunIsolatedAsync(null, null, "import", RealContentDirectory(), RealContentDirectory());

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Contains("Використання", run.Stderr);
        Assert.Empty(run.Stdout);
    }

    [Theory]
    [InlineData("--bogus")]
    [InlineData("-x")]
    [InlineData("--INCLUDE-DRAFTS")]
    [InlineData("--include-draft")]
    public async Task Import_with_an_unknown_flag_after_the_directory_names_it_and_exits_with_2(string flag)
    {
        // PRP-023: only --include-drafts is known; a typo must not run a default import into the database.
        var run = await RunIsolatedAsync(ImportWithoutDatabaseTests.Unreachable, null, "import", RealContentDirectory(), flag);

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Contains($"Невідомий прапорець команди import: {flag}.", run.Stderr);
        Assert.Contains("Використання", run.Stderr);
        Assert.DoesNotContain("Не вдалося підключитися", run.Stderr);
        Assert.Empty(run.Stdout);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Include_drafts_before_the_directory_or_without_one_is_a_usage_error(bool withDirectory)
    {
        string[] args = withDirectory ? ["import", "--include-drafts", RealContentDirectory()] : ["import", "--include-drafts"];

        var run = await RunIsolatedAsync(ImportWithoutDatabaseTests.Unreachable, null, args);

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Contains("Прапорець --include-drafts пишеться після теки", run.Stderr);
        Assert.Empty(run.Stdout);
    }

    [Theory]
    [InlineData("--include-drafts", "extra")]
    [InlineData("--include-drafts", "--include-drafts")]
    public async Task Import_with_anything_after_the_flag_is_a_usage_error(string flag, string extra)
    {
        var run = await RunIsolatedAsync(ImportWithoutDatabaseTests.Unreachable, null, "import", RealContentDirectory(), flag, extra);

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Contains("Команді import потрібна рівно одна тека з контентом.", run.Stderr);
        Assert.Empty(run.Stdout);
    }

    [Fact]
    public async Task Import_with_include_drafts_reaches_the_database_step_like_a_plain_import()
    {
        // The flag is accepted: the run gets past argument parsing and fails only on the unreachable database (3).
        var run = await RunIsolatedAsync(ImportWithoutDatabaseTests.Unreachable, null, "import", RealContentDirectory(), "--include-drafts");

        Assert.True(run.ExitCode == DatabaseErrorExitCode, run.Output);
        Assert.Contains("Не вдалося підключитися до бази даних", run.Stderr);
    }

    [Fact]
    public async Task Usage_describes_the_include_drafts_flag_of_import()
    {
        var run = await RunAsync();

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Single(run.StderrLines, line => line.StartsWith("import <тека> [--include-drafts]", StringComparison.Ordinal));
        Assert.Contains(run.StderrLines, line => line.StartsWith("--include-drafts ", StringComparison.Ordinal) && line.Contains("draft: true", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Import_of_a_missing_directory_names_it_and_exits_with_2_before_asking_for_a_database()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"prepround-missing-{Guid.NewGuid():N}");

        var run = await RunIsolatedAsync(null, null, "import", missing);

        Assert.Equal(UsageErrorExitCode, run.ExitCode);
        Assert.Contains("Теку контенту не знайдено", run.Stderr);
        Assert.Contains(missing, run.Stderr);
        Assert.DoesNotContain("рядок підключення", run.Stderr);
        Assert.Empty(run.Stdout);
    }
}
