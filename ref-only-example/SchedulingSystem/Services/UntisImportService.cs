using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Services;

/// <summary>
/// Service for importing UNTIS export files (GPU*.TXT)
/// Handles CSV parsing and data import from UNTIS timetable software
/// </summary>
public class UntisImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UntisImportService> _logger;

    public UntisImportService(ApplicationDbContext context, ILogger<UntisImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Import result with statistics
    /// </summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public int RecordsProcessed { get; set; }
        public int RecordsImported { get; set; }
        public int RecordsUpdated { get; set; }
        public int RecordsSkipped { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string Message => Success
            ? $"Import completed: {RecordsImported} imported, {RecordsUpdated} updated, {RecordsSkipped} skipped"
            : $"Import failed: {string.Join(", ", Errors)}";
    }

    /// <summary>
    /// Clears all data from tables that can be imported from UNTIS files
    /// This prepares the database for a clean import
    /// </summary>
    public async Task<ImportResult> CleanImportableDataAsync()
    {
        var result = new ImportResult();

        try
        {
            _logger.LogInformation("Cleaning importable data from database");

            // Delete in correct order (respecting foreign key constraints)
            // Most dependent first, least dependent last

            // 1. ScheduledLessons (references Lessons, Periods, Rooms, Timetables)
            var scheduledLessonsCount = await _context.ScheduledLessons.CountAsync();
            _context.ScheduledLessons.RemoveRange(_context.ScheduledLessons);
            _logger.LogInformation("Removed {Count} scheduled lessons", scheduledLessonsCount);

            // 2. Lessons (references Teachers, Classes, Subjects, Rooms)
            var lessonsCount = await _context.Lessons.CountAsync();
            _context.Lessons.RemoveRange(_context.Lessons);
            _logger.LogInformation("Removed {Count} lessons", lessonsCount);

            // 3. TeacherSubjects (references Teachers, Subjects)
            var teacherSubjectsCount = await _context.TeacherSubjects.CountAsync();
            _context.TeacherSubjects.RemoveRange(_context.TeacherSubjects);
            _logger.LogInformation("Removed {Count} teacher-subject relationships", teacherSubjectsCount);

            // 4. Students (references Classes)
            var studentsCount = await _context.Students.CountAsync();
            _context.Students.RemoveRange(_context.Students);
            _logger.LogInformation("Removed {Count} students", studentsCount);

            // 5. Classes (references Teachers, Departments)
            var classesCount = await _context.Classes.CountAsync();
            _context.Classes.RemoveRange(_context.Classes);
            _logger.LogInformation("Removed {Count} classes", classesCount);

            // 6. Subjects (references Rooms, Departments)
            var subjectsCount = await _context.Subjects.CountAsync();
            _context.Subjects.RemoveRange(_context.Subjects);
            _logger.LogInformation("Removed {Count} subjects", subjectsCount);

            // 7. Rooms (self-referencing, references Departments)
            var roomsCount = await _context.Rooms.CountAsync();
            _context.Rooms.RemoveRange(_context.Rooms);
            _logger.LogInformation("Removed {Count} rooms", roomsCount);

            // 8. Teachers (references Departments)
            var teachersCount = await _context.Teachers.CountAsync();
            _context.Teachers.RemoveRange(_context.Teachers);
            _logger.LogInformation("Removed {Count} teachers", teachersCount);

            // 9. Departments (no dependencies)
            var departmentsCount = await _context.Departments.CountAsync();
            _context.Departments.RemoveRange(_context.Departments);
            _logger.LogInformation("Removed {Count} departments", departmentsCount);

            await _context.SaveChangesAsync();

            result.Success = true;
            result.RecordsProcessed = scheduledLessonsCount + lessonsCount + teacherSubjectsCount + studentsCount +
                                     classesCount + subjectsCount + roomsCount + teachersCount + departmentsCount;

            _logger.LogInformation("Successfully cleaned {Count} total records from importable tables", result.RecordsProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning importable data");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Parse CSV line respecting quoted values and semicolon separator
    /// </summary>
    private List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ';' && !inQuotes)
            {
                fields.Add(currentField.ToString().Trim());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        fields.Add(currentField.ToString().Trim());
        return fields;
    }

    /// <summary>
    /// Parses a room field that may contain multiple rooms separated by space, comma, or semicolon
    /// Returns a list of room numbers
    /// </summary>
    private List<string> ParseMultipleRooms(string roomField)
    {
        if (string.IsNullOrWhiteSpace(roomField))
            return new List<string>();

        // Try different delimiters: space, comma, semicolon
        char[] delimiters = new[] { ' ', ',', ';' };

        return roomField
            .Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.Trim())
            .Where(r => !string.IsNullOrEmpty(r))
            .ToList();
    }

    /// <summary>
    /// Detects if a line is likely a header row based on common header patterns
    /// Returns true if the line appears to be a header
    /// </summary>
    private bool IsHeaderLine(string line, string[] expectedDataPatterns)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var upperLine = line.ToUpperInvariant();

        // Common header keywords in UNTIS files
        var headerKeywords = new[] {
            "NAME", "KÜRZEL", "KURZNAME", "LANGNAME", "BEZEICHNUNG",
            "LEHRER", "KLASSE", "RAUM", "FACH", "ABTEILUNG",
            "TEACHER", "CLASS", "ROOM", "SUBJECT", "DEPARTMENT",
            "PERIOD", "TIME", "DAY", "WOCHENTAG", "TAG"
        };

        // Check if line contains multiple header keywords
        int headerKeywordCount = headerKeywords.Count(keyword => upperLine.Contains(keyword));
        if (headerKeywordCount >= 2)
            return true;

        // If we have expected data patterns (e.g., numeric IDs, specific formats),
        // check if the first field doesn't match those patterns
        if (expectedDataPatterns != null && expectedDataPatterns.Length > 0)
        {
            var fields = ParseCsvLine(line);
            if (fields.Count > 0)
            {
                var firstField = fields[0].Trim();

                // Check if first field doesn't match expected patterns
                bool matchesPattern = expectedDataPatterns.Any(pattern =>
                {
                    if (pattern == "NUMERIC")
                        return int.TryParse(firstField, out _);
                    if (pattern == "CODE")
                        return !string.IsNullOrEmpty(firstField) && firstField.Length <= 10 && !firstField.Contains(" ");
                    return false;
                });

                // If it doesn't match expected data patterns, likely a header
                if (!matchesPattern && headerKeywordCount > 0)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Import departments from GPU007.TXT
    /// </summary>
    public async Task<ImportResult> ImportDepartmentsAsync(Stream fileStream)
    {
        var result = new ImportResult();

        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            string? line;
            bool isFirstLine = true;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Skip header if detected
                if (isFirstLine)
                {
                    isFirstLine = false;
                    if (IsHeaderLine(line, new[] { "CODE" }))
                        continue;
                }

                result.RecordsProcessed++;
                var fields = ParseCsvLine(line);
                var csvLineData = string.Join(";", fields);

                if (fields.Count < 2)
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Insufficient fields (need at least 2, got {fields.Count})|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                var name = fields[0];
                var fullName = fields.Count > 1 ? fields[1] : null;

                if (string.IsNullOrEmpty(name))
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Department name is empty|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                var existing = await _context.Departments.FirstOrDefaultAsync(d => d.Name == name);

                if (existing == null)
                {
                    _context.Departments.Add(new Department
                    {
                        Name = name,
                        FullName = fullName,
                        IsActive = true
                    });
                    result.RecordsImported++;
                }
                else
                {
                    existing.FullName = fullName;
                    existing.IsActive = true;
                    result.RecordsUpdated++;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing departments");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Import teachers from GPU004.TXT
    /// </summary>
    public async Task<ImportResult> ImportTeachersAsync(Stream fileStream)
    {
        var result = new ImportResult();

        try
        {
            // Load departments for reference
            var departments = await _context.Departments.ToListAsync();

            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            string? line;
            bool isFirstLine = true;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Skip header if detected
                if (isFirstLine)
                {
                    isFirstLine = false;
                    if (IsHeaderLine(line, new[] { "CODE" }))
                        continue;
                }

                result.RecordsProcessed++;
                var fields = ParseCsvLine(line);
                var csvLineData = string.Join(";", fields);

                if (fields.Count < 2)
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Insufficient fields (need at least 2, got {fields.Count})|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                // UNTIS GPU004 Field 1 = First Name, Field 2 = Last Name
                var firstName = fields[0];
                if (string.IsNullOrEmpty(firstName))
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Teacher first name is empty|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                var lastName = fields.Count > 1 ? fields[1] : null;

                // Find existing teacher by FirstName and LastName combination
                var existing = await _context.Teachers.FirstOrDefaultAsync(t =>
                    t.FirstName == firstName &&
                    (lastName == null || t.LastName == lastName));

                var teacher = existing ?? new Teacher { FirstName = firstName, LastName = lastName };

                // Map fields (based on GPU004.TXT column order)
                // Field 1 (index 0): Name = First Name - already mapped above
                // Field 2 (index 1): Full Name = Last Name - already mapped above
                teacher.Statistic1 = fields.Count > 2 ? fields[2] : null;
                teacher.PersonnelNumber = fields.Count > 3 ? fields[3] : null;
                teacher.HomeRoom = fields.Count > 4 ? fields[4] : null;

                // Numeric fields
                if (fields.Count > 7 && int.TryParse(fields[7], out int minPeriods))
                    teacher.MinPeriodsPerDay = minPeriods;
                if (fields.Count > 8 && int.TryParse(fields[8], out int maxPeriods))
                    teacher.MaxPeriodsPerDay = maxPeriods;
                if (fields.Count > 9 && int.TryParse(fields[9], out int minNTP))
                    teacher.MinNonTeachingPeriods = minNTP;
                if (fields.Count > 10 && int.TryParse(fields[10], out int maxNTP))
                    teacher.MaxNonTeachingPeriods = maxNTP;
                if (fields.Count > 11 && int.TryParse(fields[11], out int minLunch))
                    teacher.MinLunchBreak = minLunch;
                if (fields.Count > 12 && int.TryParse(fields[12], out int maxLunch))
                    teacher.MaxLunchBreak = maxLunch;
                if (fields.Count > 13 && int.TryParse(fields[13], out int maxConsec))
                    teacher.MaxConsecutivePeriods = maxConsec;

                // Decimal fields
                if (fields.Count > 14 && decimal.TryParse(fields[14], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal quota))
                    teacher.WeeklyQuota = quota;
                if (fields.Count > 15 && decimal.TryParse(fields[15], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
                    teacher.WeeklyValue = value;
                if (fields.Count > 21 && decimal.TryParse(fields[21], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal yearlyQuota))
                    teacher.YearlyQuota = yearlyQuota;

                // Department
                if (fields.Count > 16 && !string.IsNullOrEmpty(fields[16]))
                {
                    var dept = departments.FirstOrDefault(d => d.Name == fields[16]);
                    teacher.DepartmentId = dept?.Id;
                }

                // Other fields
                teacher.ValueFactor = fields.Count > 17 ? fields[17] : null;
                teacher.Status = fields.Count > 20 ? fields[20] : null;
                teacher.Description = fields.Count > 23 ? fields[23] : null;
                teacher.ForegroundColor = fields.Count > 24 ? fields[24] : null;
                teacher.BackgroundColor = fields.Count > 25 ? fields[25] : null;
                teacher.Statistic2 = fields.Count > 26 ? fields[26] : null;
                // Field 29 (index 28): First name - not used as we get it from Field 1
                teacher.Title = fields.Count > 29 ? fields[29] : null;
                teacher.Gender = fields.Count > 30 ? fields[30] : null;
                teacher.Email = fields.Count > 32 ? fields[32] : null;
                teacher.PhoneNumber = fields.Count > 38 ? fields[38] : null;
                teacher.MobileNumber = fields.Count > 39 ? fields[39] : null;

                // Date of birth (YYYYMMDD format)
                if (fields.Count > 40 && !string.IsNullOrEmpty(fields[40]) && DateTime.TryParseExact(fields[40], "yyyyMMdd", null, DateTimeStyles.None, out DateTime dob))
                    teacher.DateOfBirth = dob;

                teacher.IsActive = true;

                if (existing == null)
                {
                    _context.Teachers.Add(teacher);
                    result.RecordsImported++;
                }
                else
                {
                    result.RecordsUpdated++;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing teachers");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Import classes from GPU003.TXT
    /// </summary>
    public async Task<ImportResult> ImportClassesAsync(Stream fileStream)
    {
        var result = new ImportResult();

        try
        {
            var departments = await _context.Departments.ToListAsync();
            var teachers = await _context.Teachers.ToListAsync();

            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            string? line;
            bool isFirstLine = true;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Skip header if detected
                if (isFirstLine)
                {
                    isFirstLine = false;
                    if (IsHeaderLine(line, new[] { "CODE" }))
                        continue;
                }

                result.RecordsProcessed++;
                var fields = ParseCsvLine(line);
                var csvLineData = string.Join(";", fields);

                if (fields.Count < 2)
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Insufficient fields (need at least 2, got {fields.Count})|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                var name = fields[0];
                if (string.IsNullOrEmpty(name))
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Class name is empty|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                var existing = await _context.Classes.FirstOrDefaultAsync(c => c.Name == name);
                var classEntity = existing ?? new Class { Name = name };

                classEntity.FullName = fields.Count > 1 ? fields[1] : null;
                classEntity.Statistic1 = fields.Count > 2 ? fields[2] : null;
                classEntity.HomeRoom = fields.Count > 3 ? fields[3] : null;

                // Numeric fields
                if (fields.Count > 6 && int.TryParse(fields[6], out int minPeriods))
                    classEntity.MinPeriodsPerDay = minPeriods;
                if (fields.Count > 7 && int.TryParse(fields[7], out int maxPeriods))
                    classEntity.MaxPeriodsPerDay = maxPeriods;
                if (fields.Count > 8 && int.TryParse(fields[8], out int minLunch))
                    classEntity.MinLunchBreak = minLunch;
                if (fields.Count > 9 && int.TryParse(fields[9], out int maxLunch))
                    classEntity.MaxLunchBreak = maxLunch;
                if (fields.Count > 10 && int.TryParse(fields[10], out int consecMain))
                    classEntity.ConsecutiveMainSubjects = consecMain;
                if (fields.Count > 11 && int.TryParse(fields[11], out int maxConsec))
                    classEntity.MaxConsecutiveSubjects = maxConsec;

                if (fields.Count > 18 && int.TryParse(fields[18], out int classLevel))
                    classEntity.ClassLevel = classLevel;

                // Try to extract year level from name (e.g., "1A" -> 1)
                if (!string.IsNullOrEmpty(name) && char.IsDigit(name[0]))
                    classEntity.YearLevel = int.Parse(name[0].ToString());

                if (fields.Count > 16 && int.TryParse(fields[16], out int femaleStudents))
                    classEntity.FemaleStudents = femaleStudents;
                if (fields.Count > 17 && int.TryParse(fields[17], out int maleStudents))
                    classEntity.MaleStudents = maleStudents;

                // Date fields (YYYYMMDD format)
                if (fields.Count > 19 && !string.IsNullOrEmpty(fields[19]) && DateTime.TryParseExact(fields[19], "yyyyMMdd", null, DateTimeStyles.None, out DateTime startDate))
                    classEntity.LessonStartDate = startDate;
                if (fields.Count > 20 && !string.IsNullOrEmpty(fields[20]) && DateTime.TryParseExact(fields[20], "yyyyMMdd", null, DateTimeStyles.None, out DateTime endDate))
                    classEntity.LessonEndDate = endDate;

                classEntity.Statistic2 = fields.Count > 25 ? fields[25] : null;

                // Class Teacher - find by name/code
                if (fields.Count > 29 && !string.IsNullOrEmpty(fields[29]))
                {
                    var teacher = teachers.FirstOrDefault(t => t.Name == fields[29]);
                    classEntity.ClassTeacherId = teacher?.Id;
                }

                // Department
                if (fields.Count > 14 && !string.IsNullOrEmpty(fields[14]))
                {
                    var dept = departments.FirstOrDefault(d => d.Name == fields[14]);
                    classEntity.DepartmentId = dept?.Id;
                }

                classEntity.Description = fields.Count > 22 ? fields[22] : null;
                classEntity.ForegroundColor = fields.Count > 23 ? fields[23] : null;
                classEntity.BackgroundColor = fields.Count > 24 ? fields[24] : null;
                classEntity.IsActive = true;

                if (existing == null)
                {
                    _context.Classes.Add(classEntity);
                    result.RecordsImported++;
                }
                else
                {
                    result.RecordsUpdated++;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing classes");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Import subjects from GPU006.TXT
    /// </summary>
    public async Task<ImportResult> ImportSubjectsAsync(Stream fileStream)
    {
        var result = new ImportResult();

        try
        {
            var departments = await _context.Departments.ToListAsync();
            var rooms = await _context.Rooms.ToListAsync();

            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            string? line;
            bool isFirstLine = true;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Skip header if detected
                if (isFirstLine)
                {
                    isFirstLine = false;
                    if (IsHeaderLine(line, new[] { "CODE" }))
                        continue;
                }

                result.RecordsProcessed++;
                var fields = ParseCsvLine(line);
                var csvLineData = string.Join(";", fields);

                if (fields.Count < 2)
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Insufficient fields (need at least 2, got {fields.Count})|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                var code = fields[0];
                if (string.IsNullOrEmpty(code))
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Subject code is empty|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                var existing = await _context.Subjects.FirstOrDefaultAsync(s => s.Code == code);
                var subject = existing ?? new Subject { Code = code };

                subject.Name = fields.Count > 1 ? fields[1] : code;
                subject.Category = fields.Count > 12 ? fields[12] : null; // Subject group

                // Preferred Room - find by name/code
                if (fields.Count > 3 && !string.IsNullOrEmpty(fields[3]))
                {
                    var room = rooms.FirstOrDefault(r => r.RoomNumber == fields[3] || r.Name == fields[3]);
                    subject.PreferredRoomId = room?.Id;
                }

                // Numeric fields
                if (fields.Count > 6 && int.TryParse(fields[6], out int minPerWeek))
                    subject.MinPeriodsPerWeek = minPerWeek;
                if (fields.Count > 7 && int.TryParse(fields[7], out int maxPerWeek))
                    subject.MaxPeriodsPerWeek = maxPerWeek;
                if (fields.Count > 8 && int.TryParse(fields[8], out int minPerDay))
                    subject.MinPeriodsPerDay = minPerDay;
                if (fields.Count > 9 && int.TryParse(fields[9], out int maxPerDay))
                    subject.MaxPeriodsPerDay = maxPerDay;
                if (fields.Count > 10 && int.TryParse(fields[10], out int consecClass))
                    subject.ConsecutivePeriodsClass = consecClass;
                if (fields.Count > 11 && int.TryParse(fields[11], out int consecTeach))
                    subject.ConsecutivePeriodsTeacher = consecTeach;

                // Factor
                if (fields.Count > 13 && decimal.TryParse(fields[13], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal factor))
                    subject.Factor = factor;

                // Department from subject group
                if (!string.IsNullOrEmpty(subject.Category))
                {
                    var dept = departments.FirstOrDefault(d => d.Name == subject.Category);
                    subject.DepartmentId = dept?.Id;
                }

                // Field 16 (index 15): Text - skipped as not in model
                // Field 17 (index 16): Description
                subject.Description = fields.Count > 16 ? fields[16] : null;
                // Field 18 (index 17): Foreground colour
                subject.ForegroundColor = fields.Count > 17 ? fields[17] : null;
                // Field 19 (index 18): Background colour
                subject.BackgroundColor = fields.Count > 18 ? fields[18] : null;
                subject.IsActive = true;

                if (existing == null)
                {
                    _context.Subjects.Add(subject);
                    result.RecordsImported++;
                }
                else
                {
                    result.RecordsUpdated++;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing subjects");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Import rooms from GPU005.TXT
    /// </summary>
    public async Task<ImportResult> ImportRoomsAsync(Stream fileStream)
    {
        var result = new ImportResult();

        try
        {
            var departments = await _context.Departments.ToListAsync();
            var alternativeRoomMappings = new Dictionary<string, string>(); // roomNumber -> altRoomCode

            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            string? line;
            bool isFirstLine = true;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Skip header if detected
                if (isFirstLine)
                {
                    isFirstLine = false;
                    if (IsHeaderLine(line, new[] { "CODE" }))
                        continue;
                }

                result.RecordsProcessed++;
                var fields = ParseCsvLine(line);
                var csvLineData = string.Join(";", fields);

                if (fields.Count < 2)
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Insufficient fields (need at least 2, got {fields.Count})|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                var roomNumber = fields[0];
                if (string.IsNullOrEmpty(roomNumber))
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Room number is empty|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                var existing = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
                var room = existing ?? new Room { RoomNumber = roomNumber };

                room.Name = fields.Count > 1 ? fields[1] : roomNumber;
                room.RoomType = fields.Count > 3 ? fields[3] : null;

                // Store alternative room mapping for later
                if (fields.Count > 2 && !string.IsNullOrEmpty(fields[2]))
                {
                    alternativeRoomMappings[roomNumber] = fields[2];
                }

                // Numeric fields
                if (fields.Count > 6 && int.TryParse(fields[6], out int weight))
                    room.RoomWeight = weight;
                if (fields.Count > 7 && int.TryParse(fields[7], out int capacity))
                    room.Capacity = capacity;

                // Department
                if (fields.Count > 8 && !string.IsNullOrEmpty(fields[8]))
                {
                    var dept = departments.FirstOrDefault(d => d.Name == fields[8]);
                    room.DepartmentId = dept?.Id;
                }

                room.Building = fields.Count > 9 ? fields[9] : null;
                room.Floor = fields.Count > 10 ? fields[10] : null;
                room.Facilities = fields.Count > 11 ? fields[11] : null;
                room.Description = fields.Count > 12 ? fields[12] : null;
                room.ForegroundColor = fields.Count > 13 ? fields[13] : null;
                room.BackgroundColor = fields.Count > 14 ? fields[14] : null;
                room.IsActive = true;

                if (existing == null)
                {
                    _context.Rooms.Add(room);
                    result.RecordsImported++;
                }
                else
                {
                    result.RecordsUpdated++;
                }
            }

            await _context.SaveChangesAsync();

            // Second pass: Set alternative room IDs
            foreach (var mapping in alternativeRoomMappings)
            {
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == mapping.Key);
                var altRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == mapping.Value);

                if (room != null && altRoom != null)
                {
                    room.AlternativeRoomId = altRoom.Id;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing rooms");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Import lessons from GPU002.TXT
    /// </summary>
    public async Task<ImportResult> ImportLessonsAsync(Stream fileStream)
    {
        var result = new ImportResult();

        try
        {
            _logger.LogInformation("Starting lesson import with LessonNumber-based co-teaching detection");

            // Step 1: Parse all lines into memory
            var parsedLines = new List<(int LineNumber, int LessonNumber, List<string> Fields, string ClassName, string TeacherName, string SubjectCode)>();

            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string? line;
                bool isFirstLine = true;
                int lineNumber = 0;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineNumber++;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Skip header if detected
                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        if (IsHeaderLine(line, new[] { "CODE" }))
                            continue;
                    }

                    result.RecordsProcessed++;
                    var fields = ParseCsvLine(line);
                    var csvLineData = string.Join(";", fields);

                    if (fields.Count < 7)
                    {
                        result.Warnings.Add($"Line {lineNumber}: Insufficient fields (need at least 7, got {fields.Count})|LINE:{csvLineData}");
                        result.RecordsSkipped++;
                        continue;
                    }

                    // Parse lesson number
                    if (!int.TryParse(fields[0], out int lessonNumber))
                    {
                        result.Warnings.Add($"Line {lineNumber}: Invalid lesson number '{fields[0]}'|LINE:{csvLineData}");
                        result.RecordsSkipped++;
                        continue;
                    }

                    // Get class, teacher, subject
                    var className = fields[4];
                    var teacherName = fields[5];
                    var subjectCode = fields[6];

                    parsedLines.Add((lineNumber, lessonNumber, fields, className, teacherName, subjectCode));
                }
            }

            _logger.LogInformation("Parsed {Count} lines from GPU002.TXT", parsedLines.Count);

            // Step 2: Load lookups
            var classes = await _context.Classes.ToDictionaryAsync(c => c.Name, c => c);
            var teachers = await _context.Teachers.ToDictionaryAsync(t => t.FirstName, t => t);
            var subjects = await _context.Subjects.ToDictionaryAsync(s => s.Code, s => s);

            // Step 3: Group by LessonNumber to detect co-teaching
            var groupedByLessonNumber = parsedLines
                .GroupBy(p => p.LessonNumber)
                .ToList();

            _logger.LogInformation("Found {Count} unique lesson numbers", groupedByLessonNumber.Count);

            // Step 4: Process each lesson (grouped by LessonNumber)
            int lessonsCreated = 0;
            int lessonsUpdated = 0;
            int coTeachingDetected = 0;
            var coTeachingMerges = new List<(int LessonNumber, int EntriesCount, string Classes, string Teachers, string Subjects)>();

            foreach (var group in groupedByLessonNumber)
            {
                var lessonNumber = group.Key;
                var allEntries = group.ToList();

                // Validate each entry individually and filter out incomplete ones
                var validEntries = new List<(int LineNumber, int LessonNumber, List<string> Fields, string ClassName, string TeacherName, string SubjectCode)>();
                var skippedEntries = new List<(int LineNumber, string Reason)>();

                foreach (var entry in allEntries)
                {
                    bool isValid = true;
                    var reasons = new List<string>();

                    // Check if class exists
                    if (!classes.ContainsKey(entry.ClassName))
                    {
                        reasons.Add($"Class '{entry.ClassName}' not found");
                        isValid = false;
                    }

                    // Check if teacher exists
                    if (!teachers.ContainsKey(entry.TeacherName))
                    {
                        reasons.Add($"Teacher '{entry.TeacherName}' not found");
                        isValid = false;
                    }

                    // Check if subject exists
                    if (!subjects.ContainsKey(entry.SubjectCode))
                    {
                        reasons.Add($"Subject '{entry.SubjectCode}' not found");
                        isValid = false;
                    }

                    if (isValid)
                    {
                        validEntries.Add(entry);
                    }
                    else
                    {
                        skippedEntries.Add((entry.LineNumber, string.Join(", ", reasons)));
                        result.RecordsSkipped++;
                    }
                }

                // If no valid entries remain, skip this lesson number entirely
                if (!validEntries.Any())
                {
                    result.Warnings.Add($"Lesson #{lessonNumber}: All {allEntries.Count} entries are incomplete. Skipping lesson.");
                    foreach (var skipped in skippedEntries)
                    {
                        result.Warnings.Add($"  - Line {skipped.LineNumber}: {skipped.Reason}");
                    }
                    continue;
                }

                // Log warnings for skipped entries (if some entries were valid)
                if (skippedEntries.Any())
                {
                    result.Warnings.Add($"Lesson #{lessonNumber}: Imported {validEntries.Count} valid entries, skipped {skippedEntries.Count} incomplete entries (out of {allEntries.Count} total):");
                    foreach (var skipped in skippedEntries)
                    {
                        result.Warnings.Add($"  - Line {skipped.LineNumber}: {skipped.Reason}");
                    }
                }

                // Collect all unique classes, teachers, and subjects from VALID entries only
                var allClassNames = validEntries.Select(e => e.ClassName).Distinct().ToList();
                var allTeacherNames = validEntries.Select(e => e.TeacherName).Distinct().ToList();
                var allSubjectCodes = validEntries.Select(e => e.SubjectCode).Distinct().ToList();

                // Detect co-teaching (multiple valid entries with same lesson number)
                bool isCoTeaching = validEntries.Count > 1;
                if (isCoTeaching)
                {
                    coTeachingDetected++;

                    var classList = string.Join(" / ", allClassNames);
                    var teacherList = string.Join(" + ", allTeacherNames);
                    var subjectList = string.Join(" / ", allSubjectCodes);

                    coTeachingMerges.Add((
                        LessonNumber: lessonNumber,
                        EntriesCount: validEntries.Count,
                        Classes: classList,
                        Teachers: teacherList,
                        Subjects: subjectList
                    ));

                    _logger.LogInformation(
                        "Co-teaching detected: Lesson #{LessonNumber} - {Count} valid entries merged - Teachers: {Teachers} - Classes: {Classes}",
                        lessonNumber, validEntries.Count, teacherList, classList);
                }

                // Use the first valid entry as the primary data source (or the one with most periods per week)
                var primaryEntry = validEntries
                    .OrderByDescending(e => {
                        int.TryParse(e.Fields.Count > 1 ? e.Fields[1] : "0", out int perWeek);
                        return perWeek;
                    })
                    .ThenBy(e => e.LineNumber)
                    .First();

                var fields = primaryEntry.Fields;

                // Always create new lesson (LessonNumber no longer stored in model)
                var lesson = new Lesson
                {
                    IsActive = true
                };
                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync(); // Save to get Id
                lessonsCreated++;

                // Parse and set all lesson properties from primary entry
                if (fields.Count > 1 && int.TryParse(fields[1], out int perWeek))
                    lesson.FrequencyPerWeek = perWeek;
                if (fields.Count > 2 && int.TryParse(fields[2], out int classPerWeek))
                    lesson.ClassPeriodsPerWeek = classPerWeek;
                if (fields.Count > 3 && int.TryParse(fields[3], out int teachPerWeek))
                    lesson.TeacherPeriodsPerWeek = teachPerWeek;
                if (fields.Count > 9 && int.TryParse(fields[9], out int numStudents))
                    lesson.NumberOfStudents = numStudents;

                // Decimal fields
                if (fields.Count > 10 && decimal.TryParse(fields[10], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal weekValue))
                    lesson.WeekValue = weekValue;
                if (fields.Count > 16 && decimal.TryParse(fields[16], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal yearValue))
                    lesson.YearValue = yearValue;

                lesson.StudentGroup = fields.Count > 11 ? fields[11] : null;

                // Date fields (YYYYMMDD format)
                if (fields.Count > 14 && !string.IsNullOrEmpty(fields[14]) && DateTime.TryParseExact(fields[14], "yyyyMMdd", null, DateTimeStyles.None, out DateTime fromDate))
                    lesson.FromDate = fromDate;
                if (fields.Count > 15 && !string.IsNullOrEmpty(fields[15]) && DateTime.TryParseExact(fields[15], "yyyyMMdd", null, DateTimeStyles.None, out DateTime toDate))
                    lesson.ToDate = toDate;

                // Partition number
                if (fields.Count > 18 && int.TryParse(fields[18], out int partitionNo))
                    lesson.PartitionNumber = partitionNo;

                lesson.HomeRoom = fields.Count > 19 ? fields[19] : null;
                lesson.RequiredRoomType = fields.Count > 7 ? fields[7] : null;
                lesson.Description = fields.Count > 20 ? fields[20] : null;
                lesson.ForegroundColor = fields.Count > 21 ? fields[21] : null;
                lesson.BackgroundColor = fields.Count > 22 ? fields[22] : null;
                lesson.Codes = fields.Count > 23 ? fields[23] : null;

                // Constraints
                if (fields.Count > 24 && int.TryParse(fields[24], out int consecClass))
                    lesson.ConsecutiveSubjectsClass = consecClass;
                if (fields.Count > 25 && int.TryParse(fields[25], out int consecTeach))
                    lesson.ConsecutiveSubjectsTeacher = consecTeach;
                if (fields.Count > 27 && int.TryParse(fields[27], out int minDouble))
                    lesson.MinDoublePeriods = minDouble;
                if (fields.Count > 28 && int.TryParse(fields[28], out int maxDouble))
                    lesson.MaxDoublePeriods = maxDouble;
                if (fields.Count > 29 && int.TryParse(fields[29], out int blockSize))
                    lesson.BlockSize = blockSize;
                if (fields.Count > 31 && int.TryParse(fields[31], out int priority))
                    lesson.Priority = priority;

                // Gender-specific student counts
                if (fields.Count > 33 && int.TryParse(fields[33], out int maleStudents))
                    lesson.MaleStudents = maleStudents;
                if (fields.Count > 34 && int.TryParse(fields[34], out int femaleStudents))
                    lesson.FemaleStudents = femaleStudents;

                // Weekly periods in terms
                lesson.WeeklyPeriodsInTerms = fields.Count > 42 ? fields[42] : null;

                // Create LessonClass junctions for all unique classes
                for (int i = 0; i < allClassNames.Count; i++)
                {
                    var lessonClass = new LessonClass
                    {
                        LessonId = lesson.Id,
                        ClassId = classes[allClassNames[i]].Id,
                        IsPrimary = i == 0,
                        Order = i
                    };
                    _context.LessonClasses.Add(lessonClass);
                }

                // Create LessonTeacher junctions for all unique teachers
                for (int i = 0; i < allTeacherNames.Count; i++)
                {
                    var lessonTeacher = new LessonTeacher
                    {
                        LessonId = lesson.Id,
                        TeacherId = teachers[allTeacherNames[i]].Id,
                        IsLead = i == 0,
                        Order = i,
                        Role = i == 0 ? "Lead Teacher" : "Co-Teacher"
                    };
                    _context.LessonTeachers.Add(lessonTeacher);
                }

                // Create LessonSubject junctions for all unique subjects
                for (int i = 0; i < allSubjectCodes.Count; i++)
                {
                    var lessonSubject = new LessonSubject
                    {
                        LessonId = lesson.Id,
                        SubjectId = subjects[allSubjectCodes[i]].Id,
                        IsPrimary = i == 0,
                        Order = i
                    };
                    _context.LessonSubjects.Add(lessonSubject);
                }

                result.RecordsImported += validEntries.Count;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Import complete: {Created} lessons created, {Updated} lessons updated, {CoTeaching} co-teaching detected",
                lessonsCreated, lessonsUpdated, coTeachingDetected);

            // Separate warnings that were collected during processing
            var processingWarnings = new List<string>(result.Warnings);

            // Build HTML summary
            var summaryHtml = new System.Text.StringBuilder();
            summaryHtml.AppendLine("<div class='import-summary'>");
            summaryHtml.AppendLine("<h5 class='text-primary'>📊 Import Summary - LessonNumber-Based Co-Teaching Detection</h5>");
            summaryHtml.AppendLine("<hr>");

            // Compact statistics section
            summaryHtml.AppendLine("<h6>📊 Statistics:</h6>");
            summaryHtml.AppendLine("<div class='row'>");
            summaryHtml.AppendLine("<div class='col-md-6'>");
            summaryHtml.AppendLine("<ul class='mb-2'>");
            summaryHtml.AppendLine($"<li>Total entries in file: <strong>{parsedLines.Count}</strong></li>");
            summaryHtml.AppendLine($"<li>Unique lesson numbers: <strong>{groupedByLessonNumber.Count}</strong></li>");
            summaryHtml.AppendLine($"<li>Entries imported: <strong class='text-success'>{result.RecordsImported}</strong></li>");
            summaryHtml.AppendLine($"<li>Entries skipped: <strong class='text-warning'>{result.RecordsSkipped}</strong></li>");
            summaryHtml.AppendLine("</ul>");
            summaryHtml.AppendLine("</div>");
            summaryHtml.AppendLine("<div class='col-md-6'>");
            summaryHtml.AppendLine("<ul class='mb-2'>");
            summaryHtml.AppendLine($"<li>Lessons created: <strong class='text-success'>{lessonsCreated}</strong></li>");
            summaryHtml.AppendLine($"<li>Lessons updated: <strong class='text-info'>{lessonsUpdated}</strong></li>");
            summaryHtml.AppendLine($"<li>Regular lessons: <strong>{lessonsCreated + lessonsUpdated - coTeachingDetected}</strong></li>");
            summaryHtml.AppendLine($"<li>Co-teaching lessons: <strong class='text-primary'>{coTeachingDetected}</strong></li>");
            summaryHtml.AppendLine("</ul>");
            summaryHtml.AppendLine("</div>");
            summaryHtml.AppendLine("</div>");

            // Warnings/Errors section (BEFORE co-teaching merges)
            if (processingWarnings.Any())
            {
                summaryHtml.AppendLine("<h6 class='mt-3'>⚠️ Warnings and Skipped Entries:</h6>");
                summaryHtml.AppendLine("<div class='alert alert-warning' style='max-height: 300px; overflow-y: auto;'>");
                summaryHtml.AppendLine("<small>");
                foreach (var warning in processingWarnings)
                {
                    // Escape HTML and preserve formatting
                    var escapedWarning = System.Net.WebUtility.HtmlEncode(warning);
                    summaryHtml.AppendLine($"{escapedWarning}<br/>");
                }
                summaryHtml.AppendLine("</small>");
                summaryHtml.AppendLine("</div>");
            }

            // Co-teaching merge table (AFTER warnings)
            if (coTeachingMerges.Any())
            {
                summaryHtml.AppendLine("<h6 class='mt-3'>👥 Co-Teaching Lessons (Merged by Lesson Number):</h6>");
                summaryHtml.AppendLine($"<p class='text-muted small'>The following {coTeachingMerges.Count} lessons have multiple entries that were merged:</p>");

                summaryHtml.AppendLine("<div class='table-responsive'>");
                summaryHtml.AppendLine("<table class='table table-sm table-bordered table-hover'>");
                summaryHtml.AppendLine("<thead class='table-light'>");
                summaryHtml.AppendLine("<tr>");
                summaryHtml.AppendLine("<th style='width: 12%;'>Lesson #</th>");
                summaryHtml.AppendLine("<th style='width: 10%;'>Entries</th>");
                summaryHtml.AppendLine("<th style='width: 23%;'>Classes</th>");
                summaryHtml.AppendLine("<th style='width: 32%;'>Teachers</th>");
                summaryHtml.AppendLine("<th style='width: 23%;'>Subjects</th>");
                summaryHtml.AppendLine("</tr>");
                summaryHtml.AppendLine("</thead>");
                summaryHtml.AppendLine("<tbody>");

                foreach (var merge in coTeachingMerges.OrderBy(m => m.LessonNumber))
                {
                    summaryHtml.AppendLine("<tr>");
                    summaryHtml.AppendLine($"<td><span class='badge bg-primary'>Lesson #{merge.LessonNumber}</span></td>");
                    summaryHtml.AppendLine($"<td class='text-center'><span class='badge bg-warning text-dark'>{merge.EntriesCount}</span></td>");
                    summaryHtml.AppendLine($"<td><small>{merge.Classes}</small></td>");
                    summaryHtml.AppendLine($"<td><strong><small>{merge.Teachers}</small></strong></td>");
                    summaryHtml.AppendLine($"<td><small>{merge.Subjects}</small></td>");
                    summaryHtml.AppendLine("</tr>");
                }

                summaryHtml.AppendLine("</tbody>");
                summaryHtml.AppendLine("</table>");
                summaryHtml.AppendLine("</div>");
            }

            summaryHtml.AppendLine("</div>");

            // Clear warnings and insert the summary (warnings are now shown in the summary)
            result.Warnings.Clear();
            result.Warnings.Add(summaryHtml.ToString());
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing lessons");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Import complete timetable from GPU001.TXT
    /// This imports already scheduled lessons (not for generation, but for viewing existing timetables)
    /// </summary>
    public async Task<ImportResult> ImportTimetableAsync(Stream fileStream, int timetableId)
    {
        var result = new ImportResult();

        try
        {
            // Verify timetable exists
            var timetable = await _context.Timetables
                .Include(t => t.SchoolYear)
                .FirstOrDefaultAsync(t => t.Id == timetableId);

            if (timetable == null)
            {
                result.Errors.Add("Timetable not found");
                return result;
            }

            // Load lookups
            var lessons = await _context.Lessons
                .Include(l => l.LessonClasses)
                    .ThenInclude(lc => lc.Class)
                .Include(l => l.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
                .Include(l => l.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
                .ToListAsync();

            var rooms = await _context.Rooms.ToDictionaryAsync(r => r.RoomNumber, r => r);
            var periods = await _context.Periods.OrderBy(p => p.PeriodNumber).ToListAsync();

            // Clear existing scheduled lessons for this timetable
            var existingScheduled = await _context.ScheduledLessons
                .Where(sl => sl.TimetableId == timetableId)
                .ToListAsync();
            _context.ScheduledLessons.RemoveRange(existingScheduled);

            // Step 1: Parse all lines from the file
            var parsedEntries = new List<(int LineNumber, int LessonNumber, string ClassName, string TeacherName,
                string SubjectCode, string RoomNumber, int Day, int PeriodNum, string CsvLineData)>();

            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string? line;
                bool isFirstLine = true;
                int lineNumber = 0;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Skip header if detected
                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        if (IsHeaderLine(line, new[] { "CODE" }))
                            continue;
                    }

                    result.RecordsProcessed++;
                    var fields = ParseCsvLine(line);
                    var csvLineData = string.Join(";", fields);

                    if (fields.Count < 7)
                    {
                        result.Warnings.Add($"Line {lineNumber}: Insufficient fields (need at least 7, got {fields.Count})|LINE:{csvLineData}");
                        result.RecordsSkipped++;
                        continue;
                    }

                    // Parse fields: L-No;Class;Teacher;Subject;Room;Day;Period
                    if (!int.TryParse(fields[0], out int lessonNumber) ||
                        !int.TryParse(fields[5], out int day) ||
                        !int.TryParse(fields[6], out int periodNum))
                    {
                        result.Warnings.Add($"Line {lineNumber}: Invalid numeric values for lesson number, day, or period|LINE:{csvLineData}");
                        result.RecordsSkipped++;
                        continue;
                    }

                    parsedEntries.Add((lineNumber, lessonNumber, fields[1], fields[2], fields[3], fields[4], day, periodNum, csvLineData));
                }
            }

            _logger.LogInformation("Parsed {Count} entries from timetable file", parsedEntries.Count);

            // Step 2: Group by (LessonNumber, Day, Period) to detect duplicates
            var groupedEntries = parsedEntries
                .GroupBy(e => new { e.LessonNumber, e.Day, e.PeriodNum })
                .ToList();

            _logger.LogInformation("Found {Count} unique time slots across {Total} entries",
                groupedEntries.Count, parsedEntries.Count);

            // Step 3: Process each unique time slot
            foreach (var group in groupedEntries)
            {
                var firstEntry = group.First();

                // If there are duplicates at this time slot, log a warning
                if (group.Count() > 1)
                {
                    result.Warnings.Add($"Lesson #{firstEntry.LessonNumber}: Found {group.Count()} duplicate entries for Day {firstEntry.Day}, Period {firstEntry.PeriodNum}. Using only the first entry (Line {firstEntry.LineNumber}).");
                }

                // Find matching lesson
                var lesson = lessons.FirstOrDefault(l =>
                    l.LessonClasses.Any(lc => lc.Class != null && lc.Class.Name == firstEntry.ClassName) &&
                    l.LessonTeachers.Any(lt => lt.Teacher != null && lt.Teacher.Name == firstEntry.TeacherName) &&
                    l.LessonSubjects.Any(ls => ls.Subject != null && ls.Subject.Code == firstEntry.SubjectCode));

                if (lesson == null)
                {
                    result.Warnings.Add($"Line {firstEntry.LineNumber}: Lesson not found (L{firstEntry.LessonNumber}, Class '{firstEntry.ClassName}', Teacher '{firstEntry.TeacherName}', Subject '{firstEntry.SubjectCode}'). Please import lessons first.|LINE:{firstEntry.CsvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                // Find period
                var period = periods.FirstOrDefault(p => p.PeriodNumber == firstEntry.PeriodNum);
                if (period == null)
                {
                    result.Warnings.Add($"Line {firstEntry.LineNumber}: Period {firstEntry.PeriodNum} not found. Please import periods first.|LINE:{firstEntry.CsvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                // Parse room(s) - UNTIS may specify multiple rooms separated by space, comma, or semicolon
                var roomNumbers = ParseMultipleRooms(firstEntry.RoomNumber);
                Room? primaryRoom = null;
                List<Room> additionalRooms = new List<Room>();

                if (roomNumbers.Any())
                {
                    // First room becomes the primary (legacy RoomId)
                    if (rooms.TryGetValue(roomNumbers[0], out primaryRoom))
                    {
                        // Additional rooms go into ScheduledLessonRooms
                        for (int i = 1; i < roomNumbers.Count; i++)
                        {
                            if (rooms.TryGetValue(roomNumbers[i], out var additionalRoom))
                            {
                                additionalRooms.Add(additionalRoom);
                            }
                        }
                    }
                }

                // Map day number (1-5) to DayOfWeek (Sunday-Thursday for this school)
                DayOfWeek dayOfWeek = firstEntry.Day switch
                {
                    1 => DayOfWeek.Sunday,
                    2 => DayOfWeek.Monday,
                    3 => DayOfWeek.Tuesday,
                    4 => DayOfWeek.Wednesday,
                    5 => DayOfWeek.Thursday,
                    _ => DayOfWeek.Sunday
                };

                var scheduledLesson = new ScheduledLesson
                {
                    LessonId = lesson.Id,
                    DayOfWeek = dayOfWeek,
                    PeriodId = period.Id,
                    RoomId = primaryRoom?.Id,
                    TimetableId = timetableId
                };

                // Add multi-room assignments
                foreach (var additionalRoom in additionalRooms)
                {
                    scheduledLesson.ScheduledLessonRooms.Add(new ScheduledLessonRoom
                    {
                        RoomId = additionalRoom.Id,
                        ScheduledLesson = scheduledLesson
                    });
                }

                _context.ScheduledLessons.Add(scheduledLesson);

                // Log multi-room imports
                if (additionalRooms.Any())
                {
                    result.Warnings.Add($"Line {firstEntry.LineNumber}: Multi-room lesson imported - {string.Join(", ", roomNumbers)}");
                }

                result.RecordsImported++;
            }

            await _context.SaveChangesAsync();

            // Automatically detect and merge co-teaching after timetable import
            _logger.LogInformation("Running co-teaching detection for timetable {TimetableId}", timetableId);
            var coTeachingResult = await DetectAndMergeCoTeachingAsync(timetableId);

            if (coTeachingResult.Success)
            {
                _logger.LogInformation(
                    "Co-teaching auto-detection: {Merged} lessons merged, {Skipped} skipped",
                    coTeachingResult.RecordsImported,
                    coTeachingResult.RecordsSkipped
                );

                // Add co-teaching results to warnings for visibility
                if (coTeachingResult.RecordsImported > 0)
                {
                    result.Warnings.Add(
                        $"AUTO-DETECTION: Merged {coTeachingResult.RecordsImported} duplicate lessons into co-teaching. " +
                        $"Run detection query to verify."
                    );
                }

                if (coTeachingResult.Warnings.Count > 0)
                {
                    result.Warnings.AddRange(coTeachingResult.Warnings.Select(w => $"CO-TEACHING: {w}"));
                }
            }
            else
            {
                result.Warnings.Add(
                    $"CO-TEACHING DETECTION FAILED: {string.Join(", ", coTeachingResult.Errors)}"
                );
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing timetable");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Detects and merges duplicate lessons that should be co-teaching.
    /// This runs after timetable import to automatically fix lessons created without co-teaching flag.
    /// </summary>
    public async Task<ImportResult> DetectAndMergeCoTeachingAsync(int? timetableId = null)
    {
        var result = new ImportResult();

        try
        {
            _logger.LogInformation("Detecting co-teaching patterns in scheduled lessons");

            // Find duplicate scheduled lessons (same class, subject, day, period, different teachers)
            var scheduledLessons = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonClasses)
                        .ThenInclude(lc => lc.Class)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonTeachers)
                        .ThenInclude(lt => lt.Teacher)
                .Where(sl => timetableId == null || sl.TimetableId == timetableId)
                .ToListAsync();

            // Filter and group in memory due to complex junction table logic
            var duplicateGroups = scheduledLessons
                .Where(sl => {
                    var className = sl.Lesson?.LessonClasses.FirstOrDefault()?.Class?.Name;
                    return className != "Team" && className != "V-RES";
                })
                .Where(sl => sl.Lesson != null && sl.Lesson.LessonTeachers.Count == 1) // Not already co-teaching
                .GroupBy(sl => new
                {
                    sl.DayOfWeek,
                    sl.PeriodId,
                    ClassId = sl.Lesson!.LessonClasses.FirstOrDefault()?.ClassId ?? 0,
                    SubjectId = sl.Lesson!.LessonSubjects.FirstOrDefault()?.SubjectId ?? 0
                })
                .Where(g => g.Select(sl => sl.Lesson!.LessonTeachers.FirstOrDefault()?.TeacherId).Distinct().Count() > 1) // Different teachers
                .ToList();

            _logger.LogInformation("Found {Count} potential co-teaching groups", duplicateGroups.Count);

            foreach (var group in duplicateGroups)
            {
                result.RecordsProcessed++;

                var groupScheduledLessons = group.OrderBy(sl => sl.Id).ToList();
                if (groupScheduledLessons.Count < 2)
                    continue;

                // Get the distinct lessons involved
                var distinctLessons = groupScheduledLessons
                    .Select(sl => sl.Lesson)
                    .GroupBy(l => l.Id)
                    .Select(g => g.First())
                    .OrderBy(l => l.Id)
                    .ToList();

                if (distinctLessons.Count < 2)
                    continue;

                // Keep first lesson, merge others into it as co-teaching
                var primaryLesson = distinctLessons[0];
                var secondaryLessons = distinctLessons.Skip(1).ToList();

                _logger.LogInformation(
                    "Merging co-teaching: Class {Class}, Subject {Subject}, Teachers: {Teachers}",
                    primaryLesson.LessonClasses.FirstOrDefault()?.Class?.Name,
                    primaryLesson.LessonSubjects.FirstOrDefault()?.Subject?.Code,
                    string.Join(" + ", distinctLessons.SelectMany(l => l.LessonTeachers).Select(lt => lt.Teacher?.FirstName))
                );

                // Add teachers from secondary lessons to primary lesson
                foreach (var secondaryLesson in secondaryLessons)
                {
                    // Get teacher from secondary lesson
                    var secondaryTeacher = secondaryLesson.LessonTeachers.FirstOrDefault();
                    if (secondaryTeacher != null)
                    {
                        // Add to primary lesson if not already there
                        if (!primaryLesson.LessonTeachers.Any(lt => lt.TeacherId == secondaryTeacher.TeacherId))
                        {
                            primaryLesson.LessonTeachers.Add(new LessonTeacher
                            {
                                TeacherId = secondaryTeacher.TeacherId,
                                LessonId = primaryLesson.Id
                            });
                        }
                    }
                }

                // Update all scheduled lessons that pointed to secondary lessons
                foreach (var secondaryLesson in secondaryLessons)
                {
                    var scheduledToUpdate = await _context.ScheduledLessons
                        .Where(sl => sl.LessonId == secondaryLesson.Id)
                        .ToListAsync();

                    foreach (var scheduled in scheduledToUpdate)
                    {
                        scheduled.LessonId = primaryLesson.Id;
                    }

                    // Remove the secondary lesson
                    _context.Lessons.Remove(secondaryLesson);
                }

                result.RecordsImported++; // Count as successful merge
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation(
                "Co-teaching detection complete: {Merged} merged, {Skipped} skipped",
                result.RecordsImported,
                result.RecordsSkipped
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting and merging co-teaching");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Import availability data from GPU016.TXT file
    /// Format: Type;ElementName;Day;Period;Importance
    /// Type: L=Teacher, K=Class, R=Room, F=Subject
    /// Importance: -3 (optional/prefer not) to +3 (must have)
    /// </summary>
    public async Task<ImportResult> ImportAvailabilityAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        return await ImportAvailabilityAsync(stream);
    }

    /// <summary>
    /// Import availability data from GPU016.TXT stream (overload for batch import)
    /// </summary>
    public async Task<ImportResult> ImportAvailabilityAsync(Stream stream)
    {
        var result = new ImportResult();

        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Skip header row if detected
            var dataLines = lines.ToList();
            if (dataLines.Count > 0 && IsHeaderLine(dataLines[0], new[] { "CODE" }))
            {
                dataLines = dataLines.Skip(1).ToList();
            }

            // Load all reference data
            var teachers = (await _context.Teachers.ToListAsync())
                .GroupBy(t => t.FirstName)
                .ToDictionary(g => g.Key, g => g.First().Id);
            var classes = (await _context.Classes.ToListAsync())
                .GroupBy(c => c.Name)
                .ToDictionary(g => g.Key, g => g.First().Id);
            var rooms = (await _context.Rooms.ToListAsync())
                .GroupBy(r => r.RoomNumber)
                .ToDictionary(g => g.Key, g => g.First().Id);
            var subjects = (await _context.Subjects.ToListAsync())
                .GroupBy(s => s.Code)
                .ToDictionary(g => g.Key, g => g.First().Id);
            var periods = (await _context.Periods.ToListAsync())
                .GroupBy(p => p.PeriodNumber)
                .ToDictionary(g => g.Key, g => g.First().Id);

            // Clear existing availability data
            _context.TeacherAvailabilities.RemoveRange(_context.TeacherAvailabilities);
            _context.ClassAvailabilities.RemoveRange(_context.ClassAvailabilities);
            _context.RoomAvailabilities.RemoveRange(_context.RoomAvailabilities);
            _context.SubjectAvailabilities.RemoveRange(_context.SubjectAvailabilities);
            await _context.SaveChangesAsync();

            foreach (var line in dataLines)
            {
                result.RecordsProcessed++;
                var fields = ParseCsvLine(line);
                var csvLineData = string.Join(";", fields);

                if (fields.Count < 5)
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Insufficient fields (need at least 5, got {fields.Count})|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                var type = fields[0].Trim();
                var elementName = fields[1].Trim();
                var dayNumber = int.Parse(fields[2].Trim());
                var periodNumber = int.Parse(fields[3].Trim());
                var importance = int.Parse(fields[4].Trim());

                // Convert UNTIS day number (1-5) to DayOfWeek (Sunday-Thursday for this school)
                var dayOfWeek = dayNumber switch
                {
                    1 => DayOfWeek.Sunday,
                    2 => DayOfWeek.Monday,
                    3 => DayOfWeek.Tuesday,
                    4 => DayOfWeek.Wednesday,
                    5 => DayOfWeek.Thursday,
                    _ => DayOfWeek.Sunday
                };

                // Get period ID
                if (!periods.TryGetValue(periodNumber, out var periodId))
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Period {periodNumber} not found|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                switch (type)
                {
                    case "L": // Teacher (Lehrer)
                        if (teachers.TryGetValue(elementName, out var teacherId))
                        {
                            _context.TeacherAvailabilities.Add(new TeacherAvailability
                            {
                                TeacherId = teacherId,
                                DayOfWeek = dayOfWeek,
                                PeriodId = periodId,
                                Importance = importance
                            });
                            result.RecordsImported++;
                        }
                        else
                        {
                            result.Warnings.Add($"Line {result.RecordsProcessed}: Teacher '{elementName}' not found. Please import teachers first.|LINE:{csvLineData}");
                            result.RecordsSkipped++;
                        }
                        break;

                    case "K": // Class (Klasse)
                        if (classes.TryGetValue(elementName, out var classId))
                        {
                            _context.ClassAvailabilities.Add(new ClassAvailability
                            {
                                ClassId = classId,
                                DayOfWeek = dayOfWeek,
                                PeriodId = periodId,
                                Importance = importance
                            });
                            result.RecordsImported++;
                        }
                        else
                        {
                            result.Warnings.Add($"Line {result.RecordsProcessed}: Class '{elementName}' not found. Please import classes first.|LINE:{csvLineData}");
                            result.RecordsSkipped++;
                        }
                        break;

                    case "R": // Room (Raum)
                        if (rooms.TryGetValue(elementName, out var roomId))
                        {
                            _context.RoomAvailabilities.Add(new RoomAvailability
                            {
                                RoomId = roomId,
                                DayOfWeek = dayOfWeek,
                                PeriodId = periodId,
                                Importance = importance
                            });
                            result.RecordsImported++;
                        }
                        else
                        {
                            result.Warnings.Add($"Line {result.RecordsProcessed}: Room '{elementName}' not found. Please import rooms first.|LINE:{csvLineData}");
                            result.RecordsSkipped++;
                        }
                        break;

                    case "F": // Subject (Fach)
                        if (subjects.TryGetValue(elementName, out var subjectId))
                        {
                            _context.SubjectAvailabilities.Add(new SubjectAvailability
                            {
                                SubjectId = subjectId,
                                DayOfWeek = dayOfWeek,
                                PeriodId = periodId,
                                Importance = importance
                            });
                            result.RecordsImported++;
                        }
                        else
                        {
                            result.Warnings.Add($"Line {result.RecordsProcessed}: Subject '{elementName}' not found. Please import subjects first.|LINE:{csvLineData}");
                            result.RecordsSkipped++;
                        }
                        break;

                    default:
                        result.Warnings.Add($"Line {result.RecordsProcessed}: Unknown type '{type}'. Expected L (Teacher), K (Class), R (Room), or F (Subject).|LINE:{csvLineData}");
                        result.RecordsSkipped++;
                        break;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing availability data");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Import students from GPU010.TXT
    /// </summary>
    public async Task<ImportResult> ImportStudentsAsync(Stream fileStream)
    {
        var result = new ImportResult();

        try
        {
            // Load classes for reference
            var classes = await _context.Classes.ToListAsync();

            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            string? line;
            bool isFirstLine = true;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Skip header if detected
                if (isFirstLine)
                {
                    isFirstLine = false;
                    if (IsHeaderLine(line, new[] { "CODE" }))
                        continue;
                }

                result.RecordsProcessed++;
                var fields = ParseCsvLine(line);
                var csvLineData = string.Join(";", fields);

                // GPU010 requires at least fields 1-2 (Name, Full Name)
                if (fields.Count < 2)
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Insufficient fields (need at least 2, got {fields.Count})|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                // Field 1: Name (Abbreviated short name)
                var name = fields[0];
                if (string.IsNullOrEmpty(name))
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Student name is empty|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                // Field 9: Student Number (unique identifier)
                var studentNumber = fields.Count > 8 ? fields[8] : name;
                if (string.IsNullOrEmpty(studentNumber))
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Missing student number, using name '{name}'|LINE:{csvLineData}");
                    studentNumber = name;
                }

                var existing = await _context.Students.FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);

                var student = existing ?? new Student { StudentNumber = studentNumber };

                // GPU010 Field mapping
                student.Name = name; // Field 1: Short name
                student.FullName = fields.Count > 1 ? fields[1] : name; // Field 2: Full Name (Last Name)
                student.Text = fields.Count > 2 ? fields[2] : null; // Field 3
                student.Description = fields.Count > 3 ? fields[3] : null; // Field 4
                student.Statistic1 = fields.Count > 4 ? fields[4] : null; // Field 5
                student.Statistic2 = fields.Count > 5 ? fields[5] : null; // Field 6
                student.Code = fields.Count > 6 ? fields[6] : null; // Field 7
                student.FirstName = fields.Count > 7 ? fields[7] : null; // Field 8

                // Field 10: Class
                if (fields.Count > 9 && !string.IsNullOrEmpty(fields[9]))
                {
                    var className = fields[9];
                    var classObj = classes.FirstOrDefault(c => c.Name == className);
                    if (classObj != null)
                    {
                        student.ClassId = classObj.Id;
                    }
                    else
                    {
                        result.Warnings.Add($"Line {result.RecordsProcessed}: Class '{className}' not found. Please import classes first.|LINE:{csvLineData}");
                    }
                }

                // Field 11: Gender (1 = female, 2 = male)
                if (fields.Count > 10 && int.TryParse(fields[10], out int gender))
                {
                    student.Gender = gender;
                }

                // Field 12: (Course-) Optimisation Code
                student.OptimisationCode = fields.Count > 11 ? fields[11] : null;

                // Field 13: Date of birth (YYYYMMDD format)
                if (fields.Count > 12 && !string.IsNullOrEmpty(fields[12]))
                {
                    if (DateTime.TryParseExact(fields[12], "yyyyMMdd", null, DateTimeStyles.None, out DateTime dob))
                    {
                        student.DateOfBirth = dob;
                    }
                    else
                    {
                        result.Warnings.Add($"Line {result.RecordsProcessed}: Invalid date format '{fields[12]}' (expected YYYYMMDD)");
                    }
                }

                // Field 14: Email address
                student.Email = fields.Count > 13 ? fields[13] : null;

                student.IsActive = true;

                if (existing == null)
                {
                    _context.Students.Add(student);
                    result.RecordsImported++;
                }
                else
                {
                    result.RecordsUpdated++;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Student import completed: {Imported} imported, {Updated} updated, {Skipped} skipped",
                result.RecordsImported, result.RecordsUpdated, result.RecordsSkipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing students from GPU010.TXT");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Combined import of lessons and schedule with automatic co-teaching detection.
    /// This method processes GPU002.TXT (lessons) and GPU001.TXT (schedule) together,
    /// detecting co-teaching scenarios where multiple teachers/subjects share the same time slot.
    /// </summary>
    /// <param name="lessonsStream">GPU002.TXT file stream</param>
    /// <param name="scheduleStream">GPU001.TXT file stream</param>
    /// <param name="timetableId">Target timetable ID</param>
    /// <returns>Import result with statistics</returns>
    //public async Task<ImportResult> ImportScheduleWithCoTeachingDetectionAsync(
    //    Stream lessonsStream,
    //    Stream scheduleStream,
    //    int timetableId)
    //{
    //    var result = new ImportResult();

    //    try
    //    {
    //        _logger.LogInformation("Starting combined import with co-teaching detection for timetable {TimetableId}", timetableId);

    //        // Verify timetable exists
    //        var timetable = await _context.Timetables
    //            .Include(t => t.SchoolYear)
    //            .FirstOrDefaultAsync(t => t.Id == timetableId);

    //        if (timetable == null)
    //        {
    //            result.Errors.Add($"Timetable with ID {timetableId} not found");
    //            return result;
    //        }

    //        // Step 1: Parse GPU002.TXT (Lesson Definitions) into memory
    //        _logger.LogInformation("Step 1: Parsing lesson definitions (GPU002.TXT)");
    //        var lessonDefinitions = await ParseLessonDefinitionsAsync(lessonsStream);
    //        _logger.LogInformation("Parsed {Count} lesson definitions", lessonDefinitions.Count);

    //        // Step 2: Parse GPU001.TXT (Schedule) into memory
    //        _logger.LogInformation("Step 2: Parsing schedule entries (GPU001.TXT)");
    //        var scheduleEntries = await ParseScheduleEntriesAsync(scheduleStream);
    //        _logger.LogInformation("Parsed {Count} schedule entries", scheduleEntries.Count);

    //        // Step 3: Detect co-teaching by grouping schedule entries by (Class, Day, Period)
    //        _logger.LogInformation("Step 3: Detecting co-teaching scenarios");
    //        var detectedLessons = DetectCoTeachingLessons(scheduleEntries, lessonDefinitions);
    //        _logger.LogInformation("Detected {Total} unique lessons ({CoTeaching} with co-teaching, {MultiSubject} multi-subject, {MultiRoom} multi-room)",
    //            detectedLessons.Count,
    //            detectedLessons.Count(l => l.IsCoTeaching),
    //            detectedLessons.Count(l => l.IsMultiSubject),
    //            detectedLessons.Count(l => l.IsMultiRoom));

    //        // Step 4: Load lookups from database
    //        _logger.LogInformation("Step 4: Loading database lookups");
    //        var classes = (await _context.Classes.ToListAsync())
    //            .GroupBy(c => c.Name)
    //            .ToDictionary(g => g.Key, g => g.First());
    //        var teachers = (await _context.Teachers.ToListAsync())
    //            .GroupBy(t => t.FirstName)
    //            .ToDictionary(g => g.Key, g => g.First());
    //        var subjects = (await _context.Subjects.ToListAsync())
    //            .GroupBy(s => s.Code)
    //            .ToDictionary(g => g.Key, g => g.First());
    //        var rooms = (await _context.Rooms.ToListAsync())
    //            .GroupBy(r => r.RoomNumber)
    //            .ToDictionary(g => g.Key, g => g.First());
    //        var periods = (await _context.Periods.ToListAsync())
    //            .GroupBy(p => p.PeriodNumber)
    //            .ToDictionary(g => g.Key, g => g.First());

    //        // Step 5: Clear existing scheduled lessons for this timetable
    //        var existingScheduled = await _context.ScheduledLessons
    //            .Where(sl => sl.TimetableId == timetableId)
    //            .ToListAsync();
    //        _context.ScheduledLessons.RemoveRange(existingScheduled);
    //        _logger.LogInformation("Cleared {Count} existing scheduled lessons", existingScheduled.Count);

    //        // Step 6: Group detected lessons by LessonNumber to avoid creating duplicate Lesson entities
    //        // A lesson can appear in multiple time slots, but should only have ONE Lesson entity
    //        _logger.LogInformation("Step 5: Grouping lessons by LessonNumber to avoid duplicates");

    //        var lessonsByNumber = detectedLessons
    //            .SelectMany(dl => dl.SourceDefinitions.Select(def => new { DetectedLesson = dl, Definition = def }))
    //            .GroupBy(x => x.Definition.LessonNumber)
    //            .Select(g => new
    //            {
    //                LessonNumber = g.Key,
    //                PrimaryDefinition = g.First().Definition,
    //                TimeSlots = g.Select(x => x.DetectedLesson).Distinct().ToList()
    //            })
    //            .ToList();

    //        _logger.LogInformation("Found {Count} unique lesson numbers across {Slots} time slots",
    //            lessonsByNumber.Count, detectedLessons.Count);

    //        // Step 7: Create Lesson entities and ScheduledLesson entries
    //        int lessonsCreated = 0;
    //        int scheduledLessonsCreated = 0;
    //        var lessonIdsByNumber = new Dictionary<int, int>(); // LessonNumber -> Lesson.Id

    //        foreach (var lessonGroup in lessonsByNumber)
    //        {
    //            result.RecordsProcessed++;

    //            // Collect all unique classes, teachers, subjects from ALL time slots where this lesson appears
    //            var allClasses = lessonGroup.TimeSlots.Select(dl => dl.ClassName).Distinct().ToList();
    //            var allTeachers = lessonGroup.TimeSlots.SelectMany(dl => dl.TeacherNames).Distinct().ToList();
    //            var allSubjects = lessonGroup.TimeSlots.SelectMany(dl => dl.SubjectCodes).Distinct().ToList();

    //            // Validate all references exist
    //            var missingClasses = allClasses.Where(c => !classes.ContainsKey(c)).ToList();
    //            if (missingClasses.Any())
    //            {
    //                result.Warnings.Add($"Classes not found for Lesson #{lessonGroup.LessonNumber}: {string.Join(", ", missingClasses)}");
    //                result.RecordsSkipped++;
    //                continue;
    //            }

    //            var missingTeachers = allTeachers.Where(t => !teachers.ContainsKey(t)).ToList();
    //            if (missingTeachers.Any())
    //            {
    //                result.Warnings.Add($"Teachers not found for Lesson #{lessonGroup.LessonNumber}: {string.Join(", ", missingTeachers)}");
    //                result.RecordsSkipped++;
    //                continue;
    //            }

    //            var missingSubjects = allSubjects.Where(s => !subjects.ContainsKey(s)).ToList();
    //            if (missingSubjects.Any())
    //            {
    //                result.Warnings.Add($"Subjects not found for Lesson #{lessonGroup.LessonNumber}: {string.Join(", ", missingSubjects)}");
    //                result.RecordsSkipped++;
    //                continue;
    //            }

    //            // Validate periods for all time slots
    //            var missingPeriods = lessonGroup.TimeSlots
    //                .Select(dl => dl.PeriodNumber)
    //                .Distinct()
    //                .Where(p => !periods.ContainsKey(p))
    //                .ToList();

    //            if (missingPeriods.Any())
    //            {
    //                result.Warnings.Add($"Periods not found for Lesson #{lessonGroup.LessonNumber}: {string.Join(", ", missingPeriods)}");
    //                result.RecordsSkipped++;
    //                continue;
    //            }

    //            // Create ONE Lesson entity for this LessonNumber
    //            var primaryDef = lessonGroup.PrimaryDefinition;
    //            var lesson = new Models.Lesson
    //            {
    //                LessonNumber = lessonGroup.LessonNumber,
    //                Duration = 1,
    //                FrequencyPerWeek = primaryDef.PeriodsPerWeek,
    //                ClassPeriodsPerWeek = primaryDef.ClassPeriodsPerWeek,
    //                TeacherPeriodsPerWeek = primaryDef.TeacherPeriodsPerWeek,
    //                NumberOfStudents = primaryDef.NumberOfStudents,
    //                MaleStudents = primaryDef.MaleStudents,
    //                FemaleStudents = primaryDef.FemaleStudents,
    //                WeekValue = primaryDef.WeekValue,
    //                YearValue = primaryDef.YearValue,
    //                FromDate = primaryDef.FromDate,
    //                ToDate = primaryDef.ToDate,
    //                PartitionNumber = primaryDef.PartitionNumber,
    //                StudentGroup = primaryDef.StudentGroup,
    //                HomeRoom = primaryDef.HomeRoom,
    //                RequiredRoomType = primaryDef.RequiredRoomType,
    //                MinDoublePeriods = primaryDef.MinDoublePeriods,
    //                MaxDoublePeriods = primaryDef.MaxDoublePeriods,
    //                BlockSize = primaryDef.BlockSize,
    //                Priority = primaryDef.Priority,
    //                ConsecutiveSubjectsClass = primaryDef.ConsecutiveSubjectsClass,
    //                ConsecutiveSubjectsTeacher = primaryDef.ConsecutiveSubjectsTeacher,
    //                Codes = primaryDef.Codes,
    //                Description = primaryDef.Description,
    //                ForegroundColor = primaryDef.ForegroundColor,
    //                BackgroundColor = primaryDef.BackgroundColor,
    //                WeeklyPeriodsInTerms = primaryDef.WeeklyPeriodsInTerms,
    //                IsActive = true
    //            };

    //            _context.Lessons.Add(lesson);
    //            await _context.SaveChangesAsync(); // Save to get Lesson.Id
    //            lessonsCreated++;
    //            lessonIdsByNumber[lessonGroup.LessonNumber] = lesson.Id;

    //            // Create LessonClass junctions for ALL classes where this lesson appears
    //            for (int i = 0; i < allClasses.Count; i++)
    //            {
    //                var classId = classes[allClasses[i]].Id;
    //                var lessonClass = new Models.LessonClass
    //                {
    //                    LessonId = lesson.Id,
    //                    ClassId = classId,
    //                    IsPrimary = i == 0,
    //                    Order = i
    //                };
    //                _context.LessonClasses.Add(lessonClass);
    //            }

    //            // Create LessonSubject junctions for ALL subjects
    //            for (int i = 0; i < allSubjects.Count; i++)
    //            {
    //                var subjectId = subjects[allSubjects[i]].Id;
    //                var lessonSubject = new Models.LessonSubject
    //                {
    //                    LessonId = lesson.Id,
    //                    SubjectId = subjectId,
    //                    IsPrimary = i == 0,
    //                    Order = i
    //                };
    //                _context.LessonSubjects.Add(lessonSubject);
    //            }

    //            // Create LessonTeacher junctions for ALL teachers
    //            for (int i = 0; i < allTeachers.Count; i++)
    //            {
    //                var teacherId = teachers[allTeachers[i]].Id;
    //                var lessonTeacher = new Models.LessonTeacher
    //                {
    //                    LessonId = lesson.Id,
    //                    TeacherId = teacherId,
    //                    IsLead = i == 0,
    //                    Order = i,
    //                    Role = i == 0 ? "Lead Teacher" : "Co-Teacher"
    //                };
    //                _context.LessonTeachers.Add(lessonTeacher);
    //            }

    //            // Deduplicate time slots: A lesson can't be scheduled more than once on the same day/period
    //            // If there are multiple rows with same day+period, take only the first one
    //            var uniqueTimeSlots = lessonGroup.TimeSlots
    //                .GroupBy(ts => new { ts.DayNumber, ts.PeriodNumber })
    //                .Select(g =>
    //                {
    //                    if (g.Count() > 1)
    //                    {
    //                        result.Warnings.Add($"Lesson #{lessonGroup.LessonNumber}: Found {g.Count()} duplicate entries for Day {g.Key.DayNumber}, Period {g.Key.PeriodNumber}. Using only the first entry.");
    //                    }
    //                    return g.First();
    //                })
    //                .ToList();

    //            // Create ScheduledLesson entries for EACH unique time slot where this lesson appears
    //            foreach (var timeSlot in uniqueTimeSlots)
    //            {
    //                // Convert day number to DayOfWeek
    //                DayOfWeek dayOfWeek = timeSlot.DayNumber switch
    //                {
    //                    1 => DayOfWeek.Sunday,
    //                    2 => DayOfWeek.Monday,
    //                    3 => DayOfWeek.Tuesday,
    //                    4 => DayOfWeek.Wednesday,
    //                    5 => DayOfWeek.Thursday,
    //                    _ => DayOfWeek.Sunday
    //                };

    //                // Get room IDs for this specific time slot
    //                var validRoomNumbers = timeSlot.RoomNumbers
    //                    .Where(r => !string.IsNullOrWhiteSpace(r) && rooms.ContainsKey(r))
    //                    .Distinct()
    //                    .ToList();

    //                var primaryRoomId = validRoomNumbers.Any() && rooms.ContainsKey(validRoomNumbers[0])
    //                    ? rooms[validRoomNumbers[0]].Id
    //                    : (int?)null;

    //                // Create ONE ScheduledLesson for this time slot
    //                var scheduledLesson = new Models.ScheduledLesson
    //                {
    //                    LessonId = lesson.Id,
    //                    DayOfWeek = dayOfWeek,
    //                    PeriodId = periods[timeSlot.PeriodNumber].Id,
    //                    RoomId = primaryRoomId,
    //                    TimetableId = timetableId
    //                };

    //                _context.ScheduledLessons.Add(scheduledLesson);
    //                await _context.SaveChangesAsync(); // Save to get ScheduledLesson.Id
    //                scheduledLessonsCreated++;

    //                // Create ScheduledLessonRoom entries for all rooms in this time slot
    //                for (int i = 0; i < validRoomNumbers.Count; i++)
    //                {
    //                    var roomId = rooms[validRoomNumbers[i]].Id;
    //                    var scheduledLessonRoom = new Models.ScheduledLessonRoom
    //                    {
    //                        ScheduledLessonId = scheduledLesson.Id,
    //                        RoomId = roomId,
    //                        PrimaryTeacherIdForRoom = i < allTeachers.Count
    //                            ? teachers[allTeachers[i]].Id
    //                            : (int?)null
    //                    };
    //                    _context.ScheduledLessonRooms.Add(scheduledLessonRoom);
    //                }
    //            }

    //            result.RecordsImported++;
    //        }

    //        await _context.SaveChangesAsync();

    //        _logger.LogInformation("Import complete: {Lessons} lessons created, {Scheduled} scheduled entries created",
    //            lessonsCreated, scheduledLessonsCreated);

    //        result.Success = true;

    //        // Add detailed summary of co-teaching detection
    //        var coTeachingCount = detectedLessons.Count(l => l.IsCoTeaching);
    //        var multiSubjectCount = detectedLessons.Count(l => l.IsMultiSubject);
    //        var multiRoomCount = detectedLessons.Count(l => l.IsMultiRoom);
    //        var maxTeachers = detectedLessons.Any() ? detectedLessons.Max(l => l.TeacherNames.Count) : 0;
    //        var regularLessons = detectedLessons.Count - coTeachingCount;

    //        // Calculate teacher distribution
    //        var teacherDistribution = detectedLessons
    //            .GroupBy(l => l.TeacherNames.Count)
    //            .OrderBy(g => g.Key)
    //            .Select(g => $"{g.Count()} lessons with {g.Key} teacher{(g.Key > 1 ? "s" : "")}")
    //            .ToList();

    //        // Calculate subject distribution
    //        var subjectDistribution = detectedLessons
    //            .GroupBy(l => l.SubjectCodes.Distinct().Count())
    //            .OrderBy(g => g.Key)
    //            .Select(g => $"{g.Count()} lessons with {g.Key} subject{(g.Key > 1 ? "s" : "")}")
    //            .ToList();

    //        // Build comprehensive summary
    //        var summaryLines = new List<string>
    //        {
    //            "═══════════════════════════════════════════════════════════",
    //            "📊 IMPORT SUMMARY - Schedule with Co-Teaching Detection",
    //            "═══════════════════════════════════════════════════════════",
    //            "",
    //            "📥 INPUT FILES:",
    //            $"   • Lesson definitions (GPU002.TXT): {lessonDefinitions.Count} entries",
    //            $"   • Schedule entries (GPU001.TXT): {scheduleEntries.Count} entries",
    //            "",
    //            "✅ PROCESSING RESULTS:",
    //            $"   • Unique lessons created: {lessonsCreated}",
    //            $"   • Scheduled entries created: {scheduledLessonsCreated}",
    //            $"   • Records processed: {result.RecordsProcessed}",
    //            $"   • Records imported: {result.RecordsImported}",
    //            $"   • Records skipped: {result.RecordsSkipped}",
    //            "",
    //            "👥 CO-TEACHING ANALYSIS:",
    //            $"   • Regular lessons (1 teacher): {regularLessons}",
    //            $"   • Co-teaching lessons (2+ teachers): {coTeachingCount}",
    //            $"   • Maximum teachers in one lesson: {maxTeachers}",
    //            "",
    //            "📚 LESSON BREAKDOWN BY TEACHERS:"
    //        };
    //        summaryLines.AddRange(teacherDistribution.Select(td => $"   • {td}"));

    //        summaryLines.Add("");
    //        summaryLines.Add("📖 LESSON BREAKDOWN BY SUBJECTS:");
    //        summaryLines.AddRange(subjectDistribution.Select(sd => $"   • {sd}"));

    //        if (multiSubjectCount > 0)
    //        {
    //            summaryLines.Add("");
    //            summaryLines.Add($"🔀 MULTI-SUBJECT LESSONS: {multiSubjectCount}");
    //        }

    //        if (multiRoomCount > 0)
    //        {
    //            summaryLines.Add("");
    //            summaryLines.Add($"🏫 MULTI-ROOM LESSONS: {multiRoomCount}");
    //        }

    //        // Add co-teaching examples
    //        if (coTeachingCount > 0)
    //        {
    //            summaryLines.Add("");
    //            summaryLines.Add($"👥 CO-TEACHING EXAMPLES (showing {Math.Min(5, coTeachingCount)} of {coTeachingCount}):");

    //            var exampleCoTeaching = detectedLessons
    //                .Where(l => l.IsCoTeaching)
    //                .Take(5)
    //                .Select(l => $"   • {l.ClassName} @ Day {l.DayNumber}/Period {l.PeriodNumber}: " +
    //                            $"{string.Join(" + ", l.TeacherNames)} → {string.Join("/", l.SubjectCodes.Distinct())}")
    //                .ToList();

    //            summaryLines.AddRange(exampleCoTeaching);
    //        }

    //        summaryLines.Add("");
    //        summaryLines.Add("═══════════════════════════════════════════════════════════");

    //        // Add all summary lines to warnings for visibility
    //        result.Warnings.Insert(0, string.Join("\n", summaryLines));
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error during combined import with co-teaching detection");
    //        result.Errors.Add($"Error: {ex.Message}");
    //    }

    //    return result;
    //}
    /*

    /// <summary>
    /// Simplified import with optional conflict-based co-teaching detection.
    /// Detects conflicts by grouping lessons with same Class + Day + Period.
    /// </summary>
    /// <param name="enableCoTeachingDetection">When true, merges conflicting lessons into co-teaching. When false, imports each lesson separately.</param>
    public async Task<ImportResult> ImportScheduleWithConflictDetectionAsync(
        Stream lessonsStream,
        Stream scheduleStream,
        int timetableId,
        bool enableCoTeachingDetection = true)
    {
        var result = new ImportResult();

        try
        {
            _logger.LogInformation("Starting simplified import with conflict-based co-teaching detection for timetable {TimetableId}", timetableId);

            // Step 1: Parse both files into memory
            _logger.LogInformation("Step 1: Parsing lesson definitions (GPU002.TXT)");
            var lessonDefinitions = await ParseLessonDefinitionsAsync(lessonsStream);
            _logger.LogInformation("Parsed {Count} lesson definitions", lessonDefinitions.Count);

            _logger.LogInformation("Step 2: Parsing schedule entries (GPU001.TXT)");
            var scheduleEntries = await ParseScheduleEntriesAsync(scheduleStream);
            _logger.LogInformation("Parsed {Count} schedule entries", scheduleEntries.Count);

            // Step 2: Create a lookup dictionary for lesson definitions by lesson number
            var definitionsByLessonNumber = lessonDefinitions
                .GroupBy(d => d.LessonNumber)
                .ToDictionary(g => g.Key, g => g.First());

            // Step 3: Combine schedule entries with their lesson definitions
            var combinedLessons = scheduleEntries
                .Where(se => definitionsByLessonNumber.ContainsKey(se.LessonNumber))
                .Select(se => new UntisImportModels.CombinedLessonData
                {
                    ScheduleEntry = se,
                    Definition = definitionsByLessonNumber[se.LessonNumber]
                })
                .ToList();

            _logger.LogInformation("Combined {Count} schedule entries with lesson definitions", combinedLessons.Count);

            // Step 4: Group by LessonNumber to detect co-teaching OR process individually
            List<IGrouping<string, UntisImportModels.CombinedLessonData>> groupedByLessonNumber;

            if (enableCoTeachingDetection)
            {
                // Group by LessonNumber to detect and merge co-teaching lessons
                groupedByLessonNumber = combinedLessons
                    .GroupBy(cl => cl.Definition.LessonNumber.ToString())
                    .ToList();
                var lessonCount = groupedByLessonNumber.Count;
                _logger.LogInformation("Co-teaching detection ENABLED (by LessonNumber). Found {Count} unique lesson numbers", lessonCount);
            }
            else
            {
                // Each schedule entry becomes its own group (no merging)
                groupedByLessonNumber = combinedLessons
                    .Select((cl, index) => new { Key = $"{cl.Definition.LessonNumber}|{index}", Value = cl })
                    .GroupBy(x => x.Key, x => x.Value)
                    .ToList();
                var individualCount = groupedByLessonNumber.Count;
                _logger.LogInformation("Co-teaching detection DISABLED. Processing {Count} lessons individually", individualCount);
            }

            // Step 5: Load database lookups
            _logger.LogInformation("Step 5: Loading database lookups");
            var classes = (await _context.Classes.ToListAsync())
                .GroupBy(c => c.Name)
                .ToDictionary(g => g.Key, g => g.First());
            var teachers = (await _context.Teachers.ToListAsync())
                .GroupBy(t => t.FirstName)
                .ToDictionary(g => g.Key, g => g.First());
            var subjects = (await _context.Subjects.ToListAsync())
                .GroupBy(s => s.Code)
                .ToDictionary(g => g.Key, g => g.First());
            var rooms = (await _context.Rooms.ToListAsync())
                .GroupBy(r => r.RoomNumber)
                .ToDictionary(g => g.Key, g => g.First());
            var periods = (await _context.Periods.ToListAsync())
                .GroupBy(p => p.PeriodNumber)
                .ToDictionary(g => g.Key, g => g.First());

            // Step 6: Clear existing scheduled lessons for this timetable
            var existingScheduled = await _context.ScheduledLessons
                .Where(sl => sl.TimetableId == timetableId)
                .ToListAsync();
            _context.ScheduledLessons.RemoveRange(existingScheduled);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleared {Count} existing scheduled lessons", existingScheduled.Count);

            // Step 7: Process each lesson (grouped by LessonNumber)
            int lessonsCreated = 0;
            int scheduledLessonsCreated = 0;
            int coTeachingDetected = 0;
            var coTeachingMerges = new List<(int LessonNumber, int ScheduleEntriesCount, string Classes, string Teachers, string Subjects, string ScheduleSlots)>(); // Track detailed co-teaching merge info

            foreach (var group in groupedByLessonNumber)
            {
                result.RecordsProcessed++;
                var scheduleEntriesForLesson = group.ToList();

                // Get the lesson number from the group
                var lessonNumber = scheduleEntriesForLesson.First().Definition.LessonNumber;

                // Collect all unique classes, teachers, subjects, and rooms from all schedule entries for this lesson
                var allClassNames = scheduleEntriesForLesson
                    .Select(l => l.ScheduleEntry.ClassName)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .ToList();

                var allTeacherNames = scheduleEntriesForLesson
                    .Select(l => l.Definition.TeacherName)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .ToList();

                var allSubjectCodes = scheduleEntriesForLesson
                    .Select(l => l.Definition.SubjectCode)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .ToList();

                // Validate all classes exist
                var missingClasses = allClassNames.Where(c => !classes.ContainsKey(c)).ToList();
                if (missingClasses.Any())
                {
                    result.Warnings.Add($"Classes not found: {string.Join(", ", missingClasses)} for Lesson #{lessonNumber}");
                    result.RecordsSkipped++;
                    continue;
                }

                // Validate all teachers exist
                var missingTeachers = allTeacherNames.Where(t => !teachers.ContainsKey(t)).ToList();
                if (missingTeachers.Any())
                {
                    result.Warnings.Add($"Teachers not found: {string.Join(", ", missingTeachers)} for Lesson #{lessonNumber}");
                    result.RecordsSkipped++;
                    continue;
                }

                // Validate all subjects exist
                var missingSubjects = allSubjectCodes.Where(s => !subjects.ContainsKey(s)).ToList();
                if (missingSubjects.Any())
                {
                    result.Warnings.Add($"Subjects not found: {string.Join(", ", missingSubjects)} for Lesson #{lessonNumber}");
                    result.RecordsSkipped++;
                    continue;
                }

                // Determine if this is co-teaching (multiple schedule entries with same lesson number)
                bool isCoTeaching = scheduleEntriesForLesson.Count > 1;
                if (isCoTeaching)
                {
                    coTeachingDetected++;

                    // Collect detailed merge information
                    var classList = string.Join(" / ", allClassNames);
                    var teacherList = string.Join(" + ", allTeacherNames);
                    var subjectList = string.Join(" / ", allSubjectCodes);

                    var scheduleSlots = string.Join(", ", scheduleEntriesForLesson
                        .Select(se => {
                            var dayName = se.ScheduleEntry.DayNumber switch
                            {
                                1 => "Sun",
                                2 => "Mon",
                                3 => "Tue",
                                4 => "Wed",
                                5 => "Thu",
                                _ => $"Day{se.ScheduleEntry.DayNumber}"
                            };
                            return $"{se.ScheduleEntry.ClassName} {dayName}P{se.ScheduleEntry.PeriodNumber}";
                        })
                        .Distinct());

                    // Add to structured list for table display
                    coTeachingMerges.Add((
                        LessonNumber: lessonNumber,
                        ScheduleEntriesCount: scheduleEntriesForLesson.Count,
                        Classes: classList,
                        Teachers: teacherList,
                        Subjects: subjectList,
                        ScheduleSlots: scheduleSlots
                    ));

                    _logger.LogInformation(
                        "Co-teaching detected: Lesson #{LessonNumber} - {Count} schedule entries merged - Teachers: {Teachers} - Classes: {Classes}",
                        lessonNumber, scheduleEntriesForLesson.Count,
                        teacherList, classList);
                }

                // Get primary definition (use first one or one with most periods per week)
                var primaryDef = scheduleEntriesForLesson
                    .Select(l => l.Definition)
                    .OrderByDescending(d => d.PeriodsPerWeek)
                    .ThenBy(d => d.LessonNumber)
                    .First();

                // Create merged Lesson entity
                var lesson = new Models.Lesson
                {
                    LessonNumber = primaryDef.LessonNumber,
                    Duration = 1,
                    FrequencyPerWeek = primaryDef.PeriodsPerWeek,
                    ClassPeriodsPerWeek = primaryDef.ClassPeriodsPerWeek,
                    TeacherPeriodsPerWeek = primaryDef.TeacherPeriodsPerWeek,
                    NumberOfStudents = primaryDef.NumberOfStudents,
                    MaleStudents = primaryDef.MaleStudents,
                    FemaleStudents = primaryDef.FemaleStudents,
                    WeekValue = primaryDef.WeekValue,
                    YearValue = primaryDef.YearValue,
                    FromDate = primaryDef.FromDate,
                    ToDate = primaryDef.ToDate,
                    PartitionNumber = primaryDef.PartitionNumber,
                    StudentGroup = primaryDef.StudentGroup,
                    HomeRoom = primaryDef.HomeRoom,
                    RequiredRoomType = primaryDef.RequiredRoomType,
                    MinDoublePeriods = primaryDef.MinDoublePeriods,
                    MaxDoublePeriods = primaryDef.MaxDoublePeriods,
                    BlockSize = primaryDef.BlockSize,
                    Priority = primaryDef.Priority,
                    ConsecutiveSubjectsClass = primaryDef.ConsecutiveSubjectsClass,
                    ConsecutiveSubjectsTeacher = primaryDef.ConsecutiveSubjectsTeacher,
                    Codes = primaryDef.Codes,
                    Description = primaryDef.Description,
                    ForegroundColor = primaryDef.ForegroundColor,
                    BackgroundColor = primaryDef.BackgroundColor,
                    WeeklyPeriodsInTerms = primaryDef.WeeklyPeriodsInTerms,
                    IsActive = true
                };

                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync(); // Save to get Lesson.Id
                lessonsCreated++;

                // Create LessonClass junctions for all unique classes
                for (int i = 0; i < allClassNames.Count; i++)
                {
                    var classId = classes[allClassNames[i]].Id;
                    var lessonClass = new Models.LessonClass
                    {
                        LessonId = lesson.Id,
                        ClassId = classId,
                        IsPrimary = i == 0,
                        Order = i
                    };
                    _context.LessonClasses.Add(lessonClass);
                }

                // Create LessonSubject junctions for all subjects
                for (int i = 0; i < allSubjectCodes.Count; i++)
                {
                    var subjectId = subjects[allSubjectCodes[i]].Id;
                    var lessonSubject = new Models.LessonSubject
                    {
                        LessonId = lesson.Id,
                        SubjectId = subjectId,
                        IsPrimary = i == 0,
                        Order = i
                    };
                    _context.LessonSubjects.Add(lessonSubject);
                }

                // Create LessonTeacher junctions for all teachers
                for (int i = 0; i < allTeacherNames.Count; i++)
                {
                    var teacherId = teachers[allTeacherNames[i]].Id;
                    var lessonTeacher = new Models.LessonTeacher
                    {
                        LessonId = lesson.Id,
                        TeacherId = teacherId,
                        IsLead = i == 0,
                        Order = i,
                        Role = i == 0 ? "Lead Teacher" : "Co-Teacher"
                    };
                    _context.LessonTeachers.Add(lessonTeacher);
                }

                // Create ScheduledLesson entries for each unique schedule slot
                // Group by (Class, Day, Period) to get unique schedule slots
                var uniqueScheduleSlots = scheduleEntriesForLesson
                    .GroupBy(se => new {
                        ClassName = se.ScheduleEntry.ClassName,
                        DayNumber = se.ScheduleEntry.DayNumber,
                        PeriodNumber = se.ScheduleEntry.PeriodNumber
                    })
                    .Select(g => new {
                        g.Key.ClassName,
                        g.Key.DayNumber,
                        g.Key.PeriodNumber,
                        RoomNumbers = g.Select(se => se.ScheduleEntry.RoomNumber)
                                       .Where(r => !string.IsNullOrWhiteSpace(r))
                                       .Distinct()
                                       .ToList()
                    })
                    .ToList();

                foreach (var slot in uniqueScheduleSlots)
                {
                    // Validate period exists
                    if (!periods.ContainsKey(slot.PeriodNumber))
                    {
                        result.Warnings.Add($"Period {slot.PeriodNumber} not found for Lesson #{lessonNumber}");
                        continue;
                    }

                    // Convert day number to DayOfWeek
                    DayOfWeek dayOfWeek = slot.DayNumber switch
                    {
                        1 => DayOfWeek.Sunday,
                        2 => DayOfWeek.Monday,
                        3 => DayOfWeek.Tuesday,
                        4 => DayOfWeek.Wednesday,
                        5 => DayOfWeek.Thursday,
                        _ => DayOfWeek.Sunday
                    };

                    // Get primary room
                    var primaryRoomId = slot.RoomNumbers.Any() && rooms.ContainsKey(slot.RoomNumbers[0])
                        ? rooms[slot.RoomNumbers[0]].Id
                        : (int?)null;

                    // Create ScheduledLesson entry
                    var scheduledLesson = new Models.ScheduledLesson
                    {
                        LessonId = lesson.Id,
                        DayOfWeek = dayOfWeek,
                        PeriodId = periods[slot.PeriodNumber].Id,
                        RoomId = primaryRoomId,
                        TimetableId = timetableId
                    };

                    _context.ScheduledLessons.Add(scheduledLesson);
                    await _context.SaveChangesAsync(); // Save to get ScheduledLesson.Id
                    scheduledLessonsCreated++;

                    // Create ScheduledLessonRoom entries for all rooms in this slot
                    for (int i = 0; i < slot.RoomNumbers.Count; i++)
                    {
                        if (rooms.ContainsKey(slot.RoomNumbers[i]))
                        {
                            var roomId = rooms[slot.RoomNumbers[i]].Id;
                            var scheduledLessonRoom = new Models.ScheduledLessonRoom
                            {
                                ScheduledLessonId = scheduledLesson.Id,
                                RoomId = roomId,
                                PrimaryTeacherIdForRoom = i < allTeacherNames.Count && teachers.ContainsKey(allTeacherNames[i])
                                    ? teachers[allTeacherNames[i]].Id
                                    : (int?)null
                            };
                            _context.ScheduledLessonRooms.Add(scheduledLessonRoom);
                        }
                    }
                }

                result.RecordsImported++;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Import complete: {Lessons} lessons created, {Scheduled} scheduled entries created, {CoTeaching} co-teaching detected",
                lessonsCreated, scheduledLessonsCreated, coTeachingDetected);

            result.Success = true;

            // Build HTML summary
            var summaryHtml = new System.Text.StringBuilder();

            summaryHtml.AppendLine("<div class='import-summary'>");
            var summaryTitle = enableCoTeachingDetection
                ? "📊 Import Summary - LessonNumber-Based Co-Teaching Detection"
                : "📊 Import Summary - Individual Lesson Import (Co-Teaching Detection Disabled)";
            summaryHtml.AppendLine($"<h5 class='text-primary'>{summaryTitle}</h5>");
            summaryHtml.AppendLine("<hr>");

            // Input files section
            summaryHtml.AppendLine("<h6>📥 Input Files:</h6>");
            summaryHtml.AppendLine("<ul>");
            summaryHtml.AppendLine($"<li>Lesson definitions (GPU002.TXT): <strong>{lessonDefinitions.Count}</strong> entries</li>");
            summaryHtml.AppendLine($"<li>Schedule entries (GPU001.TXT): <strong>{scheduleEntries.Count}</strong> entries</li>");
            summaryHtml.AppendLine("</ul>");

            // Processing results section
            summaryHtml.AppendLine("<h6>✅ Processing Results:</h6>");
            summaryHtml.AppendLine("<ul>");
            summaryHtml.AppendLine($"<li>Unique lesson numbers processed: <strong>{groupedByLessonNumber.Count}</strong></li>");
            summaryHtml.AppendLine($"<li>Lessons created: <strong>{lessonsCreated}</strong></li>");
            summaryHtml.AppendLine($"<li>Scheduled entries created: <strong>{scheduledLessonsCreated}</strong></li>");
            summaryHtml.AppendLine($"<li>Records imported: <strong>{result.RecordsImported}</strong></li>");
            summaryHtml.AppendLine($"<li>Records skipped: <strong class='text-warning'>{result.RecordsSkipped}</strong></li>");
            summaryHtml.AppendLine("</ul>");

            // Co-teaching analysis section (only show if detection is enabled)
            if (enableCoTeachingDetection)
            {
                summaryHtml.AppendLine("<h6>👥 Co-Teaching Analysis:</h6>");
                summaryHtml.AppendLine("<ul>");
                summaryHtml.AppendLine($"<li>Regular lessons (single schedule entry): <strong>{lessonsCreated - coTeachingDetected}</strong></li>");
                summaryHtml.AppendLine($"<li>Co-teaching lessons (multiple entries with same LessonNumber): <strong class='text-success'>{coTeachingDetected}</strong></li>");
                summaryHtml.AppendLine("</ul>");
            }
            else
            {
                summaryHtml.AppendLine("<div class='alert alert-warning'>");
                summaryHtml.AppendLine("<strong><i class='bi bi-exclamation-triangle'></i> Co-Teaching Detection Disabled</strong>");
                summaryHtml.AppendLine("<p class='mb-0'>Each schedule entry was imported separately. Co-teaching was NOT detected. You may have duplicate lessons.</p>");
                summaryHtml.AppendLine("</div>");
            }

            // Add detailed co-teaching merge table (only if detection is enabled)
            if (enableCoTeachingDetection && coTeachingMerges.Any())
            {
                summaryHtml.AppendLine("<h6>📋 Detailed Co-Teaching Merges:</h6>");
                summaryHtml.AppendLine($"<p class='text-muted small'>Showing all {coTeachingMerges.Count} co-teaching lessons (multiple schedule entries with same LessonNumber):</p>");

                // Add filter inputs
                summaryHtml.AppendLine("<div class='row mb-2'>");
                summaryHtml.AppendLine("<div class='col-md-12'>");
                summaryHtml.AppendLine("<input type='text' id='coTeachingFilter' class='form-control form-control-sm' placeholder='Filter by Lesson Number, Classes, Teachers, or Subjects...'>");
                summaryHtml.AppendLine("</div>");
                summaryHtml.AppendLine("</div>");

                summaryHtml.AppendLine("<div class='table-responsive'>");
                summaryHtml.AppendLine("<table id='coTeachingMergesTable' class='table table-sm table-bordered table-hover'>");
                summaryHtml.AppendLine("<thead class='table-light'>");
                summaryHtml.AppendLine("<tr>");
                summaryHtml.AppendLine("<th style='width: 10%; cursor: pointer;' data-column='lesson' title='Click to sort'>Lesson # <i class='bi bi-arrow-down-up'></i></th>");
                summaryHtml.AppendLine("<th style='width: 10%; cursor: pointer;' data-column='entries' title='Click to sort'>Entries <i class='bi bi-arrow-down-up'></i></th>");
                summaryHtml.AppendLine("<th style='width: 15%; cursor: pointer;' data-column='classes' title='Click to sort'>Classes <i class='bi bi-arrow-down-up'></i></th>");
                summaryHtml.AppendLine("<th style='width: 20%; cursor: pointer;' data-column='teachers' title='Click to sort'>Teachers <i class='bi bi-arrow-down-up'></i></th>");
                summaryHtml.AppendLine("<th style='width: 15%; cursor: pointer;' data-column='subjects' title='Click to sort'>Subjects <i class='bi bi-arrow-down-up'></i></th>");
                summaryHtml.AppendLine("<th style='width: 30%;'>Schedule Slots</th>");
                summaryHtml.AppendLine("</tr>");
                summaryHtml.AppendLine("</thead>");
                summaryHtml.AppendLine("<tbody>");

                foreach (var merge in coTeachingMerges)
                {
                    summaryHtml.AppendLine("<tr>");
                    summaryHtml.AppendLine($"<td data-value='{merge.LessonNumber}'><span class='badge bg-primary'>#{merge.LessonNumber}</span></td>");
                    summaryHtml.AppendLine($"<td class='text-center' data-value='{merge.ScheduleEntriesCount}'><span class='badge bg-warning text-dark'>{merge.ScheduleEntriesCount} entries</span></td>");
                    summaryHtml.AppendLine($"<td data-value='{merge.Classes}'>{merge.Classes}</td>");
                    summaryHtml.AppendLine($"<td data-value='{merge.Teachers}'><strong>{merge.Teachers}</strong></td>");
                    summaryHtml.AppendLine($"<td data-value='{merge.Subjects}'>{merge.Subjects}</td>");
                    summaryHtml.AppendLine($"<td><small class='text-muted'>{merge.ScheduleSlots}</small></td>");
                    summaryHtml.AppendLine("</tr>");
                }

                summaryHtml.AppendLine("</tbody>");
                summaryHtml.AppendLine("</table>");
                summaryHtml.AppendLine("</div>");
            }

            summaryHtml.AppendLine("</div>");

            result.Warnings.Insert(0, summaryHtml.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during simplified import with conflict detection");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }
    */

    /// <summary>
    /// Parse GPU002.TXT into in-memory lesson definitions
    /// </summary>
    private async Task<List<UntisImportModels.ParsedLessonDefinition>> ParseLessonDefinitionsAsync(Stream fileStream)
    {
        var definitions = new List<UntisImportModels.ParsedLessonDefinition>();

        using var reader = new StreamReader(fileStream, Encoding.UTF8);
        string? line;
        bool isFirstLine = true;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Skip header if detected
            if (isFirstLine)
            {
                isFirstLine = false;
                if (IsHeaderLine(line, new[] { "CODE" }))
                    continue;
            }

            var fields = ParseCsvLine(line);

            if (fields.Count < 7)
                continue;

            if (!int.TryParse(fields[0], out int lessonNumber))
                continue;

            var definition = new UntisImportModels.ParsedLessonDefinition
            {
                LessonNumber = lessonNumber,
                ClassName = fields[4],
                TeacherName = fields[5],
                SubjectCode = fields[6],
                PeriodsPerWeek = int.TryParse(fields[1], out int ppw) ? ppw : 1,
                ClassPeriodsPerWeek = fields.Count > 2 && int.TryParse(fields[2], out int cpw) ? cpw : 0,
                TeacherPeriodsPerWeek = fields.Count > 3 && int.TryParse(fields[3], out int tpw) ? tpw : 0,
                NumberOfStudents = fields.Count > 9 && int.TryParse(fields[9], out int ns) ? ns : (int?)null,
                WeekValue = fields.Count > 10 && decimal.TryParse(fields[10], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal wv) ? wv : (decimal?)null,
                StudentGroup = fields.Count > 11 ? fields[11] : null,
                FromDate = fields.Count > 14 && !string.IsNullOrEmpty(fields[14]) && DateTime.TryParseExact(fields[14], "yyyyMMdd", null, DateTimeStyles.None, out DateTime fd) ? fd : (DateTime?)null,
                ToDate = fields.Count > 15 && !string.IsNullOrEmpty(fields[15]) && DateTime.TryParseExact(fields[15], "yyyyMMdd", null, DateTimeStyles.None, out DateTime td) ? td : (DateTime?)null,
                YearValue = fields.Count > 16 && decimal.TryParse(fields[16], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal yv) ? yv : (decimal?)null,
                PartitionNumber = fields.Count > 18 && int.TryParse(fields[18], out int pn) ? pn : (int?)null,
                HomeRoom = fields.Count > 19 ? fields[19] : null,
                Description = fields.Count > 20 ? fields[20] : null,
                ForegroundColor = fields.Count > 21 ? fields[21] : null,
                BackgroundColor = fields.Count > 22 ? fields[22] : null,
                Codes = fields.Count > 23 ? fields[23] : null,
                ConsecutiveSubjectsClass = fields.Count > 24 && int.TryParse(fields[24], out int csc) ? csc : (int?)null,
                ConsecutiveSubjectsTeacher = fields.Count > 25 && int.TryParse(fields[25], out int cst) ? cst : (int?)null,
                MinDoublePeriods = fields.Count > 27 && int.TryParse(fields[27], out int mindp) ? mindp : (int?)null,
                MaxDoublePeriods = fields.Count > 28 && int.TryParse(fields[28], out int maxdp) ? maxdp : (int?)null,
                BlockSize = fields.Count > 29 && int.TryParse(fields[29], out int bs) ? bs : (int?)null,
                Priority = fields.Count > 31 && int.TryParse(fields[31], out int pri) ? pri : (int?)null,
                MaleStudents = fields.Count > 33 && int.TryParse(fields[33], out int ms) ? ms : (int?)null,
                FemaleStudents = fields.Count > 34 && int.TryParse(fields[34], out int fs) ? fs : (int?)null,
                WeeklyPeriodsInTerms = fields.Count > 42 ? fields[42] : null
            };

            definitions.Add(definition);
        }

        return definitions;
    }

    /// <summary>
    /// Parse GPU001.TXT into in-memory schedule entries
    /// </summary>
    private async Task<List<UntisImportModels.ParsedScheduleEntry>> ParseScheduleEntriesAsync(Stream fileStream)
    {
        var entries = new List<UntisImportModels.ParsedScheduleEntry>();

        using var reader = new StreamReader(fileStream, Encoding.UTF8);
        string? line;
        bool isFirstLine = true;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Skip header if detected
            if (isFirstLine)
            {
                isFirstLine = false;
                if (IsHeaderLine(line, new[] { "CODE" }))
                    continue;
            }

            var fields = ParseCsvLine(line);

            if (fields.Count < 7)
                continue;

            if (!int.TryParse(fields[0], out int lessonNumber) ||
                !int.TryParse(fields[5], out int day) ||
                !int.TryParse(fields[6], out int periodNum))
                continue;

            var entry = new UntisImportModels.ParsedScheduleEntry
            {
                LessonNumber = lessonNumber,
                ClassName = fields[1],
                TeacherName = fields[2],
                SubjectCode = fields[3],
                RoomNumber = fields[4],
                DayNumber = day,
                PeriodNumber = periodNum
            };

            entries.Add(entry);
        }

        return entries;
    }

    /// <summary>
    /// Detect co-teaching by grouping schedule entries by (Class, Day, Period)
    /// </summary>
    private List<UntisImportModels.DetectedLesson> DetectCoTeachingLessons(
        List<UntisImportModels.ParsedScheduleEntry> scheduleEntries,
        List<UntisImportModels.ParsedLessonDefinition> lessonDefinitions)
    {
        var detectedLessons = new List<UntisImportModels.DetectedLesson>();

        // Create lookup dictionary for lesson definitions
        var definitionsByNumber = lessonDefinitions.ToDictionary(d => d.LessonNumber, d => d);

        // Group schedule entries by (Class, Day, Period)
        var groups = scheduleEntries.GroupBy(e => e.GroupingKey);

        foreach (var group in groups)
        {
            var firstEntry = group.First();
            var detected = new UntisImportModels.DetectedLesson
            {
                LessonKey = group.Key,
                ClassName = firstEntry.ClassName,
                DayNumber = firstEntry.DayNumber,
                PeriodNumber = firstEntry.PeriodNumber,
                SourceEntries = group.ToList()
            };

            // Collect all teachers, subjects, and rooms
            foreach (var entry in group)
            {
                if (!detected.TeacherNames.Contains(entry.TeacherName))
                    detected.TeacherNames.Add(entry.TeacherName);

                if (!detected.SubjectCodes.Contains(entry.SubjectCode))
                    detected.SubjectCodes.Add(entry.SubjectCode);

                if (!string.IsNullOrWhiteSpace(entry.RoomNumber) && !detected.RoomNumbers.Contains(entry.RoomNumber))
                    detected.RoomNumbers.Add(entry.RoomNumber);

                // Lookup corresponding lesson definition
                if (definitionsByNumber.TryGetValue(entry.LessonNumber, out var definition))
                {
                    if (!detected.SourceDefinitions.Any(d => d.LessonNumber == definition.LessonNumber))
                        detected.SourceDefinitions.Add(definition);
                }
            }

            detectedLessons.Add(detected);
        }

        return detectedLessons;
    }

    /// <summary>
    /// Import break supervision data from GPU009.TXT
    /// Creates duty assignments linking teachers to rooms (supervision locations)
    ///
    /// GPU009.TXT columns:
    /// 1. Corridor (Room) - supervision location
    /// 2. Teacher - assigned teacher name (can be empty)
    /// 3. Day Number - day of week (1=Monday to 5=Friday)
    /// 4. Period Number - period when supervision occurs
    /// 5. Points - point value for the duty
    /// </summary>
    /// <param name="fileStream">The GPU009.TXT file stream</param>
    /// <param name="timetableId">The timetable ID to associate duties with</param>
    public async Task<ImportResult> ImportBreakSupervisionAsync(Stream fileStream, int timetableId)
    {
        var result = new ImportResult();

        try
        {
            // Load existing teachers for reference (match by FirstName)
            var teachers = await _context.Teachers.ToListAsync();
            var teacherLookup = teachers
                .GroupBy(t => t.FirstName.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            // Load existing rooms for reference (match by RoomNumber)
            var rooms = await _context.Rooms.ToListAsync();
            var roomLookup = rooms
                .GroupBy(r => r.RoomNumber.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            string? line;
            bool isFirstLine = true;
            var missingRooms = new HashSet<string>();

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Skip header if detected
                if (isFirstLine)
                {
                    isFirstLine = false;
                    if (IsHeaderLine(line, new[] { "CODE" }))
                        continue;
                }

                result.RecordsProcessed++;
                var fields = ParseCsvLine(line);
                var csvLineData = string.Join(";", fields);

                // GPU009 format: Corridor;Teacher;DayNumber;PeriodNumber;Points
                if (fields.Count < 5)
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Insufficient fields (need 5, got {fields.Count})|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                var locationCode = fields[0];      // Column 1: Corridor (Room)
                var teacherName = fields[1];       // Column 2: Teacher (can be empty)
                var dayNumberStr = fields[2];      // Column 3: Day Number
                var periodNumberStr = fields[3];   // Column 4: Period Number
                var pointsStr = fields[4];         // Column 5: Points

                if (string.IsNullOrEmpty(locationCode))
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Location code is empty|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                // Parse numeric fields
                if (!int.TryParse(dayNumberStr, out int dayNumber) || dayNumber < 1 || dayNumber > 7)
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Invalid day number '{dayNumberStr}'|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                if (!int.TryParse(periodNumberStr, out int periodNumber))
                {
                    result.Warnings.Add($"Line {result.RecordsProcessed}: Invalid period number '{periodNumberStr}'|LINE:{csvLineData}");
                    result.RecordsSkipped++;
                    continue;
                }

                if (!int.TryParse(pointsStr, out int points))
                {
                    points = 30; // Default points
                }

                // Convert UNTIS day (1=Monday) to .NET DayOfWeek
                // UNTIS: 1=Mon, 2=Tue, 3=Wed, 4=Thu, 5=Fri, 6=Sat, 7=Sun
                // .NET: 0=Sun, 1=Mon, 2=Tue, 3=Wed, 4=Thu, 5=Fri, 6=Sat
                DayOfWeek dayOfWeek = dayNumber switch
                {
                    1 => DayOfWeek.Monday,
                    2 => DayOfWeek.Tuesday,
                    3 => DayOfWeek.Wednesday,
                    4 => DayOfWeek.Thursday,
                    5 => DayOfWeek.Friday,
                    6 => DayOfWeek.Saturday,
                    7 => DayOfWeek.Sunday,
                    _ => DayOfWeek.Monday
                };

                // Find room (supervision location)
                var roomKey = locationCode.ToLowerInvariant();
                if (!roomLookup.TryGetValue(roomKey, out var room))
                {
                    if (!missingRooms.Contains(locationCode))
                    {
                        result.Warnings.Add($"Room/Location '{locationCode}' not found in Rooms table. Skipping related duties.");
                        missingRooms.Add(locationCode);
                    }
                    result.RecordsSkipped++;
                    continue;
                }

                // Find teacher (optional - can be null for unassigned slots)
                int? teacherId = null;
                if (!string.IsNullOrWhiteSpace(teacherName))
                {
                    var teacherKey = teacherName.ToLowerInvariant();
                    if (teacherLookup.TryGetValue(teacherKey, out var teacher))
                    {
                        teacherId = teacher.Id;
                    }
                    else
                    {
                        result.Warnings.Add($"Line {result.RecordsProcessed}: Teacher '{teacherName}' not found in database|LINE:{csvLineData}");
                    }
                }

                // Check for existing duty at this room/day/period/timetable
                var existingDuty = await _context.BreakSupervisionDuties
                    .FirstOrDefaultAsync(d =>
                        d.RoomId == room.Id &&
                        d.DayOfWeek == dayOfWeek &&
                        d.PeriodNumber == periodNumber &&
                        d.TimetableId == timetableId);

                if (existingDuty == null)
                {
                    // Create new duty
                    var duty = new BreakSupervisionDuty
                    {
                        RoomId = room.Id,
                        TeacherId = teacherId,
                        DayOfWeek = dayOfWeek,
                        PeriodNumber = periodNumber,
                        Points = points,
                        IsActive = true,
                        TimetableId = timetableId
                    };
                    _context.BreakSupervisionDuties.Add(duty);
                    result.RecordsImported++;
                }
                else
                {
                    // Update existing duty
                    existingDuty.TeacherId = teacherId;
                    existingDuty.Points = points;
                    existingDuty.IsActive = true;
                    result.RecordsUpdated++;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation(
                "Break supervision import completed: {Imported} imported, {Updated} updated, {Skipped} skipped",
                result.RecordsImported, result.RecordsUpdated, result.RecordsSkipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing break supervision data from GPU009.TXT");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Clear break supervision duty data for a specific timetable
    /// </summary>
    /// <param name="timetableId">The timetable ID to clear duties for</param>
    public async Task<ImportResult> ClearBreakSupervisionDataAsync(int timetableId)
    {
        var result = new ImportResult();

        try
        {
            var dutiesToRemove = await _context.BreakSupervisionDuties
                .Where(d => d.TimetableId == timetableId)
                .ToListAsync();

            var dutiesCount = dutiesToRemove.Count;
            _context.BreakSupervisionDuties.RemoveRange(dutiesToRemove);

            await _context.SaveChangesAsync();

            result.Success = true;
            result.RecordsProcessed = dutiesCount;
            _logger.LogInformation("Cleared {Duties} break supervision duties for timetable {TimetableId}", dutiesCount, timetableId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing break supervision data for timetable {TimetableId}", timetableId);
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }
}
