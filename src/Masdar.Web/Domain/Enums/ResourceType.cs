namespace Masdar.Web.Domain.Enums;

public enum ResourceType
{
    PreviousExam = 1,
    Assignment = 2,
    QuizBank = 3,
    LectureSummary = 4,
    CheatSheet = 5,
    StudentNote = 6,
    RecommendedVideo = 7,
    ExternalResource = 8
}

public enum ResourceSort
{
    Quality = 1,
    MostHelpful = 2,
    Newest = 3,
    Title = 4
}

public enum ResourcePublicationStatus
{
    Draft = 1,
    PendingReview = 2,
    Published = 3,
    Rejected = 4,
    Archived = 5
}

public enum ResourceVerificationStatus
{
    Unverified = 1,
    Verified = 2,
    Rejected = 3
}

public enum FileScanStatus
{
    Pending = 1,
    Validated = 2,
    Infected = 3,
    Failed = 4
}
