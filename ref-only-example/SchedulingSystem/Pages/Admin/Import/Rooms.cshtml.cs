using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using System.Text;

namespace SchedulingSystem.Pages.Admin.Import
{
    public class RoomsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RoomsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IFormFile? UploadedFile { get; set; }

        public bool ShowResults { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public void OnGet()
        {
        }

        public IActionResult OnGetDownloadTemplate()
        {
            var template = new StringBuilder();
            template.AppendLine("RoomNumber,Name,RoomType,Capacity,IsActive");
            template.AppendLine("101,Room 101,Classroom,30,true");
            template.AppendLine("LAB1,Science Lab 1,Laboratory,24,true");
            template.AppendLine("COMP1,Computer Lab 1,Computer Lab,28,true");

            var bytes = Encoding.UTF8.GetBytes(template.ToString());
            return File(bytes, "text/csv", "rooms_template.csv");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadedFile == null || UploadedFile.Length == 0)
            {
                Errors.Add("Please select a valid CSV file.");
                ShowResults = true;
                return Page();
            }

            try
            {
                using var stream = UploadedFile.OpenReadStream();
                var rows = CsvService.ParseCsv(stream);

                if (rows.Count == 0)
                {
                    Errors.Add("The CSV file is empty or contains no valid data rows.");
                    ShowResults = true;
                    return Page();
                }

                // Get existing room numbers to check for duplicates
                var existingRoomNumbers = await _context.Rooms
                    .Select(r => r.RoomNumber.ToLower())
                    .ToListAsync();

                var roomsToImport = new List<Room>();
                int rowNumber = 1;

                foreach (var row in rows)
                {
                    rowNumber++;

                    // Validate required fields
                    if (!row.ContainsKey("RoomNumber") || string.IsNullOrWhiteSpace(row["RoomNumber"]))
                    {
                        Errors.Add($"Row {rowNumber}: RoomNumber is required.");
                        continue;
                    }

                    if (!row.ContainsKey("Name") || string.IsNullOrWhiteSpace(row["Name"]))
                    {
                        Errors.Add($"Row {rowNumber}: Name is required.");
                        continue;
                    }

                    if (!row.ContainsKey("Capacity") || string.IsNullOrWhiteSpace(row["Capacity"]))
                    {
                        Errors.Add($"Row {rowNumber}: Capacity is required.");
                        continue;
                    }

                    var roomNumber = row["RoomNumber"].Trim();
                    var name = row["Name"].Trim();

                    // Check for duplicate RoomNumber in existing data
                    if (existingRoomNumbers.Contains(roomNumber.ToLower()))
                    {
                        Warnings.Add($"Row {rowNumber}: Room with number '{roomNumber}' already exists. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Check for duplicate RoomNumber in current import batch
                    if (roomsToImport.Any(r => r.RoomNumber.Equals(roomNumber, StringComparison.OrdinalIgnoreCase)))
                    {
                        Warnings.Add($"Row {rowNumber}: Duplicate RoomNumber '{roomNumber}' in import file. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Parse capacity
                    if (!int.TryParse(row["Capacity"], out var capacity))
                    {
                        Errors.Add($"Row {rowNumber}: Invalid Capacity '{row["Capacity"]}'. Must be a number.");
                        continue;
                    }

                    // Parse optional fields
                    string? roomType = null;
                    if (row.ContainsKey("RoomType") && !string.IsNullOrWhiteSpace(row["RoomType"]))
                    {
                        roomType = row["RoomType"].Trim();
                    }

                    bool isActive = true;
                    if (row.ContainsKey("IsActive") && !string.IsNullOrWhiteSpace(row["IsActive"]))
                    {
                        if (bool.TryParse(row["IsActive"], out var active))
                        {
                            isActive = active;
                        }
                        else
                        {
                            Warnings.Add($"Row {rowNumber}: Invalid IsActive value '{row["IsActive"]}'. Using default (true).");
                        }
                    }

                    var room = new Room
                    {
                        RoomNumber = roomNumber,
                        Name = name,
                        RoomType = roomType,
                        Capacity = capacity,
                        IsActive = isActive
                    };

                    roomsToImport.Add(room);
                }

                // Import valid rooms
                if (roomsToImport.Any())
                {
                    _context.Rooms.AddRange(roomsToImport);
                    await _context.SaveChangesAsync();
                    ImportedCount = roomsToImport.Count;
                }

                ShowResults = true;
            }
            catch (Exception ex)
            {
                Errors.Add($"An error occurred while processing the file: {ex.Message}");
                ShowResults = true;
            }

            return Page();
        }
    }
}
