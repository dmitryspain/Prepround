using Prepround.Application.TestGeneration;

namespace Prepround.Tests.TestGeneration;

/// <summary>
/// QuestionPicker on its own (spec 5.2): unseen questions first, excluded ones only to fill up, no duplicates,
/// one shuffle across topics, and every result fixed by the seed of the Random passed in.
/// </summary>
public class QuestionPickerTests
{
    private static readonly int[] Seeds = Enumerable.Range(1, 200).ToArray();

    [Fact]
    public void Prefers_unseen_questions_before_seen_when_the_topic_has_enough_of_them()
    {
        // 12 questions, 8 excluded, 4 asked: the 4 unseen ones on every seed.
        var topic = Topic("alpha", 12, excluded: 8);

        foreach (var seed in Seeds)
        {
            var picked = QuestionPicker.Pick([new TopicPick(topic, 4)], new Random(seed));

            Assert.Equal(Unseen(topic).Order(), picked.Order());
        }
    }

    [Fact]
    public void Fills_up_from_seen_questions_only_with_what_the_unseen_ones_lack()
    {
        // 12 questions, 8 excluded, 6 asked: all 4 unseen plus 2 of the seen ones.
        var topic = Topic("alpha", 12, excluded: 8);
        var seen = Seen(topic).ToHashSet();
        var seenPicked = new HashSet<string>();

        foreach (var seed in Seeds)
        {
            var picked = QuestionPicker.Pick([new TopicPick(topic, 6)], new Random(seed));

            Assert.Equal(6, picked.Count);
            Assert.Subset(picked.ToHashSet(), Unseen(topic).ToHashSet());
            Assert.Equal(2, picked.Count(seen.Contains));
            seenPicked.UnionWith(picked.Where(seen.Contains));
        }

        // The 2 seen ones are a random choice, not always the same two.
        Assert.Equal(seen, seenPicked);
    }

    [Fact]
    public void Takes_a_random_subset_of_unseen_questions_not_always_the_first_ones()
    {
        var topic = Topic("alpha", 12, excluded: 0);
        var everPicked = new HashSet<string>();
        var subsets = new HashSet<string>();

        foreach (var seed in Seeds)
        {
            var picked = QuestionPicker.Pick([new TopicPick(topic, 3)], new Random(seed));
            everPicked.UnionWith(picked);
            subsets.Add(string.Join(",", picked.Order(StringComparer.Ordinal)));
        }

        Assert.Equal(topic.Select(candidate => candidate.Id).ToHashSet(), everPicked);
        Assert.True(subsets.Count > 50, $"Only {subsets.Count} different subsets on {Seeds.Length} seeds.");
    }

    [Fact]
    public void Gives_exactly_the_asked_count_of_every_topic_without_duplicates()
    {
        var alpha = Topic("alpha", 12, excluded: 5);
        var beta = Topic("beta", 7, excluded: 7);
        var gamma = Topic("gamma", 30, excluded: 0);

        foreach (var seed in Seeds)
        {
            var picked = QuestionPicker.Pick(
                [new TopicPick(alpha, 12), new TopicPick(beta, 3), new TopicPick(gamma, 25)],
                new Random(seed));

            Assert.Equal(40, picked.Count);
            Assert.Equal(picked.Count, picked.Distinct().Count());
            Assert.Equal(12, picked.Count(id => id.StartsWith("alpha-", StringComparison.Ordinal)));
            Assert.Equal(3, picked.Count(id => id.StartsWith("beta-", StringComparison.Ordinal)));
            Assert.Equal(25, picked.Count(id => id.StartsWith("gamma-", StringComparison.Ordinal)));
        }
    }

    [Fact]
    public void Same_seed_gives_the_same_test_twice()
    {
        TopicPick[] topics = [new(Topic("alpha", 12, excluded: 8), 6), new(Topic("beta", 12, excluded: 0), 5)];

        foreach (var seed in Seeds)
        {
            var first = QuestionPicker.Pick(topics, new Random(seed));
            var second = QuestionPicker.Pick(topics, new Random(seed));

            Assert.Equal(first, second);
            Assert.Equal(QuestionPicker.ShuffleOptionKeys(5, new Random(seed)), QuestionPicker.ShuffleOptionKeys(5, new Random(seed)));
        }
    }

    [Fact]
    public void Different_seeds_give_different_orders_of_the_same_set()
    {
        TopicPick[] topics = [new(Topic("alpha", 5, excluded: 0), 5), new(Topic("beta", 5, excluded: 0), 5)];
        var expected = topics.SelectMany(topic => topic.Candidates).Select(candidate => candidate.Id).Order().ToList();

        var orders = Seeds
            .Select(seed => QuestionPicker.Pick(topics, new Random(seed)))
            .Select(picked =>
            {
                Assert.Equal(expected, picked.Order());
                return string.Join(",", picked);
            })
            .ToHashSet();

        Assert.True(orders.Count > Seeds.Length / 2, $"Only {orders.Count} different orders on {Seeds.Length} seeds.");
    }

    [Fact]
    public void Questions_of_different_topics_are_mixed_not_grouped_by_topic()
    {
        // Without the shuffle across topics the test would be alpha, alpha, …, beta, beta, … on every seed.
        TopicPick[] topics = [new(Topic("alpha", 5, excluded: 0), 5), new(Topic("beta", 5, excluded: 0), 5)];
        var grouped = 0;
        var betaFirst = 0;

        foreach (var seed in Seeds)
        {
            var picked = QuestionPicker.Pick(topics, new Random(seed));
            var topicsInOrder = picked.Select(id => id[..id.IndexOf('-')]).ToList();
            var switches = topicsInOrder.Zip(topicsInOrder.Skip(1)).Count(pair => pair.First != pair.Second);
            grouped += switches == 1 ? 1 : 0;
            betaFirst += topicsInOrder[0] == "beta" ? 1 : 0;
        }

        // 2 of 252 arrangements are grouped: a few seeds may hit them, most must not.
        Assert.True(grouped < Seeds.Length / 10, $"{grouped} of {Seeds.Length} tests were grouped by topic.");
        Assert.InRange(betaFirst, Seeds.Length / 4, Seeds.Length * 3 / 4);
    }

    [Fact]
    public void Result_does_not_depend_on_the_order_the_candidates_came_in()
    {
        var topic = Topic("alpha", 12, excluded: 8);
        var reversed = topic.AsEnumerable().Reverse().ToList();

        foreach (var seed in Seeds)
        {
            Assert.Equal(
                QuestionPicker.Pick([new TopicPick(topic, 6)], new Random(seed)),
                QuestionPicker.Pick([new TopicPick(reversed, 6)], new Random(seed)));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void Option_keys_are_a_permutation_of_file_indexes(int optionCount)
    {
        foreach (var seed in Seeds)
        {
            var keys = QuestionPicker.ShuffleOptionKeys(optionCount, new Random(seed));

            Assert.Equal(Enumerable.Range(0, optionCount), keys.Order());
        }
    }

    [Fact]
    public void Option_order_changes_with_the_seed_and_the_first_option_is_not_always_first()
    {
        var orders = Seeds.Select(seed => QuestionPicker.ShuffleOptionKeys(4, new Random(seed))).ToList();

        // All 24 orders of 4 options show up on 200 seeds, and key 0 leads only about a quarter of the time.
        Assert.Equal(24, orders.Select(keys => string.Join(",", keys)).Distinct().Count());
        Assert.InRange(orders.Count(keys => keys[0] == 0), Seeds.Length / 8, Seeds.Length * 3 / 8);
    }

    [Fact]
    public void Count_above_the_candidates_of_a_topic_is_a_programming_error()
    {
        Assert.Throws<ArgumentException>(() =>
            QuestionPicker.Pick([new TopicPick(Topic("alpha", 3, excluded: 0), 4)], new Random(1)));
    }

    [Fact]
    public void Same_question_offered_by_two_topics_is_a_programming_error_not_a_duplicate_in_the_test()
    {
        var shared = new PickCandidate("shared-001", false);

        Assert.Throws<ArgumentException>(() => QuestionPicker.Pick(
            [new TopicPick([shared, new PickCandidate("alpha-001", false)], 2), new TopicPick([shared], 1)],
            new Random(1)));
    }

    /// <summary>Ids alpha-001…; the last <paramref name="excluded"/> of them are marked as already seen.</summary>
    private static List<PickCandidate> Topic(string topicId, int size, int excluded) =>
        Enumerable.Range(1, size)
            .Select(index => new PickCandidate($"{topicId}-{index:000}", index > size - excluded))
            .ToList();

    private static IEnumerable<string> Unseen(IEnumerable<PickCandidate> topic) =>
        topic.Where(candidate => !candidate.IsExcluded).Select(candidate => candidate.Id);

    private static IEnumerable<string> Seen(IEnumerable<PickCandidate> topic) =>
        topic.Where(candidate => candidate.IsExcluded).Select(candidate => candidate.Id);
}
