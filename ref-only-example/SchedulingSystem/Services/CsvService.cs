using System.Text;

namespace SchedulingSystem.Services
{
    public class CsvService
    {
        public static string GenerateCsv<T>(IEnumerable<T> data, string[] headers)
        {
            var sb = new StringBuilder();

            // Add headers
            sb.AppendLine(string.Join(",", headers));

            // Add data rows
            foreach (var item in data)
            {
                var values = new List<string>();
                var properties = typeof(T).GetProperties();

                foreach (var header in headers)
                {
                    var property = properties.FirstOrDefault(p =>
                        p.Name.Equals(header, StringComparison.OrdinalIgnoreCase));

                    if (property != null)
                    {
                        var value = property.GetValue(item)?.ToString() ?? "";
                        // Escape commas and quotes
                        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                        {
                            value = $"\"{value.Replace("\"", "\"\"")}\"";
                        }
                        values.Add(value);
                    }
                    else
                    {
                        values.Add("");
                    }
                }

                sb.AppendLine(string.Join(",", values));
            }

            return sb.ToString();
        }

        public static List<Dictionary<string, string>> ParseCsv(Stream stream)
        {
            var result = new List<Dictionary<string, string>>();

            using (var reader = new StreamReader(stream))
            {
                // Read header
                var headerLine = reader.ReadLine();
                if (string.IsNullOrEmpty(headerLine))
                {
                    return result;
                }

                var headers = ParseCsvLine(headerLine);

                // Read data rows
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var values = ParseCsvLine(line);
                    var row = new Dictionary<string, string>();

                    for (int i = 0; i < headers.Length && i < values.Length; i++)
                    {
                        row[headers[i]] = values[i];
                    }

                    result.Add(row);
                }
            }

            return result;
        }

        private static string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        // Toggle quote state
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // End of field
                    values.Add(currentValue.ToString().Trim());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            // Add last field
            values.Add(currentValue.ToString().Trim());

            return values.ToArray();
        }
    }
}
