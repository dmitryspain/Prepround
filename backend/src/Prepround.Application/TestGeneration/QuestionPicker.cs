namespace Prepround.Application.TestGeneration;

/// <summary>
/// The question choice of spec 5.2, free of EF Core so it is tested on its own. All randomness comes from the
/// <see cref="Random"/> passed in: the API takes <see cref="Random.Shared"/> from DI, a test passes a seeded one.
/// </summary>
public static class QuestionPicker
{
    /// <summary>
    /// Takes <see cref="TopicPick.Count"/> random questions of every topic: those not excluded first, then, when they
    /// run out, excluded ones. The picked questions of all topics are then shuffled together.
    /// </summary>
    /// <returns>Ids of the picked questions in test order.</returns>
    /// <exception cref="ArgumentException">A count is negative or above the topic's candidates, or an id repeats.</exception>
    public static IReadOnlyList<string> Pick(IReadOnlyList<TopicPick> topics, Random random)
    {
        ArgumentNullException.ThrowIfNull(topics);
        ArgumentNullException.ThrowIfNull(random);

        // The handler validates counts and ids come from a primary key, so a violation here is a programming error.
        var candidateIds = new HashSet<string>(StringComparer.Ordinal);
        var picked = new List<string>();

        foreach (var topic in topics)
        {
            if (topic.Count < 0 || topic.Count > topic.Candidates.Count)
            {
                throw new ArgumentException(
                    $"Count {topic.Count} is outside 0..{topic.Candidates.Count}, the candidates of the topic.", nameof(topics));
            }

            foreach (var candidate in topic.Candidates)
            {
                if (!candidateIds.Add(candidate.Id))
                {
                    throw new ArgumentException($"Question '{candidate.Id}' is a candidate more than once.", nameof(topics));
                }
            }

            var fresh = Shuffled(topic.Candidates.Where(candidate => !candidate.IsExcluded), random);
            picked.AddRange(fresh.Take(topic.Count));

            if (fresh.Length < topic.Count)
            {
                var excluded = Shuffled(topic.Candidates.Where(candidate => candidate.IsExcluded), random);
                picked.AddRange(excluded.Take(topic.Count - fresh.Length));
            }
        }

        var test = picked.ToArray();
        random.Shuffle(test);
        return test;
    }

    /// <summary>A random display order of the options of one question.</summary>
    /// <returns>Option keys, i.e. 0-based indexes in the file, in display order.</returns>
    public static IReadOnlyList<int> ShuffleOptionKeys(int optionCount, Random random)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(optionCount);
        ArgumentNullException.ThrowIfNull(random);

        var keys = Enumerable.Range(0, optionCount).ToArray();
        random.Shuffle(keys);
        return keys;
    }

    /// <summary>
    /// Sorted before the shuffle, so the result depends only on the set of candidates and the seed,
    /// not on the order in which the database happened to return them.
    /// </summary>
    private static string[] Shuffled(IEnumerable<PickCandidate> candidates, Random random)
    {
        var ids = candidates.Select(candidate => candidate.Id).Order(StringComparer.Ordinal).ToArray();
        random.Shuffle(ids);
        return ids;
    }
}

/// <param name="Candidates">Questions the topic may give: active and on the selected levels.</param>
/// <param name="Count">How many of them the test takes.</param>
public sealed record TopicPick(IReadOnlyList<PickCandidate> Candidates, int Count);

/// <param name="IsExcluded">Already seen on the device: picked only when the topic runs out of the others.</param>
public sealed record PickCandidate(string Id, bool IsExcluded);
