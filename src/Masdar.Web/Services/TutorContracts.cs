namespace Masdar.Web.Services;

public enum TutorAnswerKind
{
    Explanation,
    WorkedExample,
    Quiz,
    Misconceptions,
    StudyPlan,
    Summary,
    Comparison,
    ScopeRedirect
}

public sealed record TutorCourseContext(
    int Id,
    string Code,
    string Name,
    string Instructor,
    string Semester,
    int PublishedResourceCount);

public sealed record TutorWorkspace(
    TutorCourseContext? ActiveCourse,
    IReadOnlyList<string> Topics,
    IReadOnlyList<string> SuggestedQuestions);

public sealed record TutorAnswerSection(
    string Heading,
    string Content);

public sealed record TutorSourceCitation(
    int Number,
    long ResourceId,
    string Title,
    string ResourceType,
    string Excerpt,
    string MatchReason);

public sealed record TutorExchange(
    TutorAnswerKind Kind,
    string Question,
    string Lead,
    IReadOnlyList<TutorAnswerSection> Sections,
    IReadOnlyList<TutorSourceCitation> Citations,
    IReadOnlyList<string> FollowUpQuestions,
    bool IsOutOfScope = false);

public sealed record TutorAskResult(
    TutorWorkspace Workspace,
    TutorExchange? Exchange,
    bool ContextChanged = false);

public interface ITutorService
{
    Task<TutorWorkspace> GetWorkspaceAsync(string userId, CancellationToken cancellationToken = default);

    Task<TutorAskResult> AskAsync(
        string userId,
        string question,
        int? expectedCourseId,
        CancellationToken cancellationToken = default);
}
