using Prepround.Application.Content;
using Prepround.Infrastructure.Content;

namespace Prepround.Tests.Content;

/// <summary>
/// Rule 13 (PRP-033): the optional <c>icon</c> of a stack and a topic and the optional <c>symbol</c> of a stack,
/// from YAML through the reader and the validator, as <c>validate</c> and <c>import</c> see them.
/// </summary>
public class IconRuleTests
{
    private const string StackTitleLine = "        title: .NET (C#)";
    private const string TopicTitleLine = "title: Alpha";

    private static ContentLoader Loader() => new(new YamlContentReader(), new ContentValidator());

    [Fact]
    public void Absent_icon_and_symbol_are_valid_and_read_as_null()
    {
        using var content = new ContentFixture();

        var result = Loader().Load(content.Root);

        Assert.Empty(result.Errors);
        var field = Assert.Single(result.Fields);
        var stack = Assert.Single(Assert.Single(field.Directions).Stacks);
        Assert.Null(stack.Icon);
        Assert.Null(stack.Symbol);
        Assert.All(field.Topics, topic => Assert.Null(topic.Icon));
    }

    [Fact]
    public void Icon_and_symbol_of_a_stack_and_icon_of_a_topic_are_read_trimmed_and_valid()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.FieldFile, StackTitleLine, $"{StackTitleLine}\n        icon: dotnet\n        symbol: \" Nt \"");
        content.Replace(ContentFixture.AlphaFile, TopicTitleLine, $"{TopicTitleLine}\nicon: postgresql");

        var result = Loader().Load(content.Root);

        Assert.Empty(result.Errors);
        var field = Assert.Single(result.Fields);
        var stack = Assert.Single(Assert.Single(field.Directions).Stacks);
        Assert.Equal(("dotnet", "Nt"), (stack.Icon, stack.Symbol));
        Assert.Equal("postgresql", Assert.Single(field.Topics, topic => topic.Id == "alpha").Icon);
        Assert.Null(Assert.Single(field.Topics, topic => topic.Id == "beta").Icon);
    }

    [Theory]
    [InlineData("Dotnet")]
    [InlineData("dot-net")]
    [InlineData("dot.net")]
    [InlineData("dot_net")]
    [InlineData("\"dot net\"")]
    [InlineData("\"../x\"")]
    [InlineData("дотнет")]
    public void Stack_icon_that_is_not_a_lowercase_slug_is_an_error_of_the_stack(string icon)
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.FieldFile, StackTitleLine, $"{StackTitleLine}\n        icon: {icon}");

        var error = Assert.Single(Loader().Load(content.Root).Errors);

        Assert.Equal((ContentFixture.FieldFile, "dotnet"), (error.File, error.Id));
        Assert.Contains("малих латинських літер і цифр", error.Message);
    }

    [Theory]
    [InlineData("Postgres")]
    [InlineData("postgre-sql")]
    [InlineData("\"url(x)\"")]
    public void Topic_icon_that_is_not_a_lowercase_slug_is_an_error_of_the_topic(string icon)
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, TopicTitleLine, $"{TopicTitleLine}\nicon: {icon}");

        var error = Assert.Single(Loader().Load(content.Root).Errors);

        Assert.Equal((ContentFixture.AlphaFile, "alpha"), (error.File, error.Id));
        Assert.Contains("малих латинських літер і цифр", error.Message);
    }

    [Theory]
    [InlineData(ContentFixture.FieldFile, StackTitleLine, "        ", "dotnet", "icon:")]
    [InlineData(ContentFixture.FieldFile, StackTitleLine, "        ", "dotnet", "icon: ~")]
    [InlineData(ContentFixture.FieldFile, StackTitleLine, "        ", "dotnet", "icon: \"  \"")]
    [InlineData(ContentFixture.AlphaFile, TopicTitleLine, "", "alpha", "icon:")]
    [InlineData(ContentFixture.FieldFile, StackTitleLine, "        ", "dotnet", "symbol:")]
    [InlineData(ContentFixture.FieldFile, StackTitleLine, "        ", "dotnet", "symbol: \" \"")]
    public void Key_given_without_a_value_is_an_error_not_an_absent_key(
        string file, string afterLine, string indent, string ownerId, string line)
    {
        using var content = new ContentFixture();
        content.Replace(file, afterLine, $"{afterLine}\n{indent}{line}");

        var error = Assert.Single(Loader().Load(content.Root).Errors);

        Assert.Equal((file, ownerId), (error.File, error.Id));
        Assert.StartsWith("Порожній", error.Message);
    }

    [Theory]
    [InlineData("N")]
    [InlineData("Nt")]
    [InlineData("C++")]
    [InlineData("42")]
    // Three letters on screen, five UTF-16 units: counted as the user sees them.
    [InlineData("\"Ńt̃x\"")]
    public void Symbol_of_one_to_three_characters_is_valid(string symbol)
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.FieldFile, StackTitleLine, $"{StackTitleLine}\n        symbol: {symbol}");

        Assert.Empty(Loader().Load(content.Root).Errors);
    }

    [Theory]
    [InlineData("Ntxx")]
    [InlineData("true")]
    [InlineData("\"N t д\"")]
    public void Symbol_longer_than_three_characters_is_an_error_of_the_stack(string symbol)
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.FieldFile, StackTitleLine, $"{StackTitleLine}\n        symbol: {symbol}");

        var error = Assert.Single(Loader().Load(content.Root).Errors);

        Assert.Equal((ContentFixture.FieldFile, "dotnet"), (error.File, error.Id));
        Assert.Contains("задовгий", error.Message);
    }

    [Fact]
    public void Symbol_belongs_to_the_stack_only_on_a_topic_it_is_an_unknown_key()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, TopicTitleLine, $"{TopicTitleLine}\nsymbol: Al");

        var error = Assert.Single(Loader().Load(content.Root).Errors);

        Assert.Equal((ContentFixture.AlphaFile, "alpha"), (error.File, error.Id));
        Assert.Contains("«symbol»", error.Message);
    }

    [Fact]
    public void Icon_that_is_a_list_is_a_reader_error_not_a_silent_null()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.FieldFile, StackTitleLine, $"{StackTitleLine}\n        icon: [dotnet]");

        var error = Assert.Single(Loader().Load(content.Root).Errors);

        Assert.Equal((ContentFixture.FieldFile, "dotnet"), (error.File, error.Id));
        Assert.Contains("«icon»", error.Message);
    }

    [Fact]
    public void Bad_icon_on_a_stack_and_on_a_topic_are_both_reported_in_one_pass()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.FieldFile, StackTitleLine, $"{StackTitleLine}\n        icon: Bad\n        symbol: Long");
        content.Replace(ContentFixture.AlphaFile, TopicTitleLine, $"{TopicTitleLine}\nicon: Bad");

        var errors = Loader().Load(content.Root).Errors;

        Assert.Equal(
            [("dotnet", "Icon"), ("dotnet", "Symbol"), ("alpha", "Icon")],
            errors.Select(error => (error.Id, error.Message.Split(' ')[0])).OrderBy(pair => pair.Id == "alpha"));
    }
}
