using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Masdar.Web.Services;

public sealed class GeminiGroundedRagService(
    GeminiApiClient gemini,
    IRagDocumentTextExtractor extractor,
    IMemoryCache cache,
    IOptions<GeminiRagOptions> options,
    ILogger<GeminiGroundedRagService> logger) : IGroundedRagService
{
    public async Task<GroundedRagAnswer> AskAsync(
        TutorCourseContext course,
        string question,
        IReadOnlyList<RagSourceDocument> documents,
        CancellationToken cancellationToken = default)
    {
        var indexedChunks = await BuildOrLoadIndexAsync(documents, cancellationToken);
        if (indexedChunks.Count == 0)
        {
            return new GroundedRagAnswer(
                false,
                LocalizeNoEvidence(question),
                [],
                []);
        }

        var questionVector = await gemini.EmbedQuestionAsync(question, cancellationToken);
        var ranked = indexedChunks
            .Select(chunk => new RagSourceMatch(chunk.Chunk, CosineSimilarity(questionVector, chunk.Vector)))
            .OrderByDescending(match => match.Similarity)
            .ToArray();
        var topK = Math.Clamp(options.Value.TopK, 2, 12);
        var minimumSimilarity = Math.Clamp(options.Value.MinimumSimilarity, -1, 1);
        var selected = new List<RagSourceMatch>(topK);
        var selectedPerResource = new Dictionary<long, int>();
        foreach (var match in ranked)
        {
            if (match.Similarity < minimumSimilarity) break;
            selectedPerResource.TryGetValue(match.Chunk.ResourceId, out var sourceCount);
            if (sourceCount >= 2) continue;
            selected.Add(match);
            selectedPerResource[match.Chunk.ResourceId] = sourceCount + 1;
            if (selected.Count == topK) break;
        }

        if (selected.Count == 0)
        {
            return new GroundedRagAnswer(
                false,
                LocalizeNoEvidence(question),
                [],
                []);
        }

        var generated = await gemini.GenerateGroundedAnswerAsync(
            course,
            question,
            selected,
            cancellationToken);
        return new GroundedRagAnswer(
            generated.IsAnswerable,
            generated.Answer,
            selected,
            generated.FollowUpQuestions);
    }

    private async Task<IReadOnlyList<IndexedChunk>> BuildOrLoadIndexAsync(
        IReadOnlyList<RagSourceDocument> documents,
        CancellationToken cancellationToken)
    {
        var indexed = new List<IndexedChunk>();
        var pending = new List<PendingResource>();
        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cacheKey = CacheKey(document);
            if (cache.TryGetValue(cacheKey, out IReadOnlyList<IndexedChunk>? cached) && cached is not null)
            {
                indexed.AddRange(cached);
                continue;
            }

            try
            {
                var chunks = await extractor.ExtractAsync(document, cancellationToken);
                if (chunks.Count > 0) pending.Add(new PendingResource(cacheKey, chunks));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    exception,
                    "RAG skipped unreadable resource {ResourceId} ({Title}).",
                    document.ResourceId,
                    document.Title);
                var metadata = string.Join("\n", new[]
                {
                    document.Title,
                    document.Description,
                    document.TutorContext ?? string.Empty,
                    string.Join(", ", document.Tags)
                }.Where(value => !string.IsNullOrWhiteSpace(value)));
                if (!string.IsNullOrWhiteSpace(metadata))
                {
                    pending.Add(new PendingResource(cacheKey,
                    [
                        new RagTextChunk(
                            document.ResourceId,
                            document.Title,
                            document.ResourceType,
                            metadata,
                            "resource metadata",
                            0)
                    ]));
                }
            }
        }

        var missingChunks = pending.SelectMany(resource => resource.Chunks).ToArray();
        var vectors = await gemini.EmbedDocumentsAsync(
            missingChunks.Select(chunk => $"title: {chunk.Title} | text: {chunk.Text}").ToArray(),
            cancellationToken);
        var vectorIndex = 0;
        foreach (var resource in pending)
        {
            var resourceIndex = resource.Chunks
                .Select(chunk => new IndexedChunk(chunk, vectors[vectorIndex++]))
                .ToArray();
            cache.Set(
                resource.CacheKey,
                (IReadOnlyList<IndexedChunk>)resourceIndex,
                new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(12),
                    Size = null
                });
            indexed.AddRange(resourceIndex);
        }

        return indexed;
    }

    private string CacheKey(RagSourceDocument document) =>
        $"rag:{options.Value.EmbeddingModel}:{options.Value.EmbeddingDimensions}:{document.ResourceId}:{document.Sha256}";

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        if (left.Count == 0 || left.Count != right.Count) return -1;
        double dot = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;
        for (var index = 0; index < left.Count; index++)
        {
            dot += left[index] * right[index];
            leftMagnitude += left[index] * left[index];
            rightMagnitude += right[index] * right[index];
        }

        var denominator = Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude);
        return denominator <= double.Epsilon ? -1 : dot / denominator;
    }

    private static string LocalizeNoEvidence(string question) => question.Any(character =>
        character is >= '\u0600' and <= '\u06ff')
        ? "لم أجد في مواد المساق المنشورة مقاطع كافية للإجابة بثقة. جرّب استخدام مصطلح أدق من المساق أو أضف مصدرًا يغطي الموضوع."
        : "I could not find enough evidence in the published course materials to answer confidently. Try a more specific course term or add a source that covers the topic.";

    private sealed record IndexedChunk(RagTextChunk Chunk, float[] Vector);
    private sealed record PendingResource(string CacheKey, IReadOnlyList<RagTextChunk> Chunks);
}
