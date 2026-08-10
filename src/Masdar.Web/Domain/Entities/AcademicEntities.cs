namespace Masdar.Web.Domain.Entities;

public sealed class University
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoStorageKey { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Major> Majors { get; set; } = [];
    public ICollection<ApplicationUser> Users { get; set; } = [];
}

public sealed class Major
{
    public int Id { get; set; }
    public int UniversityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public University University { get; set; } = null!;
    public ICollection<Course> Courses { get; set; } = [];
    public ICollection<ApplicationUser> Users { get; set; } = [];
}

public sealed class Course
{
    public int Id { get; set; }
    public int MajorId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public DateTime? ExamDateUtc { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Major Major { get; set; } = null!;
    public ICollection<UserCourse> UserCourses { get; set; } = [];
    public ICollection<StudyResource> Resources { get; set; } = [];
    public ICollection<RoadmapNode> RoadmapNodes { get; set; } = [];
    public ICollection<ResourceRequest> ResourceRequests { get; set; } = [];
}

public sealed class UserCourse
{
    public string UserId { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public bool IsActive { get; set; }
    public DateTime SelectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAtUtc { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
