namespace Masdar.Web.ViewModels;

public sealed class ResourceExplorerViewModel
{
    public string? Search { get; init; }
    public int UniversityId { get; init; }
    public int MajorId { get; init; }
    public IReadOnlyList<int> SelectedCourseIds { get; init; } = [];
    public string CourseIdsQuery => string.Join(',', SelectedCourseIds);
    public int? ActiveCourseId { get; init; }
    public string? Type { get; init; }
    public string? Semester { get; init; }
    public string Sort { get; init; } = "Quality";
    public bool SavedOnly { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalCount { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public IReadOnlyList<AcademicOptionViewModel> Universities { get; init; } = [];
    public IReadOnlyList<AcademicOptionViewModel> Majors { get; init; } = [];
    public IReadOnlyList<ExplorerCourseOptionViewModel> AvailableCourses { get; init; } = [];
    public IReadOnlyList<ExplorerCourseOptionViewModel> SelectedCourses { get; init; } = [];
    public IReadOnlyList<ResourceTypeFilterViewModel> ResourceTypes { get; init; } = [];
    public IReadOnlyList<string> Semesters { get; init; } = [];
    public IReadOnlyList<ExplorerResourceCardViewModel> Resources { get; init; } = [];

    public string ScopeLabel => SelectedCourses.Count switch
    {
        0 => "all courses",
        1 => SelectedCourses[0].Code,
        _ => $"{SelectedCourses.Count} selected courses"
    };
}

public sealed class AcademicOptionViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

public sealed class ExplorerCourseOptionViewModel
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string Instructor { get; init; } = "";
    public bool IsSelected { get; init; }
    public bool IsActive { get; init; }
}

public sealed class ResourceTypeFilterViewModel
{
    public string Value { get; init; } = "";
    public string Label { get; init; } = "";
    public string Icon { get; init; } = "grid";
    public bool IsSelected { get; init; }
}

public sealed class ExplorerResourceCardViewModel
{
    public long Id { get; init; }
    public int CourseId { get; init; }
    public string CourseCode { get; init; } = "";
    public string CourseName { get; init; } = "";
    public string Title { get; init; } = "";
    public string Type { get; init; } = "";
    public string TypeKey { get; init; } = "resource";
    public string Semester { get; init; } = "";
    public string UploaderName { get; init; } = "";
    public double AverageRating { get; init; }
    public int RatingCount { get; init; }
    public int HelpfulCount { get; init; }
    public bool IsVerified { get; init; }
    public bool IsRepresentative { get; init; }
    public bool IsSaved { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public DateTime PublishedAtUtc { get; init; }
    public int QualityPercent => (int)Math.Round(Math.Clamp(AverageRating, 0, 5) / 5d * 100d);
    public string QualityLabel => AverageRating switch
    {
        >= 4.7 => "Excellent",
        >= 4.3 => "Strong",
        > 0 => "Community rated",
        _ => "New"
    };
}
