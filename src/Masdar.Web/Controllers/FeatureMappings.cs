using System.Text.RegularExpressions;
using Masdar.Web.Services;
using Masdar.Web.ViewModels;

namespace Masdar.Web.Controllers;

internal static partial class FeatureMappings
{
    public static CourseSummaryViewModel ToViewModel(this CourseListItem item, bool isActive = false, int progressPercent = 0) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Instructor = item.Instructor,
        Semester = item.Semester,
        ExamDate = item.ExamDateUtc,
        Major = item.MajorName,
        University = item.UniversityName,
        ResourceCount = item.PublishedResourceCount,
        IsActive = isActive,
        ProgressPercent = progressPercent
    };

    public static ResourceCardViewModel ToViewModel(this ResourceListItem item) => new()
    {
        Id = item.Id,
        CourseId = item.CourseId,
        CourseCode = item.CourseCode,
        Title = item.Title,
        Type = DisplayEnum(item.Type),
        Semester = item.Semester,
        UploaderName = item.UploaderName,
        AverageRating = item.AverageRating,
        RatingCount = item.RatingCount,
        HelpfulCount = item.HelpfulCount,
        IsVerified = item.IsVerified,
        IsRepresentative = item.IsRepresentative,
        IsSaved = item.IsSaved,
        Tags = item.Tags
    };

    public static ResourceCardViewModel ToViewModel(this ResourceDetails item) => new()
    {
        Id = item.Id,
        CourseId = item.CourseId,
        CourseCode = item.CourseCode,
        Title = item.Title,
        Type = DisplayEnum(item.Type),
        Semester = item.Semester,
        Description = item.Description,
        UploaderName = item.UploaderName,
        AverageRating = item.AverageRating,
        RatingCount = item.RatingCount,
        HelpfulCount = item.HelpfulCount,
        IsVerified = item.IsVerified,
        IsRepresentative = item.IsRepresentative,
        IsSaved = item.IsSaved,
        IsHelpful = item.IsHelpful,
        UserRating = item.CurrentUserRating,
        Tags = item.Tags
    };

    public static RoadmapNodeViewModel ToViewModel(this RoadmapNodeItem item) => new()
    {
        Id = item.Id,
        SortOrder = item.Order,
        Title = item.Title,
        Description = item.Description,
        Difficulty = DisplayEnum(item.Difficulty),
        WeightPercent = item.ExamWeightPercent,
        IsCompleted = item.IsCompleted
    };

    public static OpportunityViewModel ToViewModel(this OpportunityItem item) => new()
    {
        Id = item.Id,
        Type = DisplayEnum(item.Type),
        Title = item.Title,
        Description = item.Description,
        Location = item.Location ?? "Online",
        Url = item.RegistrationUrl,
        StartsAtUtc = item.StartsAtUtc,
        RegistrationDeadlineUtc = item.RegistrationDeadlineUtc,
        RegistrationCount = item.RegistrationCount,
        IsRegistered = item.IsCurrentUserRegistered
    };

    public static string DisplayEnum<TEnum>(TEnum value) where TEnum : struct, Enum =>
        PascalCaseBoundary().Replace(value.ToString(), " $1");

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex PascalCaseBoundary();
}
