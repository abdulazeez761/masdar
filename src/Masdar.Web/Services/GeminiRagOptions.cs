namespace Masdar.Web.Services;

public sealed class GeminiRagOptions
{
    public const string SectionName = "Rag";

    public string Provider { get; set; } = "Gemini";
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gemini-3.6-flash";
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";
    public int EmbeddingDimensions { get; set; } = 768;
    public int TopK { get; set; } = 6;
    public double MinimumSimilarity { get; set; } = 0.25;
    public int ChunkSize { get; set; } = 1200;
    public int ChunkOverlap { get; set; } = 180;
    public int MaximumCharactersPerResource { get; set; } = 100_000;

    public string ResolveApiKey() => string.IsNullOrWhiteSpace(ApiKey)
        ? Environment.GetEnvironmentVariable("GEMINI_API_KEY")?.Trim() ?? string.Empty
        : ApiKey.Trim();
}

public sealed class RagUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed record RagSourceDocument(
    long ResourceId,
    string Title,
    string ResourceType,
    string Description,
    string? TutorContext,
    IReadOnlyList<string> Tags,
    string StorageKey,
    string ContentType,
    string Sha256);

public sealed record RagTextChunk(
    long ResourceId,
    string Title,
    string ResourceType,
    string Text,
    string Location,
    int Sequence);

public sealed record RagSourceMatch(RagTextChunk Chunk, double Similarity);

public sealed record GroundedRagAnswer(
    bool IsAnswerable,
    string Answer,
    IReadOnlyList<RagSourceMatch> Matches,
    IReadOnlyList<string> FollowUpQuestions);

public interface IGroundedRagService
{
    Task<GroundedRagAnswer> AskAsync(
        TutorCourseContext course,
        string question,
        IReadOnlyList<RagSourceDocument> documents,
        CancellationToken cancellationToken = default);
}

public interface IRagDocumentTextExtractor
{
    Task<IReadOnlyList<RagTextChunk>> ExtractAsync(
        RagSourceDocument document,
        CancellationToken cancellationToken = default);
}
