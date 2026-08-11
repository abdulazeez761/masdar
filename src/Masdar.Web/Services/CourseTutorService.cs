using System.Text.RegularExpressions;
using Masdar.Web.Data;
using Masdar.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Services;

public sealed partial class CourseTutorService(
    ApplicationDbContext dbContext,
    IGroundedRagService ragService) : ITutorService
{
    private const int MaximumQuestionLength = 800;

    public async Task<TutorWorkspace> GetWorkspaceAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        userId = RequireUserId(userId);
        var course = await dbContext.UserCourses
            .AsNoTracking()
            .Where(selection =>
                selection.UserId == userId &&
                selection.IsActive &&
                selection.User.IsActive &&
                selection.Course.IsActive &&
                selection.Course.Major.IsActive &&
                selection.Course.Major.University.IsActive)
            .OrderByDescending(selection => selection.LastAccessedAtUtc)
            .ThenByDescending(selection => selection.SelectedAtUtc)
            .ThenByDescending(selection => selection.CourseId)
            .Select(selection => new TutorCourseContext(
                selection.CourseId,
                selection.Course.Code,
                selection.Course.Name,
                selection.Course.Instructor,
                selection.Course.Semester,
                selection.Course.Resources.Count(resource =>
                    resource.PublicationStatus == ResourcePublicationStatus.Published &&
                    resource.FileScanStatus == FileScanStatus.Validated)))
            .FirstOrDefaultAsync(cancellationToken);

        if (course is null) return new TutorWorkspace(null, [], []);

        var resourceTopics = await dbContext.ResourceTags
            .AsNoTracking()
            .Where(link =>
                link.Resource.CourseId == course.Id &&
                link.Resource.PublicationStatus == ResourcePublicationStatus.Published &&
                link.Resource.FileScanStatus == FileScanStatus.Validated)
            .Select(link => link.Tag.Name)
            .Distinct()
            .OrderBy(topic => topic)
            .Take(12)
            .ToListAsync(cancellationToken);
        var roadmapTopics = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Where(node => node.CourseId == course.Id && node.IsActive)
            .OrderBy(node => node.Order)
            .Select(node => node.Title)
            .Take(6)
            .ToListAsync(cancellationToken);
        var topics = resourceTopics
            .Concat(roadmapTopics)
            .Where(topic => !string.IsNullOrWhiteSpace(topic))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();

        return new TutorWorkspace(course, topics, BuildSuggestions(course, topics));
    }

    public async Task<TutorAskResult> AskAsync(
        string userId,
        string question,
        int? expectedCourseId,
        CancellationToken cancellationToken = default)
    {
        userId = RequireUserId(userId);
        question = question?.Trim() ?? string.Empty;
        if (question.Length is < 2 or > MaximumQuestionLength)
            throw new DomainValidationException(
                $"Your question must be between 2 and {MaximumQuestionLength} characters.");

        var workspace = await GetWorkspaceAsync(userId, cancellationToken);
        var course = workspace.ActiveCourse;
        if (course is null) return new TutorAskResult(workspace, null);

        // This posted id is only a stale-form guard. Course authorization and context
        // always come from the authenticated user's active UserCourse row above.
        if (expectedCourseId.HasValue && expectedCourseId.Value != course.Id)
            return new TutorAskResult(workspace, null, ContextChanged: true);

        var documents = await LoadDocumentsAsync(course.Id, cancellationToken);
        if (documents.Count == 0)
        {
            var noSources = new TutorExchange(
                TutorAnswerKind.Summary,
                question,
                $"I can't answer from {course.Code} yet because this course has no published, validated tutor sources.",
                [new TutorAnswerSection(
                    "How to unlock the tutor",
                    "Upload a relevant PDF or DOCX. Once it is published and validated, the tutor can index its text, retrieve passages, and cite it here.")],
                [],
                []);
            return new TutorAskResult(workspace, noSources);
        }

        try
        {
            var ragAnswer = await ragService.AskAsync(course, question, documents, cancellationToken);
            var citations = ragAnswer.Matches
                .Select((match, index) => new TutorSourceCitation(
                    index + 1,
                    match.Chunk.ResourceId,
                    match.Chunk.Title,
                    match.Chunk.ResourceType,
                    match.Chunk.Text.Length > 280
                        ? $"{match.Chunk.Text[..277]}..."
                        : match.Chunk.Text,
                    $"Semantic match {match.Similarity:P0} · {match.Chunk.Location}"))
                .ToArray();
            var exchange = new TutorExchange(
                ragAnswer.IsAnswerable ? DetectKind(Normalize(question)) : TutorAnswerKind.ScopeRedirect,
                question,
                ragAnswer.IsAnswerable
                    ? $"Gemini retrieved {citations.Length} relevant passage{(citations.Length == 1 ? string.Empty : "s")} from {course.Code} and generated the answer below."
                    : $"The semantic search did not find enough evidence in {course.Code} to answer safely.",
                [new TutorAnswerSection(
                    ragAnswer.IsAnswerable ? "Grounded AI answer" : "What the course sources support",
                    ragAnswer.Answer)],
                citations,
                ragAnswer.FollowUpQuestions.Count > 0
                    ? ragAnswer.FollowUpQuestions
                    : workspace.SuggestedQuestions.Take(3).ToArray(),
                IsOutOfScope: !ragAnswer.IsAnswerable);
            return new TutorAskResult(workspace, exchange);
        }
        catch (RagUnavailableException exception)
        {
            var unavailable = new TutorExchange(
                TutorAnswerKind.Summary,
                question,
                "The external Gemini RAG service is currently unavailable.",
                [new TutorAnswerSection("RAG status", exception.Message)],
                [],
                workspace.SuggestedQuestions.Take(3).ToArray(),
                IsServiceUnavailable: true);
            return new TutorAskResult(workspace, unavailable);
        }
    }

    private async Task<IReadOnlyList<RagSourceDocument>> LoadDocumentsAsync(
        int courseId,
        CancellationToken cancellationToken)
    {
        // Trust boundary: draft, rejected, unvalidated, inactive, and other-course
        // material is excluded before extraction, embedding, retrieval, and generation.
        return await dbContext.StudyResources
            .AsNoTracking()
            .Where(resource =>
                resource.CourseId == courseId &&
                resource.PublicationStatus == ResourcePublicationStatus.Published &&
                resource.FileScanStatus == FileScanStatus.Validated &&
                resource.Course.IsActive &&
                resource.Course.Major.IsActive &&
                resource.Course.Major.University.IsActive)
            .Select(resource => new RagSourceDocument(
                resource.Id,
                resource.Title,
                resource.Type.ToString(),
                resource.Description,
                resource.TutorContext,
                resource.ResourceTags.Select(link => link.Tag.Name).ToArray(),
                resource.StorageKey,
                resource.ContentType,
                resource.Sha256))
            .ToListAsync(cancellationToken);
    }

    private static TutorAnswerKind DetectKind(string question)
    {
        if (ContainsAny(question, "quiz", "questions", "test", "اختبار", "اسئلة", "أسئلة"))
            return TutorAnswerKind.Quiz;
        if (ContainsAny(question, "example", "worked", "مثال", "حل"))
            return TutorAnswerKind.WorkedExample;
        if (ContainsAny(question, "study plan", "schedule", "خطة", "جدول"))
            return TutorAnswerKind.StudyPlan;
        if (ContainsAny(question, "misconception", "mistake", "خطأ", "اخطاء", "أخطاء"))
            return TutorAnswerKind.Misconceptions;
        if (ContainsAny(question, "compare", "difference", "versus", "قارن", "فرق"))
            return TutorAnswerKind.Comparison;
        if (ContainsAny(question, "summary", "summarize", "overview", "ملخص"))
            return TutorAnswerKind.Summary;
        return TutorAnswerKind.Explanation;
    }

    private static IReadOnlyList<string> BuildSuggestions(
        TutorCourseContext course,
        IReadOnlyList<string> topics)
    {
        var primary = topics.ElementAtOrDefault(0) ?? course.Name;
        var secondary = topics.ElementAtOrDefault(1) ?? primary;
        return
        [
            $"Explain {primary} simply and cite the course sources",
            $"Give me a worked example for {secondary}",
            $"Create a 5-question quiz on {primary} with an answer guide",
            $"Build a 7-day study plan for {course.Code}"
        ];
    }

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(term => text.Contains(term, StringComparison.Ordinal));

    private static string Normalize(string value) =>
        NonWordPattern().Replace(value.ToLowerInvariant(), " ").Trim();

    private static string RequireUserId(string? userId) =>
        string.IsNullOrWhiteSpace(userId)
            ? throw new UnauthorizedAccessException("An authenticated user is required.")
            : userId;

    [GeneratedRegex(@"[^\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonWordPattern();
}
