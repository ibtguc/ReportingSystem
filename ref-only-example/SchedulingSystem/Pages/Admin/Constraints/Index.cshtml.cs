using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Services.Constraints;

namespace SchedulingSystem.Pages.Admin.Constraints;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<ConstraintDefinition> HardConstraints { get; set; } = new();
    public List<ConstraintDefinition> SoftConstraints { get; set; } = new();
    public Dictionary<string, List<ConstraintDefinition>> HardConstraintsByCategory { get; set; } = new();
    public Dictionary<string, List<ConstraintDefinition>> SoftConstraintsByCategory { get; set; } = new();
    public List<SpecialCaseInfo> SpecialCases { get; set; } = new();
    public List<TeacherConstraintValue> TeacherConstraintValues { get; set; } = new();
    public List<ClassConstraintValue> ClassConstraintValues { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Get all constraints
        HardConstraints = ConstraintDefinitions.GetHardConstraints();
        SoftConstraints = ConstraintDefinitions.GetSoftConstraints();

        // Group by category
        HardConstraintsByCategory = HardConstraints
            .GroupBy(c => c.Category.ToString())
            .ToDictionary(g => g.Key, g => g.ToList());

        SoftConstraintsByCategory = SoftConstraints
            .GroupBy(c => c.Category.ToString())
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build special cases information
        SpecialCases = new List<SpecialCaseInfo>
        {
            new()
            {
                Name = ConstraintDefinitions.SpecialCases.InternTeacherName,
                Type = "Teacher",
                Description = "Placeholder for intern/temporary teachers. Conflicts not enforced.",
                ExemptConstraints = GetExemptConstraints(ConstraintDefinitions.SpecialCases.InternTeacherName)
            },
            new()
            {
                Name = ConstraintDefinitions.SpecialCases.ReserveClassName,
                Type = "Class",
                Description = "Reserve class used for substitution planning. Conflicts not enforced.",
                ExemptConstraints = GetExemptConstraints(ConstraintDefinitions.SpecialCases.ReserveClassName)
            },
            new()
            {
                Name = ConstraintDefinitions.SpecialCases.TeamClassName,
                Type = "Class",
                Description = "Team class exempt from double-booking constraints.",
                ExemptConstraints = GetExemptConstraints(ConstraintDefinitions.SpecialCases.TeamClassName)
            },
            new()
            {
                Name = ConstraintDefinitions.SpecialCases.TeamRoomName,
                Type = "Room",
                Description = "Team room exempt from double-booking constraints.",
                ExemptConstraints = GetExemptConstraints(ConstraintDefinitions.SpecialCases.TeamRoomName)
            }
        };

        // Load actual constraint values from database
        TeacherConstraintValues = await _context.Teachers
            .Where(t => t.MaxConsecutivePeriods.HasValue ||
                       t.MaxPeriodsPerDay.HasValue ||
                       t.MinPeriodsPerDay.HasValue ||
                       t.MinLunchBreak.HasValue)
            .OrderBy(t => t.FirstName)
            .ThenBy(t => t.LastName)
            .Select(t => new TeacherConstraintValue
            {
                Name = !string.IsNullOrEmpty(t.LastName)
                    ? (t.FirstName + " " + t.LastName)
                    : t.FirstName,
                MaxConsecutivePeriods = t.MaxConsecutivePeriods,
                MaxPeriodsPerDay = t.MaxPeriodsPerDay,
                MinPeriodsPerDay = t.MinPeriodsPerDay,
                MinLunchBreak = t.MinLunchBreak
            })
            .ToListAsync();

        ClassConstraintValues = await _context.Classes
            .Where(c => c.MaxConsecutiveSubjects.HasValue ||
                       c.MaxPeriodsPerDay.HasValue ||
                       c.MinPeriodsPerDay.HasValue ||
                       c.MinLunchBreak.HasValue)
            .Select(c => new ClassConstraintValue
            {
                Name = c.Name,
                MaxConsecutiveSubjects = c.MaxConsecutiveSubjects,
                MaxPeriodsPerDay = c.MaxPeriodsPerDay,
                MinPeriodsPerDay = c.MinPeriodsPerDay,
                MinLunchBreak = c.MinLunchBreak
            })
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    private List<string> GetExemptConstraints(string entityName)
    {
        var allConstraints = ConstraintDefinitions.GetAllConstraints();
        return allConstraints
            .Where(c => c.ExemptEntities.Contains(entityName, StringComparer.OrdinalIgnoreCase))
            .Select(c => c.Code)
            .ToList();
    }
}

public class SpecialCaseInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ExemptConstraints { get; set; } = new();
}

public class TeacherConstraintValue
{
    public string Name { get; set; } = string.Empty;
    public int? MaxConsecutivePeriods { get; set; }
    public int? MaxPeriodsPerDay { get; set; }
    public int? MinPeriodsPerDay { get; set; }
    public int? MinLunchBreak { get; set; }
}

public class ClassConstraintValue
{
    public string Name { get; set; } = string.Empty;
    public int? MaxConsecutiveSubjects { get; set; }
    public int? MaxPeriodsPerDay { get; set; }
    public int? MinPeriodsPerDay { get; set; }
    public int? MinLunchBreak { get; set; }
}
