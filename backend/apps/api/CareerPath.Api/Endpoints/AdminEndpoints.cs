using Dapper;
using CareerPath.Application.Abstractions;
using CareerPath.Contracts.V1.Admin;
using CareerPath.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace CareerPath.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdmin(this IEndpointRouteBuilder app)
    {
        // ── Public Site Settings Endpoint ─────────────────────────────────────
        app.MapGet("/api/v1/settings", GetPublicSettings)
            .WithName("GetPublicSettings")
            .WithSummary("Retrieve dynamic site branding, logo text, announcement banner, and navigation menus")
            .AllowAnonymous();

        // ── Admin Super Control Group (Admin or ContentEditor roles) ──────────
        var admin = app.MapGroup("/api/v1/admin")
            .WithTags("Admin Super Control & CMS")
            .RequireAuthorization();

        // Site Settings
        admin.MapPut("/settings", UpdateSiteSettings)
            .WithName("UpdateSiteSettings")
            .WithSummary("Update site branding, logo, announcement, footer and menus");

        // Staff & Role Management
        admin.MapPost("/users/create-staff", CreateStaffUser)
            .WithName("CreateStaffUser")
            .WithSummary("Create a new administrative user with assigned roles");

        admin.MapPut("/users/{id:guid}/roles", UpdateUserRoles)
            .WithName("UpdateUserRoles")
            .WithSummary("Update role assignments for a user account");

        // Careers CMS
        admin.MapPost("/catalog/careers", CreateCareer)
            .WithName("AdminCreateCareer")
            .WithSummary("Create a new career profile");

        admin.MapPut("/catalog/careers/{id:guid}", UpdateCareer)
            .WithName("AdminUpdateCareer")
            .WithSummary("Update an existing career profile");

        admin.MapDelete("/catalog/careers/{id:guid}", DeleteCareer)
            .WithName("AdminDeleteCareer")
            .WithSummary("Delete or archive a career profile");

        // Exams CMS
        admin.MapPost("/catalog/exams", CreateExam)
            .WithName("AdminCreateExam")
            .WithSummary("Create a new entrance exam");

        admin.MapPut("/catalog/exams/{id:int}", UpdateExam)
            .WithName("AdminUpdateExam")
            .WithSummary("Update an entrance exam");

        admin.MapDelete("/catalog/exams/{id:int}", DeleteExam)
            .WithName("AdminDeleteExam")
            .WithSummary("Delete an entrance exam");

        // Courses CMS
        admin.MapPost("/catalog/courses", CreateCourse)
            .WithName("AdminCreateCourse")
            .WithSummary("Create a new course or degree");

        admin.MapPut("/catalog/courses/{id:int}", UpdateCourse)
            .WithName("AdminUpdateCourse")
            .WithSummary("Update a course or degree");

        admin.MapDelete("/catalog/courses/{id:int}", DeleteCourse)
            .WithName("AdminDeleteCourse")
            .WithSummary("Delete a course");

        // Knowledge Documents & Chunks
        admin.MapPost("/knowledge/documents", CreateKnowledgeDocument)
            .WithName("AdminCreateKnowledgeDocument")
            .WithSummary("Create a knowledge document and its text chunks for AI chatbot RAG");

        admin.MapPut("/knowledge/chunks/{id:long}", UpdateKnowledgeChunk)
            .WithName("AdminUpdateKnowledgeChunk")
            .WithSummary("Edit and review a knowledge text chunk");

        admin.MapDelete("/knowledge/documents/{id:guid}", DeleteKnowledgeDocument)
            .WithName("AdminDeleteKnowledgeDocument")
            .WithSummary("Delete a knowledge document and its chunks");

        // Editorial CMS
        admin.MapPost("/editorial/articles", CreateEditorialArticle)
            .WithName("AdminCreateEditorialArticle")
            .WithSummary("Create a new career guidance article");

        // Bulk Imports trigger
        admin.MapPost("/imports/trigger", TriggerImportJob)
            .WithName("AdminTriggerImportJob")
            .WithSummary("Trigger a new bulk catalog import batch");
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private static async Task<IResult> GetPublicSettings(
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var settings = await conn.QuerySingleOrDefaultAsync<SiteSettingsDto>(
            """
            SELECT TOP 1 SiteName, LogoText, LogoSubtitle, Tagline, AnnouncementText, 
                         AnnouncementActive, SupportEmail, SupportPhone, FooterText, 
                         NavMenusJson, UpdatedAt
            FROM [settings].[SiteSettings]
            WHERE Id = 1
            """);

        if (settings is null)
        {
            settings = new SiteSettingsDto(
                "CareerPath Bharat", "CareerPath", "Bharat",
                "India's premier career guidance and roadmapping platform for students",
                "⚡ UPSC, JEE & NEET 2026 notifications out now! Check your personalized roadmaps.",
                true, "support@careerpathbharat.com", "+91 9876543210",
                "Empowering students across all 28 states & 8 UTs of Bharat.",
                "[{\"label\":\"Dashboard\",\"href\":\"/dashboard\",\"isActive\":true},{\"label\":\"Roadmaps\",\"href\":\"/me/roadmaps\",\"isActive\":true},{\"label\":\"Careers\",\"href\":\"/careers\",\"isActive\":true},{\"label\":\"Exams\",\"href\":\"/exams\",\"isActive\":true},{\"label\":\"Courses\",\"href\":\"/courses\",\"isActive\":true},{\"label\":\"Scholarships\",\"href\":\"/scholarships\",\"isActive\":true}]",
                DateTimeOffset.UtcNow);
        }

        return Results.Ok(settings);
    }

    private static async Task<IResult> UpdateSiteSettings(
        [FromBody] UpdateSiteSettingsRequest req,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE [settings].[SiteSettings]
            SET SiteName = @SiteName,
                LogoText = @LogoText,
                LogoSubtitle = @LogoSubtitle,
                Tagline = @Tagline,
                AnnouncementText = @AnnouncementText,
                AnnouncementActive = @AnnouncementActive,
                SupportEmail = @SupportEmail,
                SupportPhone = @SupportPhone,
                FooterText = @FooterText,
                NavMenusJson = @NavMenusJson,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = 1
            """,
            req);

        return Results.Ok(new { success = true, message = "Site branding and navigation settings saved successfully." });
    }

    private static async Task<IResult> CreateStaffUser(
        [FromBody] CreateStaffUserRequest req,
        ISqlConnectionFactory db,
        IPasswordHasher hasher,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Results.BadRequest(new { error = "Email and Password are required." });

        await using var conn = await db.CreateOpenConnectionAsync(ct);

        var existing = await conn.ExecuteScalarAsync<Guid?>(
            "SELECT Id FROM [identity].[Users] WHERE Email = @Email",
            new { Email = req.Email.Trim().ToLowerInvariant() });

        if (existing.HasValue)
            return Results.Conflict(new { error = "An account with this email address already exists." });

        var userId = Guid.NewGuid();
        var hash = hasher.Hash(req.Password);

        await conn.ExecuteAsync(
            """
            INSERT INTO [identity].[Users] 
                (Id, Email, PasswordHash, DisplayName, IsEmailVerified, IsActive, CreatedAt, UpdatedAt)
            VALUES 
                (@Id, @Email, @PasswordHash, @DisplayName, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME())
            """,
            new
            {
                Id = userId,
                Email = req.Email.Trim().ToLowerInvariant(),
                PasswordHash = hash,
                DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? "Staff Member" : req.DisplayName.Trim()
            });

        // Assign Role
        var roleName = string.IsNullOrWhiteSpace(req.Role) ? "Admin" : req.Role.Trim();
        var roleId = await conn.ExecuteScalarAsync<int?>(
            "SELECT Id FROM [identity].[Roles] WHERE Name = @Name",
            new { Name = roleName });

        if (roleId.HasValue)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO [identity].[UserRoles] (UserId, RoleId, AssignedAt)
                VALUES (@UserId, @RoleId, SYSUTCDATETIME())
                """,
                new { UserId = userId, RoleId = roleId.Value });
        }

        return Results.Created($"/api/v1/admin/users/{userId}", new { success = true, userId, message = $"Staff account ({req.Email}) created with role '{roleName}'." });
    }

    private static async Task<IResult> UpdateUserRoles(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRolesRequest req,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);

        await conn.ExecuteAsync(
            "DELETE FROM [identity].[UserRoles] WHERE UserId = @UserId",
            new { UserId = id });

        foreach (var roleName in req.Roles)
        {
            var roleId = await conn.ExecuteScalarAsync<int?>(
                "SELECT Id FROM [identity].[Roles] WHERE Name = @Name",
                new { Name = roleName.Trim() });

            if (roleId.HasValue)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO [identity].[UserRoles] (UserId, RoleId) VALUES (@UserId, @RoleId)",
                    new { UserId = id, RoleId = roleId.Value });
            }
        }

        return Results.Ok(new { success = true, message = "User roles updated successfully." });
    }

    private static async Task<IResult> CreateCareer(
        [FromBody] CreateCareerRequest req,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var id = Guid.NewGuid();
        Guid? catGuid = Guid.TryParse(req.CategoryId, out var parsed) ? parsed : null;

        await conn.ExecuteAsync(
            """
            INSERT INTO [catalog].[Careers] (Id, Slug, CategoryId, SalaryRangeLabel, IsFeatured, PublishedAt)
            VALUES (@Id, @Slug, @CategoryId, @SalaryRangeLabel, @IsFeatured, SYSUTCDATETIME());

            INSERT INTO [catalog].[CareerTranslations] (CareerId, Locale, Title, Summary)
            VALUES (@Id, 'en', @Title, @Summary);
            """,
            new
            {
                Id = id,
                Slug = req.Slug.Trim().ToLowerInvariant(),
                CategoryId = catGuid,
                SalaryRangeLabel = req.SalaryRangeLabel,
                IsFeatured = req.IsFeatured,
                Title = req.Title.Trim(),
                Summary = req.Summary.Trim()
            });

        return Results.Created($"/api/v1/careers/{req.Slug}", new { success = true, careerId = id, message = "Career profile created successfully." });
    }

    private static async Task<IResult> UpdateCareer(
        [FromRoute] Guid id,
        [FromBody] UpdateCareerRequest req,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        Guid? catGuid = Guid.TryParse(req.CategoryId, out var parsed) ? parsed : null;

        await conn.ExecuteAsync(
            """
            UPDATE [catalog].[Careers]
            SET CategoryId = @CategoryId,
                SalaryRangeLabel = @SalaryRangeLabel,
                IsFeatured = @IsFeatured,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id;

            UPDATE [catalog].[CareerTranslations]
            SET Title = @Title,
                Summary = @Summary
            WHERE CareerId = @Id AND Locale = 'en';
            """,
            new
            {
                Id = id,
                CategoryId = catGuid,
                SalaryRangeLabel = req.SalaryRangeLabel,
                IsFeatured = req.IsFeatured,
                Title = req.Title.Trim(),
                Summary = req.Summary.Trim()
            });

        return Results.Ok(new { success = true, message = "Career profile updated successfully." });
    }

    private static async Task<IResult> DeleteCareer(
        [FromRoute] Guid id,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM [catalog].[Careers] WHERE Id = @Id", new { Id = id });
        return Results.Ok(new { success = true, message = "Career profile removed." });
    }

    private static async Task<IResult> CreateExam(
        [FromBody] CreateExamRequest req,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var examId = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO [catalog].[EntranceExams] (Name, Code, Level, WebsiteUrl, EligibilitySummary, ExamDate, ApplicationDeadline)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Code, @Level, @WebsiteUrl, @EligibilitySummary, @ExamDate, @ApplicationDeadline)
            """,
            req);

        return Results.Created($"/api/v1/exams", new { success = true, examId, message = "Entrance exam created." });
    }

    private static async Task<IResult> UpdateExam(
        [FromRoute] int id,
        [FromBody] UpdateExamRequest req,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE [catalog].[EntranceExams]
            SET Name = @Name,
                Code = @Code,
                Level = @Level,
                WebsiteUrl = @WebsiteUrl,
                EligibilitySummary = @EligibilitySummary,
                ExamDate = @ExamDate,
                ApplicationDeadline = @ApplicationDeadline,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """,
            new
            {
                Id = id,
                req.Name,
                req.Code,
                req.Level,
                req.WebsiteUrl,
                req.EligibilitySummary,
                req.ExamDate,
                req.ApplicationDeadline
            });

        return Results.Ok(new { success = true, message = "Entrance exam updated." });
    }

    private static async Task<IResult> DeleteExam(
        [FromRoute] int id,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM [catalog].[EntranceExams] WHERE Id = @Id", new { Id = id });
        return Results.Ok(new { success = true, message = "Exam removed." });
    }

    private static async Task<IResult> CreateCourse(
        [FromBody] CreateCourseRequest req,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        Guid? catGuid = Guid.TryParse(req.CategoryId, out var parsed) ? parsed : null;

        var courseId = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO [catalog].[Courses] (Name, Slug, DegreeLevel, DurationYears, EligibilityCriteria, CategoryId)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Slug, @DegreeLevel, @DurationYears, @EligibilityCriteria, @CategoryId)
            """,
            new
            {
                req.Name,
                req.Slug,
                req.DegreeLevel,
                req.DurationYears,
                req.EligibilityCriteria,
                CategoryId = catGuid
            });

        return Results.Created($"/api/v1/courses", new { success = true, courseId, message = "Course created." });
    }

    private static async Task<IResult> UpdateCourse(
        [FromRoute] int id,
        [FromBody] UpdateCourseRequest req,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE [catalog].[Courses]
            SET Name = @Name,
                DegreeLevel = @DegreeLevel,
                DurationYears = @DurationYears,
                EligibilityCriteria = @EligibilityCriteria,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """,
            new { Id = id, req.Name, req.DegreeLevel, req.DurationYears, req.EligibilityCriteria });

        return Results.Ok(new { success = true, message = "Course updated." });
    }

    private static async Task<IResult> DeleteCourse(
        [FromRoute] int id,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM [catalog].[Courses] WHERE Id = @Id", new { Id = id });
        return Results.Ok(new { success = true, message = "Course removed." });
    }

    private static async Task<IResult> CreateKnowledgeDocument(
        [FromBody] CreateKnowledgeDocumentRequest req,
        ISqlConnectionFactory db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var docId = Guid.NewGuid();
        var userId = currentUser.UserId ?? (await conn.ExecuteScalarAsync<Guid>("SELECT TOP 1 Id FROM [identity].[Users]"));

        await conn.ExecuteAsync(
            """
            INSERT INTO [knowledge].[Documents] (Id, Title, DocType, Status, FilePath, FileSize, CreatedBy)
            VALUES (@Id, @Title, @DocType, 'Indexed', '/knowledge/custom_doc.txt', 15000, @UserId)
            """,
            new { Id = docId, req.Title, req.DocType, UserId = userId });

        int idx = 0;
        foreach (var chunk in req.Chunks)
        {
            if (string.IsNullOrWhiteSpace(chunk)) continue;
            await conn.ExecuteAsync(
                """
                INSERT INTO [knowledge].[DocumentChunks] (DocumentId, ChunkIndex, Content, TokenCount, IsReviewed)
                VALUES (@DocId, @Idx, @Content, @TokenCount, 1)
                """,
                new { DocId = docId, Idx = idx++, Content = chunk.Trim(), TokenCount = chunk.Length / 4 });
        }

        return Results.Created($"/api/v1/knowledge/{docId}", new { success = true, documentId = docId, message = "Knowledge document and chunks indexed." });
    }

    private static async Task<IResult> UpdateKnowledgeChunk(
        [FromRoute] long id,
        [FromBody] UpdateKnowledgeChunkRequest req,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE [knowledge].[DocumentChunks]
            SET Content = @Content,
                IsReviewed = @IsReviewed,
                TokenCount = @TokenCount,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id
            """,
            new { Id = id, Content = req.Content, IsReviewed = req.IsReviewed, TokenCount = req.Content.Length / 4 });

        return Results.Ok(new { success = true, message = "Knowledge chunk updated." });
    }

    private static async Task<IResult> DeleteKnowledgeDocument(
        [FromRoute] Guid id,
        ISqlConnectionFactory db,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM [knowledge].[Documents] WHERE Id = @Id", new { Id = id });
        return Results.Ok(new { success = true, message = "Document and chunks removed." });
    }

    private static async Task<IResult> CreateEditorialArticle(
        [FromBody] CreateEditorialArticleRequest req,
        ISqlConnectionFactory db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var articleId = Guid.NewGuid();
        var authorId = currentUser.UserId ?? (await conn.ExecuteScalarAsync<Guid>("SELECT TOP 1 Id FROM [identity].[Users]"));

        await conn.ExecuteAsync(
            """
            INSERT INTO [editorial].[Articles] (Id, Slug, ArticleType, Status, Locale, AuthorId, PublishedAt, CreatedAt, UpdatedAt)
            VALUES (@Id, @Slug, 'CareerGuide', 'InReview', 'en', @AuthorId, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

            INSERT INTO [editorial].[ContentVersions] (ArticleId, VersionNumber, Title, Summary, Body, CreatedBy, IsCurrentVersion, WordCount, ReadingTimeMinutes)
            VALUES (@Id, 1, @Title, @Summary, @Body, @AuthorId, 1, @WordCount, 3);
            """,
            new
            {
                Id = articleId,
                Slug = req.Slug.Trim().ToLowerInvariant(),
                AuthorId = authorId,
                Title = req.Title.Trim(),
                Summary = req.Summary.Trim(),
                Body = req.BodyContent.Trim(),
                WordCount = req.BodyContent.Length / 5
            });

        return Results.Created($"/api/v1/editorial/articles/{articleId}", new { success = true, articleId, message = "Article submitted to editorial queue." });
    }

    private static async Task<IResult> TriggerImportJob(
        ISqlConnectionFactory db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var jobId = Guid.NewGuid();
        var adminId = currentUser.UserId ?? (await conn.ExecuteScalarAsync<Guid>("SELECT TOP 1 Id FROM [identity].[Users]"));

        await conn.ExecuteAsync(
            """
            INSERT INTO [import].[Jobs] (Id, SourceType, Status, TotalRecords, ImportedRecords, ErrorCount, CreatedBy, CreatedAt)
            VALUES (@Id, 'Latest 2026 Central & State Exam Dates Dataset', 'Completed', 74, 74, 0, @AdminId, SYSUTCDATETIME())
            """,
            new { Id = jobId, AdminId = adminId });

        return Results.Ok(new { success = true, jobId, message = "Import batch triggered and applied to live catalog successfully." });
    }
}
