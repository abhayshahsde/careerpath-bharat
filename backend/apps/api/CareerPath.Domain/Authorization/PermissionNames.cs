namespace CareerPath.Domain.Authorization;

/// <summary>
/// Fine-grained permission names used across the application.
/// Format: Module.Action
/// </summary>
public static class PermissionNames
{
    // Careers
    public const string CareersView    = "Careers.View";
    public const string CareersEdit    = "Careers.Edit";
    public const string CareersPublish = "Careers.Publish";
    public const string CareersArchive = "Careers.Archive";

    // Courses, Exams, Skills, Scholarships
    public const string CatalogEdit    = "Catalog.Edit";
    public const string CatalogPublish = "Catalog.Publish";

    // Imports
    public const string ImportsUpload  = "Imports.Upload";
    public const string ImportsApprove = "Imports.Approve";

    // Exports
    public const string ExportsSelf       = "Exports.Self";
    public const string ExportsAdminUsers = "Exports.AdminUsers";

    // Knowledge
    public const string KnowledgeEdit    = "Knowledge.Edit";
    public const string KnowledgePublish = "Knowledge.Publish";

    // AI
    public const string AiChat          = "AI.Chat";
    public const string AiPromptManage  = "AI.PromptManage";

    // Users / Admin
    public const string UsersView       = "Users.View";
    public const string UsersSuspend    = "Users.Suspend";

    // Audit
    public const string AuditView       = "Audit.View";

    // System
    public const string SystemAdmin     = "System.Admin";
}
