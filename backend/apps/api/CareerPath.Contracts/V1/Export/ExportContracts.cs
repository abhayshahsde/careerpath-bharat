namespace CareerPath.Contracts.V1.Export;

public sealed record RequestExportRequest(
    string ExportType, // Careers, Roadmaps, Profile
    string Format);      // PDF, XLSX, CSV, DOCX

public sealed record ExportJobDto(
    Guid Id,
    string ExportType,
    string Format,
    string Status,
    DateTimeOffset ExpireAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? DownloadUrl,
    string? ErrorDetails);
