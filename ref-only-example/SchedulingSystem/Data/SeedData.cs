using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Models;

namespace SchedulingSystem.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Check if database already has data
        if (await context.SchoolYears.AnyAsync())
        {
            return; // Database has been seeded
        }

        // ============================================================
        // IMPORTANT: Only seed entities NOT imported from UNTIS
        // ============================================================
        // UNTIS Import provides: Departments, Teachers, Classes, Subjects, Rooms, Lessons
        // Manual seed provides: SchoolYears, Periods
        // Students must be imported separately (not in UNTIS)
        // ============================================================

        // Create School Year
        var schoolYear = new SchoolYear
        {
            Name = "2024-2025",
            StartDate = new DateTime(2024, 9, 1),
            EndDate = new DateTime(2025, 6, 30),
            IsActive = true
        };
        context.SchoolYears.Add(schoolYear);
        await context.SaveChangesAsync();

        /* ============================================================
         * COMMENTED OUT: These entities are imported from UNTIS
         * Use Admin > Import Data > UNTIS Complete Import instead
         * ============================================================

        // Create Subjects from CSV data (NOW IMPORTED FROM UNTIS GPU006.TXT)
        var subjects = new List<Subject>
        {
            new Subject { Id = 1, Code = "Arab", Name = "Arabic", Category = "Language", Color = "#FF6B6B", DefaultDuration = 1, RequiredRoomType = "Classroom" },
            new Subject { Id = 2, Code = "Arts", Name = "Kunst", Category = "Arts", Color = "#4ECDC4", DefaultDuration = 1, RequiredRoomType = "Art Room" },
            new Subject { Id = 3, Code = "Bio", Name = "Biologie", Category = "Science", Color = "#95E1D3", DefaultDuration = 1, RequiredRoomType = "Biology Lab" },
            new Subject { Id = 4, Code = "Che", Name = "Chemie", Category = "Science", Color = "#F38181", DefaultDuration = 1, RequiredRoomType = "Chemistry Lab" },
            new Subject { Id = 5, Code = "Civ", Name = "Civics", Category = "Social Studies", Color = "#AA96DA", DefaultDuration = 1, RequiredRoomType = "Classroom" },
            new Subject { Id = 6, Code = "CopR", Name = "Coptic Religion", Category = "Religion", Color = "#FCBAD3", DefaultDuration = 1, RequiredRoomType = "Classroom" },
            new Subject { Id = 7, Code = "Des", Name = "Design", Category = "Arts", Color = "#FFFFD2", DefaultDuration = 1, RequiredRoomType = "Design Room" },
            new Subject { Id = 8, Code = "Deu", Name = "Deutsch", Category = "Language", Color = "#A8D8EA", DefaultDuration = 1, RequiredRoomType = "Classroom" },
            new Subject { Id = 9, Code = "Eng", Name = "English", Category = "Language", Color = "#FFAAA5", DefaultDuration = 1, RequiredRoomType = "Classroom" },
            new Subject { Id = 10, Code = "Fra", Name = "Französisch - L Acq", Category = "Language", Color = "#FF8B94", DefaultDuration = 1, RequiredRoomType = "Classroom" },
            new Subject { Id = 11, Code = "IUS", Name = "Individuals and Societies (GeGeo)", Category = "Social Studies", Color = "#FFA07A", DefaultDuration = 1, RequiredRoomType = "Classroom" },
            new Subject { Id = 12, Code = "Mat", Name = "Mathematik", Category = "Core", Color = "#98DDCA", DefaultDuration = 1, RequiredRoomType = "Classroom" },
            new Subject { Id = 13, Code = "Mus", Name = "Music", Category = "Arts", Color = "#D5AAFF", DefaultDuration = 1, RequiredRoomType = "Music Room" },
            new Subject { Id = 15, Code = "PHE", Name = "Physical and Health Education", Category = "PE", Color = "#C7CEEA", DefaultDuration = 1, RequiredRoomType = "Gym" },
            new Subject { Id = 16, Code = "Phy", Name = "Physik", Category = "Science", Color = "#B8E6B8", DefaultDuration = 1, RequiredRoomType = "Physics Lab" },
            new Subject { Id = 17, Code = "Rel", Name = "Religion", Category = "Religion", Color = "#FFB6B9", DefaultDuration = 1, RequiredRoomType = "Classroom" },
            new Subject { Id = 18, Code = "Sach", Name = "Sachunterricht", Category = "General Studies", Color = "#FEC8D8", DefaultDuration = 1, RequiredRoomType = "Classroom" },
            new Subject { Id = 19, Code = "Sci", Name = "Science", Category = "Science", Color = "#957DAD", DefaultDuration = 1, RequiredRoomType = "Science Lab" }
        };
        context.Subjects.AddRange(subjects);
        await context.SaveChangesAsync();

        // Create Teachers from CSV data
        var teachers = new List<Teacher>
        {
            new Teacher { Id = 1, Name = "AA", FirstName = "AA", LastName = "", Email = "aa@school.com", MaxHoursPerWeek = 0 },
            new Teacher { Id = 2, Name = "Aisha", FirstName = "Aisha", LastName = "Tamer", Email = "aisha.tamer@school.com", MaxHoursPerWeek = 18 },
            new Teacher { Id = 3, Name = "Alija", FirstName = "Alija", LastName = "Schaffrin", Email = "alija.schaffrin@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 4, Name = "Amina", FirstName = "Amina", LastName = "Dorbok", Email = "amina.dorbok@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 5, Name = "Doaa", FirstName = "Doaa", LastName = "Ashraf", Email = "doaa.ashraf@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 6, Name = "Faten", FirstName = "Faten", LastName = "Farouk", Email = "faten.farouk@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 7, Name = "Hanadi", FirstName = "Hanadi", LastName = "Mohamed", Email = "hanadi.mohamed@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 8, Name = "Heba", FirstName = "Heba", LastName = "Hashem", Email = "heba.hashem@school.com", MaxHoursPerWeek = 6 },
            new Teacher { Id = 9, Name = "Jouvana", FirstName = "Jouvana", LastName = "Taymour", Email = "jouvana.taymour@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 10, Name = "Kiw", FirstName = "Kiw Mahm", LastName = "Kiwan", Email = "kiw.kiwan@school.com", MaxHoursPerWeek = 18 },
            new Teacher { Id = 11, Name = "Magy", FirstName = "Magy", LastName = "Maged", Email = "magy.maged@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 12, Name = "Mariam", FirstName = "Mariam", LastName = "Amr", Email = "mariam.amr@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 13, Name = "Marilena", FirstName = "Marilena", LastName = "Roussoglu", Email = "marilena.roussoglu@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 14, Name = "Marwa", FirstName = "Marwa", LastName = "Hedar", Email = "marwa.hedar@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 15, Name = "Miriam", FirstName = "Miriam", LastName = "Opitz", Email = "miriam.opitz@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 16, Name = "Mohamed", FirstName = "Mohamed", LastName = "Abdel Khalik", Email = "mohamed.khalik@school.com", MaxHoursPerWeek = 5 },
            new Teacher { Id = 17, Name = "Moustafa", FirstName = "Moustafa", LastName = "Shoukry", Email = "moustafa.shoukry@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 18, Name = "Natascha", FirstName = "Natascha", LastName = "Omar", Email = "natascha.omar@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 19, Name = "Noreye", FirstName = "Noreye", LastName = "Khalaf", Email = "noreye.khalaf@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 20, Name = "Randa", FirstName = "Randa", LastName = "El Sayed", Email = "randa.elsayed@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 21, Name = "Rowina", FirstName = "Rowina", LastName = "Saleh", Email = "rowina.saleh@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 22, Name = "Sarah", FirstName = "Sarah", LastName = "Abdelfattah", Email = "sarah.abdelfattah@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 23, Name = "Sina", FirstName = "Sina", LastName = "Lange", Email = "sina.lange@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 24, Name = "Tarek", FirstName = "Tarek", LastName = "Salama", Email = "tarek.salama@school.com", MaxHoursPerWeek = 23 },
            new Teacher { Id = 26, Name = "Yasmine", FirstName = "Yasmine", LastName = "El Sabbagh", Email = "yasmine.sabbagh@school.com", MaxHoursPerWeek = 23 }
        };
        context.Teachers.AddRange(teachers);
        await context.SaveChangesAsync();

        // Create Rooms from CSV data
        var rooms = new List<Room>
        {
            new Room { Id = 1, RoomNumber = "Hof1", Name = "Hof 1", RoomType = "Outdoor", Capacity = 60, IsActive = true },
            new Room { Id = 2, RoomNumber = "Hof2", Name = "Hof 2", RoomType = "Outdoor", Capacity = 60, IsActive = true },
            new Room { Id = 3, RoomNumber = "R1A", Name = "Raum1A", RoomType = "Classroom", Capacity = 25, IsActive = true },
            new Room { Id = 4, RoomNumber = "R1B", Name = "Raum1B", RoomType = "Classroom", Capacity = 25, IsActive = true },
            new Room { Id = 5, RoomNumber = "R2A", Name = "Raum2A", RoomType = "Classroom", Capacity = 25, IsActive = true },
            new Room { Id = 6, RoomNumber = "R2B", Name = "Raum2B", RoomType = "Classroom", Capacity = 25, IsActive = true },
            new Room { Id = 7, RoomNumber = "R3A", Name = "Raum3A", RoomType = "Classroom", Capacity = 25, IsActive = true },
            new Room { Id = 8, RoomNumber = "R3B", Name = "Raum3B", RoomType = "Classroom", Capacity = 25, IsActive = true },
            new Room { Id = 9, RoomNumber = "R4A", Name = "Raum4A", RoomType = "Classroom", Capacity = 25, IsActive = true },
            new Room { Id = 10, RoomNumber = "R5A", Name = "Raum5A", RoomType = "Classroom", Capacity = 25, IsActive = true },
            new Room { Id = 11, RoomNumber = "R6A", Name = "Raum6A", RoomType = "Classroom", Capacity = 25, IsActive = true },
            new Room { Id = 12, RoomNumber = "RBio", Name = "RBio", RoomType = "Biology Lab", Capacity = 24, IsActive = true },
            new Room { Id = 13, RoomNumber = "RChe", Name = "Chemieraum", RoomType = "Chemistry Lab", Capacity = 24, IsActive = true },
            new Room { Id = 14, RoomNumber = "RKu", Name = "Kunstraum", RoomType = "Art Room", Capacity = 24, IsActive = true },
            new Room { Id = 15, RoomNumber = "RMus", Name = "Musikraum", RoomType = "Music Room", Capacity = 24, IsActive = true },
            new Room { Id = 16, RoomNumber = "RPhy", Name = "Physikraum", RoomType = "Physics Lab", Capacity = 24, IsActive = true }
        };
        context.Rooms.AddRange(rooms);
        await context.SaveChangesAsync();

        // Create Classes from CSV data
        var classes = new List<Class>
        {
            new Class { Id = 1, Name = "1A", YearLevel = 1, MaleStudents = 12, FemaleStudents = 12 },
            new Class { Id = 2, Name = "1B", YearLevel = 1, MaleStudents = 12, FemaleStudents = 12 },
            new Class { Id = 3, Name = "2A", YearLevel = 2, MaleStudents = 12, FemaleStudents = 12 },
            new Class { Id = 4, Name = "2B", YearLevel = 2, MaleStudents = 12, FemaleStudents = 12 },
            new Class { Id = 5, Name = "3A", YearLevel = 3, MaleStudents = 12, FemaleStudents = 12 },
            new Class { Id = 6, Name = "3B", YearLevel = 3, MaleStudents = 12, FemaleStudents = 12 },
            new Class { Id = 7, Name = "4A", YearLevel = 4, MaleStudents = 12, FemaleStudents = 12 },
            new Class { Id = 8, Name = "5A", YearLevel = 5, MaleStudents = 12, FemaleStudents = 12 },
            new Class { Id = 9, Name = "6A", YearLevel = 6, MaleStudents = 12, FemaleStudents = 12 }
        };
        context.Classes.AddRange(classes);
        await context.SaveChangesAsync();

        */ // End of UNTIS entities comment block

        // ============================================================
        // Periods - Must be seeded manually (NOT in UNTIS import)
        // ============================================================

        // Create Periods
        var periods = new List<Period>
        {
            new Period { Id = 1, PeriodNumber = 1, Name = "Period 1", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 0, 0), IsBreak = false },
            new Period { Id = 2, PeriodNumber = 2, Name = "Period 2", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 0, 0), IsBreak = false },
            new Period { Id = 3, PeriodNumber = 3, Name = "Period 3", StartTime = new TimeSpan(10, 30, 0), EndTime = new TimeSpan(11, 30, 0), IsBreak = false },
            new Period { Id = 4, PeriodNumber = 4, Name = "Period 4", StartTime = new TimeSpan(11, 30, 0), EndTime = new TimeSpan(12, 30, 0), IsBreak = false },
            new Period { Id = 5, PeriodNumber = 5, Name = "Period 5", StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 0, 0), IsBreak = false },
            new Period { Id = 6, PeriodNumber = 6, Name = "Period 6", StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(15, 0, 0), IsBreak = false },
            new Period { Id = 7, PeriodNumber = 7, Name = "Period 7", StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(16, 0, 0), IsBreak = false }
        };
        context.Periods.AddRange(periods);
        await context.SaveChangesAsync();

        // NOTE: Lessons are imported from UNTIS Complete Import
        // Lesson seeding removed - use Admin > Import Data > UNTIS Complete Import
    }
}
