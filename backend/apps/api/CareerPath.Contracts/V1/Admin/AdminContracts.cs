namespace CareerPath.Contracts.V1.Admin;

public sealed record SiteSettingsDto(
    string SiteName,
    string LogoText,
    string LogoSubtitle,
    string Tagline,
    string? AnnouncementText,
    bool AnnouncementActive,
    string SupportEmail,
    string SupportPhone,
    string FooterText,
    string NavMenusJson,
    DateTimeOffset UpdatedAt);

public sealed record UpdateSiteSettingsRequest(
    string SiteName,
    string LogoText,
    string LogoSubtitle,
    string Tagline,
    string? AnnouncementText,
    bool AnnouncementActive,
    string SupportEmail,
    string SupportPhone,
    string FooterText,
    string NavMenusJson);

public sealed record CreateStaffUserRequest(
    string Email,
    string Password,
    string DisplayName,
    string Role); // 'Admin', 'ContentEditor', 'Reviewer', 'FinanceAdmin', 'Support'

public sealed record UpdateUserRolesRequest(
    List<string> Roles);

public sealed record CreateCareerRequest(
    string Title,
    string Slug,
    string Summary,
    string? CategoryId,
    string? SalaryRangeLabel,
    bool IsFeatured);

public sealed record UpdateCareerRequest(
    string Title,
    string Summary,
    string? CategoryId,
    string? SalaryRangeLabel,
    bool IsFeatured);

public sealed record CreateExamRequest(
    string Name,
    string Code,
    string Level,
    string? WebsiteUrl,
    string? EligibilitySummary,
    DateTimeOffset? ExamDate,
    DateTimeOffset? ApplicationDeadline);

public sealed record UpdateExamRequest(
    string Name,
    string Code,
    string Level,
    string? WebsiteUrl,
    string? EligibilitySummary,
    DateTimeOffset? ExamDate,
    DateTimeOffset? ApplicationDeadline);

public sealed record CreateCourseRequest(
    string Name,
    string Slug,
    string DegreeLevel,
    int DurationYears,
    string? EligibilityCriteria,
    string? CategoryId);

public sealed record UpdateCourseRequest(
    string Name,
    string DegreeLevel,
    int DurationYears,
    string? EligibilityCriteria);

public sealed record CreateKnowledgeDocumentRequest(
    string Title,
    string DocType,
    List<string> Chunks);

public sealed record UpdateKnowledgeChunkRequest(
    string Content,
    bool IsReviewed);

public sealed record CreateEditorialArticleRequest(
    string Title,
    string Slug,
    string Summary,
    string BodyContent,
    string AuthorName);
