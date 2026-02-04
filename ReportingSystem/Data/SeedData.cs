using Microsoft.EntityFrameworkCore;
using ReportingSystem.Models;

namespace ReportingSystem.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Seed organizational units if none exist
        if (!await context.OrganizationalUnits.AnyAsync())
        {
            await SeedOrganizationalUnitsAsync(context);
        }
    }

    private static async Task SeedOrganizationalUnitsAsync(ApplicationDbContext context)
    {
        // Root organization
        var root = new OrganizationalUnit
        {
            Name = "German University in Cairo",
            Code = "GUC",
            Level = OrgUnitLevel.Root,
            SortOrder = 0,
            Description = "Root organization"
        };
        context.OrganizationalUnits.Add(root);
        await context.SaveChangesAsync();

        // Campuses
        var mainCampus = new OrganizationalUnit
        {
            Name = "Main Campus",
            Code = "GUC-MC",
            Level = OrgUnitLevel.Campus,
            ParentId = root.Id,
            SortOrder = 1
        };
        var newCampus = new OrganizationalUnit
        {
            Name = "New Campus",
            Code = "GUC-NC",
            Level = OrgUnitLevel.Campus,
            ParentId = root.Id,
            SortOrder = 2
        };
        context.OrganizationalUnits.AddRange(mainCampus, newCampus);
        await context.SaveChangesAsync();

        // Faculties under Main Campus
        var facEngineering = new OrganizationalUnit
        {
            Name = "Faculty of Engineering",
            Code = "ENG",
            Level = OrgUnitLevel.Faculty,
            ParentId = mainCampus.Id,
            SortOrder = 1
        };
        var facMET = new OrganizationalUnit
        {
            Name = "Faculty of Management, Economics & Technology",
            Code = "MET",
            Level = OrgUnitLevel.Faculty,
            ParentId = mainCampus.Id,
            SortOrder = 2
        };
        var facITAdmin = new OrganizationalUnit
        {
            Name = "IT & Administration",
            Code = "ITA",
            Level = OrgUnitLevel.Faculty,
            ParentId = mainCampus.Id,
            SortOrder = 3
        };
        context.OrganizationalUnits.AddRange(facEngineering, facMET, facITAdmin);
        await context.SaveChangesAsync();

        // Departments under Engineering
        var deptCS = new OrganizationalUnit
        {
            Name = "Computer Science & Engineering",
            Code = "CS",
            Level = OrgUnitLevel.Department,
            ParentId = facEngineering.Id,
            SortOrder = 1
        };
        var deptMech = new OrganizationalUnit
        {
            Name = "Mechatronics Engineering",
            Code = "MECH",
            Level = OrgUnitLevel.Department,
            ParentId = facEngineering.Id,
            SortOrder = 2
        };
        context.OrganizationalUnits.AddRange(deptCS, deptMech);
        await context.SaveChangesAsync();

        // Departments under IT & Admin
        var deptSoftware = new OrganizationalUnit
        {
            Name = "Software Development",
            Code = "SWDEV",
            Level = OrgUnitLevel.Department,
            ParentId = facITAdmin.Id,
            SortOrder = 1
        };
        context.OrganizationalUnits.Add(deptSoftware);
        await context.SaveChangesAsync();

        // Teams under Software Development
        var teamBackend = new OrganizationalUnit
        {
            Name = "Backend Team",
            Code = "SWDEV-BE",
            Level = OrgUnitLevel.Team,
            ParentId = deptSoftware.Id,
            SortOrder = 1
        };
        var teamFrontend = new OrganizationalUnit
        {
            Name = "Frontend Team",
            Code = "SWDEV-FE",
            Level = OrgUnitLevel.Team,
            ParentId = deptSoftware.Id,
            SortOrder = 2
        };
        context.OrganizationalUnits.AddRange(teamBackend, teamFrontend);
        await context.SaveChangesAsync();
    }
}
