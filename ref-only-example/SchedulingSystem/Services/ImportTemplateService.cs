using System.Text;

namespace SchedulingSystem.Services;

/// <summary>
/// Service for generating downloadable import templates (CSV format)
/// </summary>
public class ImportTemplateService
{
    /// <summary>
    /// Generate a CSV template with headers and optional sample data
    /// </summary>
    private static byte[] GenerateCsvTemplate(string[] headers, string[][]? sampleRows = null)
    {
        var sb = new StringBuilder();

        // Add headers
        sb.AppendLine(string.Join(";", headers));

        // Add sample rows if provided
        if (sampleRows != null)
        {
            foreach (var row in sampleRows)
            {
                sb.AppendLine(string.Join(";", row));
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    #region UNTIS Templates (GPU files)

    public static byte[] GenerateDepartmentsTemplate()
    {
        var headers = new[] { "Name", "Description" };
        var samples = new[]
        {
            new[] { "Mathematics", "Mathematics Department" },
            new[] { "Science", "Science Department" },
            new[] { "Languages", "Languages Department" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateTeachersTemplate()
    {
        var headers = new[]
        {
            "Name (First Name)", "Full Name (Last Name)", "Statistic 1", "Personnel Number", "Home Room",
            "Field 6", "Field 7", "Min. per./day", "Max. per./day", "Min NTP", "Max NTP",
            "Min Lunchbreak", "Max Lunchbreak", "Max. Consec. pers.", "Weekly quota", "Weekly value",
            "Department 1", "Value resp. factor", "Field 19", "Field 20", "Status", "Yearly quota",
            "Field 23", "Description", "Foreground colour", "Background colour", "Statistic 2",
            "Field 28", "First name (field 29)", "Title", "Gender", "Field 32", "E-mail address",
            "Field 34-38", "Field 35", "Field 36", "Field 37", "Field 38", "Phone", "Mobile",
            "Date of birth (YYYYMMDD)", "Field 42"
        };
        var samples = new[]
        {
            new[] { "John", "Smith", "", "T001", "R101", "", "", "4", "6", "1", "2", "30", "60", "4",
                   "25", "25", "Mathematics", "", "", "", "Active", "900", "", "Math teacher", "", "",
                   "", "", "", "Mr.", "2", "", "john.smith@school.com", "", "", "", "", "", "555-1234",
                   "555-5678", "19800115", "" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateClassesTemplate()
    {
        var headers = new[]
        {
            "Name", "Long name", "Category", "Class teacher", "Statistic 1", "Statistic 2",
            "Class level", "Department 1", "Department 2", "Department 3", "Room", "Field 12",
            "Field 13", "Max. per./day", "Weekly periods", "Weekly value", "Status", "Description",
            "Foreground colour", "Background colour", "Codes", "Student group", "Min. double pers.",
            "Max. double pers.", "Block size", "Collision check", "Room group", "Building", "Text", "Value"
        };
        var samples = new[]
        {
            new[] { "1A", "Grade 1A", "Primary", "John Smith", "", "", "1", "Primary", "", "", "R101",
                   "", "", "6", "25", "25", "Active", "First grade class", "", "", "", "", "1", "3",
                   "1", "1", "", "", "", "25" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateStudentsTemplate()
    {
        var headers = new[]
        {
            "Name", "First name", "Class", "Status", "Student number", "Date of birth (YYYYMMDD)",
            "Gender", "Field 8", "E-mail address", "Mobile number", "Field 11", "Field 12",
            "Statistic 1", "Field 14"
        };
        var samples = new[]
        {
            new[] { "Doe", "Jane", "1A", "Active", "S001", "20150315", "1", "", "jane.doe@example.com",
                   "555-9999", "", "", "", "" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateSubjectsTemplate()
    {
        var headers = new[]
        {
            "Code", "Name", "Long name", "Category", "Statistical category 1", "Statistical category 2",
            "Lessons per week", "Field 8", "Block", "Room group", "Subject group", "Weekly periods",
            "Weekly value", "Yearly value", "Department 1", "Field 16 (Text)", "Description",
            "Foreground colour", "Background colour", "Value factor", "Codes"
        };
        var samples = new[]
        {
            new[] { "MAT", "Mathematics", "Mathematics", "Core", "", "", "5", "", "", "", "", "5",
                   "5", "180", "Mathematics", "", "Math lessons", "", "", "", "" },
            new[] { "ENG", "English", "English Language", "Core", "", "", "4", "", "", "", "", "4",
                   "4", "144", "Languages", "", "English lessons", "", "", "", "" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateRoomsTemplate()
    {
        var headers = new[]
        {
            "Room Number", "Long name", "Capacity", "Department 1", "Field 5", "Field 6",
            "Building", "Weight", "Description", "Foreground colour", "Background colour",
            "Field 12", "Field 13", "Field 14", "Field 15", "Field 16", "Field 17"
        };
        var samples = new[]
        {
            new[] { "R101", "Mathematics Room 101", "30", "Mathematics", "", "", "Main Building",
                   "1", "Primary math classroom", "", "", "", "", "", "", "", "" },
            new[] { "R102", "Science Lab", "25", "Science", "", "", "Science Wing", "1",
                   "Science laboratory", "", "", "", "", "", "", "", "" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateLessonsTemplate()
    {
        var headers = new[]
        {
            "L-No.", "Pers./week", "Class per./wk", "Teacher per./wk.", "Class", "Teacher", "Subject",
            "Subject Room", "Less. Statistic 1.", "No. of students", "Week value", "Group",
            "Teacher text", "Line value", "From date", "To date", "Year value", "Text",
            "Partition No.", "Home Room", "Description", "F`ground colour", "B`kground colour",
            "Codes", "Consec. Subj. Class", "Consec. Subj. Teach.", "Class Collision Ref.",
            "Min Double pers.", "Max. Double pers.", "Block size", "Pers. in the room", "Priority",
            "Teacher Statistic 1", "Students male", "Students female", "Value resp. factor",
            "2nd Block", "3rd Block", "Line text 2", "Value", "Value(in 1/100000)",
            "Student group", "Weekly periods in 'Year's planning in terms'"
        };
        var samples = new[]
        {
            new[] { "1", "5", "5", "5", "1A", "John", "MAT", "R101", "", "25", "5.00000", "",
                   "", "", "20240901", "20250630", "0.02150", "", "", "R101", "", "", "", "",
                   "", "", "", "1", "2", "", "", "0", "", "13", "12", "", "", "", "", "500000",
                   "5.00000", "", "0" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateTeacherQualificationsTemplate()
    {
        var headers = new[] { "Teacher Code", "Subject Code", "Department", "Field 4" };
        var samples = new[]
        {
            new[] { "John", "MAT", "Mathematics", "" },
            new[] { "John", "SCI", "Science", "" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateTimetableTemplate()
    {
        var headers = new[]
        {
            "L-No", "Class", "Teacher", "Subject", "Room", "Day", "Period"
        };
        var samples = new[]
        {
            new[] { "1", "1A", "John", "MAT", "R101", "1", "1" },
            new[] { "1", "1A", "John", "MAT", "R101", "1", "2" },
            new[] { "2", "1A", "Jane", "ENG", "R102", "2", "1" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateAvailabilityTemplate()
    {
        var headers = new[]
        {
            "Type", "Element Name", "Day Number", "Period Number", "Weight", "Reason"
        };
        var samples = new[]
        {
            new[] { "TE", "John", "1", "1", "-3", "Doctor appointment" },
            new[] { "TE", "John", "1", "2", "2", "Prefer morning slots" },
            new[] { "CL", "1A", "5", "6", "-2", "No classes on Friday afternoon" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    #endregion

    #region Manual Import Templates

    public static byte[] GenerateManualTeachersTemplate()
    {
        var headers = new[]
        {
            "FirstName", "LastName", "Email", "PhoneNumber", "DepartmentName",
            "PersonnelNumber", "AvailableForSubstitution", "MaxSubstitutionsPerWeek"
        };
        var samples = new[]
        {
            new[] { "John", "Smith", "john.smith@school.com", "555-1234", "Mathematics", "T001", "true", "5" },
            new[] { "Jane", "Doe", "jane.doe@school.com", "555-5678", "Science", "T002", "true", "3" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateManualClassesTemplate()
    {
        var headers = new[] { "Name", "Level", "DepartmentName", "TeacherName", "MaxStudents" };
        var samples = new[]
        {
            new[] { "1A", "1", "Primary", "John Smith", "25" },
            new[] { "2B", "2", "Primary", "Jane Doe", "30" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateManualStudentsTemplate()
    {
        var headers = new[]
        {
            "FirstName", "LastName", "StudentNumber", "DateOfBirth (YYYY-MM-DD)", "Gender",
            "Email", "ClassName"
        };
        var samples = new[]
        {
            new[] { "Alice", "Johnson", "S001", "2015-03-15", "Female", "alice.j@example.com", "1A" },
            new[] { "Bob", "Williams", "S002", "2015-07-22", "Male", "bob.w@example.com", "1A" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateManualSubjectsTemplate()
    {
        var headers = new[] { "Code", "Name", "DepartmentName", "WeeklyHours", "Description" };
        var samples = new[]
        {
            new[] { "MAT", "Mathematics", "Mathematics", "5", "Math lessons" },
            new[] { "ENG", "English", "Languages", "4", "English language" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateManualRoomsTemplate()
    {
        var headers = new[] { "RoomNumber", "Capacity", "Building", "Floor", "RoomType" };
        var samples = new[]
        {
            new[] { "R101", "30", "Main Building", "1", "Classroom" },
            new[] { "LAB1", "25", "Science Wing", "2", "Laboratory" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateManualPeriodsTemplate()
    {
        var headers = new[] { "PeriodNumber", "StartTime (HH:mm)", "EndTime (HH:mm)", "Name" };
        var samples = new[]
        {
            new[] { "1", "08:00", "08:50", "Period 1" },
            new[] { "2", "09:00", "09:50", "Period 2" },
            new[] { "3", "10:00", "10:50", "Period 3" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    public static byte[] GenerateManualLessonsTemplate()
    {
        var headers = new[]
        {
            "LessonNumber", "ClassName", "TeacherFirstName", "SubjectCode", "WeeklyPeriods"
        };
        var samples = new[]
        {
            new[] { "1", "1A", "John", "MAT", "5" },
            new[] { "2", "1A", "Jane", "ENG", "4" }
        };
        return GenerateCsvTemplate(headers, samples);
    }

    #endregion
}
