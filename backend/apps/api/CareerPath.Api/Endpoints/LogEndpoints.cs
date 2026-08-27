using Microsoft.AspNetCore.Mvc;

namespace CareerPath.Api.Endpoints;

public static class LogEndpoints
{
    public static void MapLogs(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/system/logs")
            .WithTags("SystemLogs");

        // GET /api/v1/system/logs -> List all log files with sizes and dates
        group.MapGet("/", GetLogFiles)
            .WithName("GetLogFiles")
            .WithSummary("List all text log files in the logs directory")
            .AllowAnonymous();

        // GET /api/v1/system/logs/latest -> View or download the latest log content as text
        group.MapGet("/latest", GetLatestLog)
            .WithName("GetLatestLog")
            .WithSummary("View latest text log file entries (tail)")
            .AllowAnonymous();

        // GET /api/v1/system/logs/{fileName} -> Read a specific log file
        group.MapGet("/{fileName}", GetLogByName)
            .WithName("GetLogByName")
            .WithSummary("Read full text of a specific log file")
            .AllowAnonymous();
    }

    private static IResult GetLogFiles()
    {
        var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        if (!Directory.Exists(logDir))
        {
            logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (!Directory.Exists(logDir))
            {
                return Results.Ok(new { message = "No logs directory found yet.", files = Array.Empty<object>() });
            }
        }

        var files = Directory.GetFiles(logDir, "*.txt")
            .Select(f =>
            {
                var fi = new FileInfo(f);
                return new
                {
                    name = fi.Name,
                    sizeBytes = fi.Length,
                    sizeKb = Math.Round((double)fi.Length / 1024, 2),
                    lastModifiedUtc = fi.LastWriteTimeUtc,
                    viewUrl = $"/api/v1/system/logs/{fi.Name}"
                };
            })
            .OrderByDescending(f => f.lastModifiedUtc)
            .ToList();

        return Results.Ok(new { count = files.Count, files });
    }

    private static IResult GetLatestLog([FromQuery] int lines = 200)
    {
        var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        if (!Directory.Exists(logDir))
        {
            logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (!Directory.Exists(logDir))
                return Results.Content("No logs directory found yet.", "text/plain");
        }

        var latestFile = Directory.GetFiles(logDir, "*.txt")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault();

        if (latestFile == null || !latestFile.Exists)
            return Results.Content("No log files recorded yet.", "text/plain");

        return ReadFileTail(latestFile.FullName, lines);
    }

    private static IResult GetLogByName(string fileName, [FromQuery] int lines = 500)
    {
        var cleanFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(cleanFileName) || !cleanFileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "Invalid log file name. Must be a .txt file." });

        var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        var filePath = Path.Combine(logDir, cleanFileName);

        if (!File.Exists(filePath))
        {
            logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            filePath = Path.Combine(logDir, cleanFileName);
            if (!File.Exists(filePath))
                return Results.NotFound(new { error = $"Log file '{cleanFileName}' not found." });
        }

        return ReadFileTail(filePath, lines);
    }

    private static IResult ReadFileTail(string fullPath, int maxLines)
    {
        try
        {
            using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);

            var lineList = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lineList.Add(line);
            }

            var tail = maxLines > 0 && lineList.Count > maxLines
                ? lineList.Skip(lineList.Count - maxLines)
                : lineList;

            var content = string.Join(Environment.NewLine, tail);
            return Results.Content(content, "text/plain; charset=utf-8");
        }
        catch (Exception ex)
        {
            return Results.Content($"Error reading log file: {ex.Message}", "text/plain");
        }
    }
}
