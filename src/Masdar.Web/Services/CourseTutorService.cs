using System.Text.RegularExpressions;
using Masdar.Web.Data;
using Masdar.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Services;

public sealed partial class CourseTutorService(ApplicationDbContext dbContext) : ITutorService
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

        if (course is null)
        {
            return new TutorWorkspace(null, [], []);
        }

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
        {
            throw new DomainValidationException(
                $"Your question must be between 2 and {MaximumQuestionLength} characters.");
        }

        var workspace = await GetWorkspaceAsync(userId, cancellationToken);
        var course = workspace.ActiveCourse;
        if (course is null)
        {
            return new TutorAskResult(workspace, null);
        }

        // A posted course id is never accepted as the requested subject. It is used only
        // to detect that the active course changed after this form was rendered.
        if (expectedCourseId.HasValue && expectedCourseId.Value != course.Id)
        {
            return new TutorAskResult(workspace, null, ContextChanged: true);
        }

        var documents = await LoadDocumentsAsync(course.Id, cancellationToken);
        if (documents.Count == 0)
        {
            var noSources = new TutorExchange(
                TutorAnswerKind.Summary,
                question,
                $"I can't answer from {course.Code} yet because this course has no published, validated tutor sources.",
                [new TutorAnswerSection(
                    "How to unlock the tutor",
                    "Upload a relevant PDF or DOCX with a clear description and optional tutor context. Once it is published and validated, the tutor can retrieve it and cite it here.")],
                [],
                [],
                IsOutOfScope: false);
            return new TutorAskResult(workspace, noSources);
        }

        var normalizedQuestion = Normalize(question);
        var questionTokens = Tokenize(question);
        var retrievalTokens = questionTokens
            .Where(token => !InstructionTokens.Contains(token) && !token.All(char.IsDigit))
            .ToArray();
        var guide = TutorTopicKnowledge.Find(question, workspace.Topics);
        var ranked = documents
            .Select(document => Rank(document, normalizedQuestion, retrievalTokens, guide))
            .OrderByDescending(result => result.TotalScore)
            .ThenByDescending(result => result.Document.IsRepresentative)
            .ThenByDescending(result => result.Document.IsVerified)
            .ThenByDescending(result => result.Document.AverageRating)
            .ThenBy(result => result.Document.Id)
            .ToArray();

        var isCourseOverviewRequest = IsCourseOverviewRequest(normalizedQuestion, questionTokens);
        var topic = SelectTopic(workspace.Topics, retrievalTokens, guide);
        var hasDirectMatch = guide is not null ||
                             ranked.Any(result => result.RelevanceScore > 0) ||
                             isCourseOverviewRequest;
        if (!hasDirectMatch)
        {
            var namedOtherCourse = await FindNamedOtherCourseAsync(course.Id, normalizedQuestion, cancellationToken);
            var redirectLead = namedOtherCourse is null
                ? $"I couldn't verify that topic in the published materials for your active course, {course.Code} · {course.Name}."
                : $"That question appears to be about {namedOtherCourse}. Your active tutor context is {course.Code} · {course.Name}.";
            var redirect = new TutorExchange(
                TutorAnswerKind.ScopeRedirect,
                question,
                redirectLead,
                [new TutorAnswerSection(
                    "Stay in course context",
                    $"Switch your active course from the Courses page if you meant another subject. For {course.Code}, try one of these grounded topics: {FormatTopicList(workspace.Topics)}.")],
                [],
                workspace.SuggestedQuestions.Take(3).ToArray(),
                IsOutOfScope: true);
            return new TutorAskResult(workspace, redirect);
        }

        if (guide is not null && ranked.All(result => result.RelevanceScore <= 0))
        {
            var noGroundedMatch = new TutorExchange(
                TutorAnswerKind.Summary,
                question,
                $"{guide.Topic} belongs to the {course.Code} roadmap, but no published, validated resource directly matches it yet.",
                [
                    new TutorAnswerSection(
                        "Why I stopped",
                        "I will not attach a high-rated but unrelated course file just to produce an answer. Add or validate a source that covers this topic, then ask again for a grounded explanation."),
                    new TutorAnswerSection(
                        "Safe study workflow",
                        $"Until a source is available, write the learning objective for {guide.Topic}, list the terms you need to define, and ask your instructor which course material should be treated as authoritative. This is general study guidance, not a sourced explanation of the topic.")
                ],
                [],
                workspace.SuggestedQuestions.Take(3).ToArray());
            return new TutorAskResult(workspace, noGroundedMatch);
        }

        var selected = ranked
            .Where(result => result.RelevanceScore > 0 || isCourseOverviewRequest)
            .Take(3)
            .ToArray();
        if (selected.Length == 0)
        {
            var noMatch = new TutorExchange(
                TutorAnswerKind.Summary,
                question,
                $"I found the topic in {course.Code}, but none of the eligible resources matched closely enough to cite.",
                [new TutorAnswerSection(
                    "Try a course term",
                    $"Use one of the exact course topics—{FormatTopicList(workspace.Topics)}—or contribute a resource with a clear description and tutor context. I did not substitute an unrelated citation.")],
                [],
                workspace.SuggestedQuestions.Take(3).ToArray());
            return new TutorAskResult(workspace, noMatch);
        }

        var citations = selected
            .Select((result, index) => ToCitation(result, index + 1, topic))
            .ToArray();
        var kind = DetectKind(normalizedQuestion);
        var sections = BuildAnswerSections(kind, course, topic, guide, citations, workspace.Topics);
        var lead = kind == TutorAnswerKind.Quiz
            ? $"Here is a {course.Code} practice set on {topic}, grounded only in the active course sources below."
            : $"I found {selected.Length} relevant {course.Code} source{(selected.Length == 1 ? string.Empty : "s")} and used them to explain {topic}.";
        var followUps = BuildFollowUps(topic, workspace.Topics);
        var exchange = new TutorExchange(kind, question, lead, sections, citations, followUps);
        return new TutorAskResult(workspace, exchange);
    }

    private async Task<IReadOnlyList<TutorDocument>> LoadDocumentsAsync(
        int courseId,
        CancellationToken cancellationToken)
    {
        // This is the RAG trust boundary: no draft, rejected, unscanned, or other-course
        // material can enter ranking or the generated answer.
        return await dbContext.StudyResources
            .AsNoTracking()
            .Where(resource =>
                resource.CourseId == courseId &&
                resource.PublicationStatus == ResourcePublicationStatus.Published &&
                resource.FileScanStatus == FileScanStatus.Validated &&
                resource.Course.IsActive &&
                resource.Course.Major.IsActive &&
                resource.Course.Major.University.IsActive)
            .Select(resource => new TutorDocument(
                resource.Id,
                resource.Title,
                resource.Type,
                resource.Description,
                resource.TutorContext,
                resource.ResourceTags.Select(link => link.Tag.Name).ToArray(),
                resource.VerificationStatus == ResourceVerificationStatus.Verified,
                resource.IsRepresentative,
                resource.Ratings.Any() ? resource.Ratings.Average(rating => rating.Value) : 0,
                resource.HelpfulVotes.Count))
            .ToListAsync(cancellationToken);
    }

    private async Task<string?> FindNamedOtherCourseAsync(
        int activeCourseId,
        string normalizedQuestion,
        CancellationToken cancellationToken)
    {
        var compactQuestion = normalizedQuestion.Replace(" ", string.Empty, StringComparison.Ordinal);
        var courses = await dbContext.Courses
            .AsNoTracking()
            .Where(course =>
                course.Id != activeCourseId &&
                course.IsActive &&
                course.Major.IsActive &&
                course.Major.University.IsActive)
            .OrderBy(course => course.Code)
            .Select(course => new { course.Code, course.Name })
            .ToListAsync(cancellationToken);

        var match = courses.FirstOrDefault(course =>
            ContainsPhrase(normalizedQuestion, Normalize(course.Name)) ||
            compactQuestion.Contains(
                Normalize(course.Code).Replace(" ", string.Empty, StringComparison.Ordinal),
                StringComparison.Ordinal));
        return match is null ? null : $"{match.Code} · {match.Name}";
    }

    private static RankedDocument Rank(
        TutorDocument document,
        string normalizedQuestion,
        IReadOnlyCollection<string> questionTokens,
        TutorTopicGuide? guide)
    {
        var normalizedTitle = Normalize(document.Title);
        var normalizedTags = document.Tags.Select(Normalize).ToArray();
        var normalizedDescription = Normalize(document.Description);
        var normalizedContext = Normalize(document.TutorContext ?? string.Empty);
        var relevance = 0d;

        foreach (var token in questionTokens)
        {
            if (Tokenize(document.Title).Contains(token, StringComparer.Ordinal)) relevance += 9;
            else if (normalizedTitle.Contains(token, StringComparison.Ordinal)) relevance += 5;
            if (normalizedTags.Any(tag => ContainsPhrase(tag, token))) relevance += 11;
            else if (normalizedTags.Any(tag => tag.Contains(token, StringComparison.Ordinal))) relevance += 6;
            if (normalizedContext.Contains(token, StringComparison.Ordinal)) relevance += 4;
            if (normalizedDescription.Contains(token, StringComparison.Ordinal)) relevance += 3;
        }

        if (questionTokens.Count > 0 &&
            normalizedQuestion.Length > 4 &&
            normalizedTitle.Contains(normalizedQuestion, StringComparison.Ordinal))
        {
            relevance += 14;
        }

        if (guide is not null)
        {
            var normalizedTopic = Normalize(guide.Topic);
            if (normalizedTags.Any(tag => tag.Contains(normalizedTopic, StringComparison.Ordinal)) ||
                normalizedTitle.Contains(normalizedTopic, StringComparison.Ordinal))
            {
                relevance += 24;
            }
        }

        var quality = (document.IsRepresentative ? 3.5 : 0) +
                      (document.IsVerified ? 2.5 : 0) +
                      Math.Min(document.AverageRating, 5) * 0.45 +
                      Math.Log10(document.HelpfulCount + 1);
        return new RankedDocument(document, relevance, relevance + quality);
    }

    private static TutorSourceCitation ToCitation(RankedDocument result, int number, string topic)
    {
        var context = string.IsNullOrWhiteSpace(result.Document.TutorContext)
            ? result.Document.Description
            : $"{result.Document.Description} {result.Document.TutorContext}";
        var matchingTags = result.Document.Tags
            .Where(tag => Normalize(topic).Contains(Normalize(tag), StringComparison.Ordinal) ||
                          Normalize(tag).Contains(Normalize(topic), StringComparison.Ordinal))
            .Take(2)
            .ToArray();
        var reason = matchingTags.Length > 0
            ? $"Matches {string.Join(" and ", matchingTags)}"
            : result.Document.IsRepresentative
                ? "Representative course material"
                : "Highest lexical and quality match";
        return new TutorSourceCitation(
            number,
            result.Document.Id,
            result.Document.Title,
            DisplayResourceType(result.Document.Type),
            Truncate(context, 260),
            reason);
    }

    private static IReadOnlyList<TutorAnswerSection> BuildAnswerSections(
        TutorAnswerKind kind,
        TutorCourseContext course,
        string topic,
        TutorTopicGuide? guide,
        IReadOnlyList<TutorSourceCitation> citations,
        IReadOnlyList<string> courseTopics)
    {
        var citationRange = citations.Count switch
        {
            0 => string.Empty,
            1 => "[1]",
            2 => "[1] [2]",
            _ => "[1] [2] [3]"
        };
        var groundedSummary = citations.Count == 0
            ? $"The active course identifies {topic} as a study topic."
            : $"The retrieved materials connect {topic} to worked examples, review guidance, and exam preparation {citationRange}.";
        var evidenceSection = BuildEvidenceSection(citations);
        var strongestEvidence = citations.FirstOrDefault()?.Excerpt;
        var evidenceConnection = string.IsNullOrWhiteSpace(strongestEvidence)
            ? string.Empty
            : $"\n\nThe strongest retrieved source says: {strongestEvidence} [1]";
        var plainLanguage = (guide?.PlainLanguage ??
            $"{groundedSummary} Start by defining the idea in your own words, identify its inputs and goal, and then trace one representative example from the cited material.") +
            evidenceConnection;
        var steps = guide?.Steps ??
            $"1. Read the definition and learning goal in [1].\n2. Write the key rule or process without copying it.\n3. Follow one example and explain why each step is valid.\n4. Rework the example with one input changed.\n5. Check your result against [1] and note the first step that differed.";
        var example = guide?.Example ??
            $"Use this exam-style frame: “Given a new {topic} scenario, identify the goal, select the relevant method, show each intermediate step, and verify the result.” Use [1] for the method and [2] for a second explanation when available.";
        var misconception = guide?.Misconception ??
            $"A common learning trap is recognizing the name “{topic}” without being able to choose or apply it. Test understanding by explaining when the method does not apply, then verify that boundary against [1].";
        var checkpoint = guide?.Checkpoint ??
            $"Can you explain {topic} in two sentences, solve one example without notes, and justify your check using [1]?";

        return kind switch
        {
            TutorAnswerKind.Quiz =>
            [
                evidenceSection,
                new("5-question quiz",
                    $"1. Define {topic} in your own words and name its purpose.\n2. List the main steps or decision points.\n3. Apply it to a small example and show your reasoning.\n4. Diagnose one incorrect solution and repair its first wrong step.\n5. Explain how {topic} connects to another {course.Code} topic: {courseTopics.Skip(1).FirstOrDefault() ?? course.Name}."),
                new("Answer guide",
                    $"A strong answer should include this core idea: {plainLanguage}\n\nFor process questions, look for: {steps}\n\nSelf-check: {checkpoint} Use the cited resources to verify terminology and course-specific expectations {citationRange}."),
                new("How to use this quiz", "Answer once without notes, mark uncertainty rather than guessing, then correct your work from [1]. Wait a day and retry only the missed questions.")
            ],
            TutorAnswerKind.WorkedExample =>
            [
                evidenceSection,
                new("Concept first", $"{plainLanguage} {citationRange}"),
                new("Worked example", example),
                new("Step-by-step method", steps),
                new("Check your result", checkpoint)
            ],
            TutorAnswerKind.Misconceptions =>
            [
                evidenceSection,
                new("The key distinction", $"{plainLanguage} {citationRange}"),
                new("Common misconception", misconception),
                new("Repair strategy", $"State the goal, choose the rule only after checking its conditions, and verify the final result independently. Use [1] to compare your terminology and [2] for another worked perspective when available."),
                new("Quick diagnostic", checkpoint)
            ],
            TutorAnswerKind.StudyPlan => BuildStudyPlan(course, courseTopics, citations),
            TutorAnswerKind.Summary =>
            [
                evidenceSection,
                new("What matters", $"{plainLanguage} {citationRange}"),
                new("Process to remember", steps),
                new("Example anchor", example),
                new("Before the exam", $"Watch for this trap: {misconception}\n\nMastery check: {checkpoint}")
            ],
            TutorAnswerKind.Comparison =>
            [
                evidenceSection,
                new("Comparison lens", $"For each concept, compare its goal, required inputs, core procedure, result, and failure conditions. For {topic}, begin with: {plainLanguage} {citationRange}"),
                new("Decision checklist", $"1. What problem does each method solve?\n2. What assumptions must hold?\n3. What does one worked step look like?\n4. How can the output be checked?\n5. What mistake would make you choose the wrong one?"),
                new("Apply it", example)
            ],
            _ =>
            [
                evidenceSection,
                new("Plain-language explanation", $"{plainLanguage} {citationRange}"),
                new("Step by step", steps),
                new("Concrete example", example),
                new("Common mistake", misconception),
                new("Mastery check", checkpoint)
            ]
        };
    }

    private static IReadOnlyList<TutorAnswerSection> BuildStudyPlan(
        TutorCourseContext course,
        IReadOnlyList<string> topics,
        IReadOnlyList<TutorSourceCitation> citations)
    {
        var selectedTopics = topics.DefaultIfEmpty(course.Name).Take(4).ToArray();
        string Topic(int index) => selectedTopics[index % selectedTopics.Length];
        var sourceNote = citations.Count > 0
            ? $"Start from this retrieved evidence: {citations[0].Excerpt} [1] Use [2] as a second perspective when available."
            : "Use the published course resources listed in this workspace.";
        return
        [
            BuildEvidenceSection(citations),
            new("7-day plan",
                $"Day 1 — Diagnose: explain {Topic(0)} from memory, then list gaps.\nDay 2 — Learn: study {Topic(0)} and build a one-page concept map.\nDay 3 — Apply: solve guided examples on {Topic(1)} and annotate every step.\nDay 4 — Mix: alternate questions on {Topic(0)} and {Topic(2)} without notes.\nDay 5 — Correct: keep an error log and redo each missed problem.\nDay 6 — Simulate: complete a timed set covering {Topic(1)} and {Topic(3)}.\nDay 7 — Retrieve: teach the hardest idea aloud, then do a short mixed quiz."),
            new("One study block", "Use 10 minutes for recall, 25 minutes for a worked example, 20 minutes for independent practice, and 5 minutes to record the first mistake and its correction."),
            new("Source strategy", sourceNote),
            new("Ready-to-move-on rule", "Move on only when you can explain the topic without notes, solve a fresh question, and check the result with a different method or source.")
        ];
    }

    private static TutorAnswerSection BuildEvidenceSection(IReadOnlyList<TutorSourceCitation> citations)
    {
        var evidence = citations.Count == 0
            ? "No eligible course-source excerpt was retrieved."
            : string.Join(
                "\n\n",
                citations.Select(citation =>
                    $"[{citation.Number}] {citation.Title}: {citation.Excerpt}"));
        return new TutorAnswerSection("What your course sources say", evidence);
    }

    private static TutorAnswerKind DetectKind(string normalizedQuestion)
    {
        if (ContainsAny(normalizedQuestion, "quiz", "questions", "question", "exam", "test", "اختبار", "اسئلة", "أسئلة")) return TutorAnswerKind.Quiz;
        if (ContainsAny(normalizedQuestion, "study plan", "schedule", "revision plan", "prepare", "خطة", "جدول", "ادرس", "أدرس")) return TutorAnswerKind.StudyPlan;
        if (ContainsAny(normalizedQuestion, "mistake", "misconception", "confuse", "wrong", "اخطاء", "أخطاء", "خطأ")) return TutorAnswerKind.Misconceptions;
        if (ContainsAny(normalizedQuestion, "example", "solve", "worked", "step by step", "مثال", "حل", "خطوة")) return TutorAnswerKind.WorkedExample;
        if (ContainsAny(normalizedQuestion, "compare", "difference", " versus ", " vs ", "قارن", "الفرق")) return TutorAnswerKind.Comparison;
        if (ContainsAny(normalizedQuestion, "summary", "summarize", "overview", "ملخص", "لخص")) return TutorAnswerKind.Summary;
        return TutorAnswerKind.Explanation;
    }

    private static string SelectTopic(
        IReadOnlyList<string> topics,
        IReadOnlyCollection<string> questionTokens,
        TutorTopicGuide? guide)
    {
        if (guide is not null)
        {
            return guide.Topic;
        }

        return topics
            .Select(topic => new
            {
                Topic = topic,
                Score = Tokenize(topic).Count(token => questionTokens.Contains(token, StringComparer.Ordinal))
            })
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Topic)
            .FirstOrDefault()?.Topic ?? "the core course concepts";
    }

    private static bool IsCourseOverviewRequest(
        string normalizedQuestion,
        IReadOnlyCollection<string> questionTokens) =>
        questionTokens.Count == 0 ||
        questionTokens.All(token => InstructionTokens.Contains(token) || token.All(char.IsDigit)) ||
        ContainsAny(
            normalizedQuestion,
            "this course",
            "key concept",
            "important idea",
            "most important",
            "course overview",
            "هذا المساق",
            "هذا الكورس",
            "اهم فكرة",
            "أهم فكرة");

    private static IReadOnlyList<string> BuildSuggestions(
        TutorCourseContext course,
        IReadOnlyList<string> topics)
    {
        var primary = topics.FirstOrDefault() ?? course.Name;
        var second = topics.Skip(1).FirstOrDefault() ?? primary;
        var third = topics.Skip(2).FirstOrDefault() ?? primary;
        return
        [
            $"Explain {primary} step by step",
            $"Give me a worked example for {second}",
            $"Create a 5-question quiz on {third}",
            $"What mistakes should I avoid in {primary}?",
            $"Build a 7-day study plan for {primary} in {course.Code}"
        ];
    }

    private static IReadOnlyList<string> BuildFollowUps(string topic, IReadOnlyList<string> topics)
    {
        var nextTopic = topics.FirstOrDefault(candidate =>
            !candidate.Equals(topic, StringComparison.OrdinalIgnoreCase)) ?? topic;
        return
        [
            $"Give me another example for {topic}",
            $"Quiz me on {topic}",
            $"Compare {topic} with {nextTopic}"
        ];
    }

    private static IReadOnlyList<string> Tokenize(string value) => WordPattern()
        .Matches(value.ToLowerInvariant())
        .Select(match => match.Value)
        .Where(token => token.Length > 1 && !StopWords.Contains(token))
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    private static string Normalize(string value) =>
        WhitespacePattern().Replace(NonWordPattern().Replace(value.ToLowerInvariant(), " "), " ").Trim();

    private static bool ContainsPhrase(string text, string phrase) =>
        !string.IsNullOrWhiteSpace(phrase) &&
        (" " + text + " ").Contains(" " + phrase + " ", StringComparison.Ordinal);

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(term => ContainsPhrase(text, Normalize(term)));

    private static string FormatTopicList(IReadOnlyList<string> topics) => topics.Count == 0
        ? "the roadmap and published course resources"
        : string.Join(", ", topics.Take(5));

    private static string Truncate(string value, int maximumLength)
    {
        var text = WhitespacePattern().Replace(value.Trim(), " ");
        return text.Length <= maximumLength
            ? text
            : text[..maximumLength].TrimEnd() + "…";
    }

    private static string DisplayResourceType(ResourceType type) => type switch
    {
        ResourceType.PreviousExam => "Previous exam",
        ResourceType.QuizBank => "Quiz bank",
        ResourceType.LectureSummary => "Lecture summary",
        ResourceType.CheatSheet => "Cheat sheet",
        ResourceType.StudentNote => "Student notes",
        ResourceType.RecommendedVideo => "Video",
        ResourceType.ExternalResource => "External resource",
        _ => type.ToString()
    };

    private static string RequireUserId(string? userId) =>
        string.IsNullOrWhiteSpace(userId)
            ? throw new UnauthorizedAccessException("An authenticated user is required.")
            : userId;

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a", "an", "the", "and", "or", "to", "of", "in", "on", "for", "with", "from", "about",
        "what", "where", "when", "which", "who", "why", "how", "is", "are", "be", "this", "that",
        "please", "can", "could", "would", "me", "my", "course", "explain", "create", "give", "make",
        "build", "simple", "simply", "help", "want", "need", "into", "it", "its", "هل", "ما", "ماذا",
        "كيف", "لماذا", "في", "من", "عن", "على", "الى", "إلى", "هذا", "هذه", "اشرح", "لي", "مع"
    };

    private static readonly HashSet<string> InstructionTokens = new(StringComparer.Ordinal)
    {
        "quiz", "question", "questions", "exam", "test", "practice", "example", "worked", "step", "steps",
        "study", "plan", "schedule", "revision", "prepare", "day", "days", "week", "weekly", "daily",
        "summary", "overview", "mistake", "mistakes",
        "misconception", "misconceptions", "compare", "difference", "guide", "answer", "اسئلة", "أسئلة",
        "اختبار", "مثال", "حل", "خطوة", "خطوات", "خطة", "جدول", "ملخص", "اخطاء", "أخطاء", "قارن"
    };

    private sealed record TutorDocument(
        long Id,
        string Title,
        ResourceType Type,
        string Description,
        string? TutorContext,
        IReadOnlyList<string> Tags,
        bool IsVerified,
        bool IsRepresentative,
        double AverageRating,
        int HelpfulCount);

    private sealed record RankedDocument(
        TutorDocument Document,
        double RelevanceScore,
        double TotalScore);

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();

    [GeneratedRegex(@"[^\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonWordPattern();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespacePattern();
}
