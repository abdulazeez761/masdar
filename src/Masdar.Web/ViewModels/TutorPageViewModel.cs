using System.ComponentModel.DataAnnotations;
using Masdar.Web.Services;

namespace Masdar.Web.ViewModels;

public sealed class TutorPageViewModel
{
    [Required(ErrorMessage = "Ask a question about your active course.")]
    [StringLength(800, MinimumLength = 2, ErrorMessage = "Your question must be between 2 and 800 characters.")]
    public string Question { get; set; } = string.Empty;

    // This is a stale-form guard, not a course selector. The server always resolves
    // course context from the authenticated user's active UserCourse row.
    public int? ContextCourseId { get; set; }

    public TutorWorkspace Workspace { get; set; } = new(null, [], []);
    public TutorExchange? Exchange { get; set; }
    public bool ContextChanged { get; set; }
}
