using Dapper;
using System.Linq;
using CareerPath.Domain.Entities;
using CareerPath.Infrastructure.Data;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Student;

namespace CareerPath.Infrastructure.Repositories;

public sealed class StudentProfileRepository : IStudentProfileRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public StudentProfileRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<StudentProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<dynamic>(
            """
            SELECT
                p.Id,
                p.UserId,
                p.DisplayName,
                p.AvatarUrl,
                p.CurrentEducationLevel,
                p.StateOfResidence,
                p.PreferredLocale,
                p.SchoolBoard,
                p.StreamOrSubjects,
                p.IsOnboardingComplete,
                p.CreatedAt,
                p.UpdatedAt
            FROM [student].[Profiles] p
            WHERE p.UserId = @UserId
            """,
            new { UserId = userId });

        if (row is null) return null;

        var interestsRows = await connection.QueryAsync<string>(
            "SELECT CategoryId FROM [student].[CareerInterests] WHERE UserId = @UserId",
            new { UserId = userId });
        var interestsList = interestsRows.ToList();

        var profile = StudentProfile.Create(userId, (DateTimeOffset)row.CreatedAt);
        profile.Update(
            (string?)row.DisplayName,
            (string?)row.CurrentEducationLevel,
            (string?)row.StateOfResidence,
            (string?)row.PreferredLocale,
            (string?)row.SchoolBoard,
            (string?)row.StreamOrSubjects,
            interestsList,
            (DateTimeOffset)row.UpdatedAt);

        return profile;
    }

    public async Task UpsertAsync(
        Guid userId,
        string? displayName,
        string? educationLevel,
        string? state,
        string? locale,
        string? schoolBoard,
        string? streamOrSubjects,
        IReadOnlyList<string>? interests,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(
                """
                MERGE [student].[Profiles] AS target
                USING (SELECT @UserId AS UserId) AS source ON target.UserId = source.UserId
                WHEN MATCHED THEN
                    UPDATE SET
                        DisplayName = @DisplayName,
                        CurrentEducationLevel = @EducationLevel,
                        StateOfResidence = @State,
                        PreferredLocale = @Locale,
                        SchoolBoard = @SchoolBoard,
                        StreamOrSubjects = @StreamOrSubjects,
                        UpdatedAt = @Now
                WHEN NOT MATCHED THEN
                    INSERT (Id, UserId, DisplayName, CurrentEducationLevel, StateOfResidence, PreferredLocale, SchoolBoard, StreamOrSubjects, IsOnboardingComplete, CreatedAt, UpdatedAt)
                    VALUES (NEWID(), @UserId, @DisplayName, @EducationLevel, @State, @Locale, @SchoolBoard, @StreamOrSubjects, 0, @Now, @Now);
                """,
                new
                {
                    UserId = userId,
                    DisplayName = displayName,
                    EducationLevel = educationLevel,
                    State = state,
                    Locale = locale,
                    SchoolBoard = schoolBoard,
                    StreamOrSubjects = streamOrSubjects,
                    Now = DateTimeOffset.UtcNow
                },
                transaction);

            // Delete old interests
            await connection.ExecuteAsync(
                "DELETE FROM [student].[CareerInterests] WHERE UserId = @UserId",
                new { UserId = userId },
                transaction);

            // Add new interests
            if (interests != null && interests.Any())
            {
                var insertQuery = "INSERT INTO [student].[CareerInterests] (UserId, CategoryId, Rank, CreatedAt) VALUES (@UserId, @CategoryId, 0, SYSUTCDATETIME())";
                var paramsList = interests.Select(catId => new { UserId = userId, CategoryId = catId }).ToList();
                await connection.ExecuteAsync(insertQuery, paramsList, transaction);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<SavedItem>> GetSavedItemsAsync(
        Guid userId,
        string itemType,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<SavedItem>(
            """
            SELECT
                si.Id,
                si.UserId,
                si.ItemType,
                si.ItemId,
                si.SavedAt
            FROM [student].[SavedItems] si
            WHERE si.UserId = @UserId
              AND si.ItemType = @ItemType
            ORDER BY si.SavedAt DESC
            """,
            new { UserId = userId, ItemType = itemType });

        return rows.ToList();
    }

    public async Task<bool> SaveItemAsync(
        Guid userId,
        string itemType,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var existing = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [student].[SavedItems] WHERE UserId = @UserId AND ItemType = @ItemType AND ItemId = @ItemId",
            new { UserId = userId, ItemType = itemType, ItemId = itemId });

        if (existing > 0) return false;

        await connection.ExecuteAsync(
            "INSERT INTO [student].[SavedItems] (Id, UserId, ItemType, ItemId, SavedAt) VALUES (NEWID(), @UserId, @ItemType, @ItemId, @Now)",
            new { UserId = userId, ItemType = itemType, ItemId = itemId, Now = DateTimeOffset.UtcNow });

        return true;
    }

    public async Task<bool> UnsaveItemAsync(
        Guid userId,
        string itemType,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            "DELETE FROM [student].[SavedItems] WHERE UserId = @UserId AND ItemType = @ItemType AND ItemId = @ItemId",
            new { UserId = userId, ItemType = itemType, ItemId = itemId });

        return affected > 0;
    }

    public async Task<IReadOnlyList<SavedCareerResponse>> GetSavedCareersWithDetailsAsync(
        Guid userId,
        string locale,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<SavedCareerResponse>(
            """
            SELECT
                si.Id,
                si.ItemId AS CareerId,
                COALESCE(ct.Title, c.Slug) AS CareerTitle,
                c.Slug AS CareerSlug,
                si.SavedAt
            FROM [student].[SavedItems] si
            JOIN [catalog].[Careers] c ON si.ItemId = c.Id
            LEFT JOIN [catalog].[CareerTranslations] ct ON c.Id = ct.CareerId AND ct.Locale = @Locale
            WHERE si.UserId = @UserId
              AND si.ItemType = 'Career'
            ORDER BY si.SavedAt DESC
            """,
            new { UserId = userId, Locale = locale });

        return rows.ToList();
    }

    public async Task<IReadOnlyList<SavedCourseResponse>> GetSavedCoursesWithDetailsAsync(
        Guid userId,
        string locale,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // 1. Get all saved items of type 'Course' for this user
        var savedItems = await connection.QueryAsync<SavedItem>(
            """
            SELECT Id, UserId, ItemType, ItemId, SavedAt
            FROM [student].[SavedItems]
            WHERE UserId = @UserId AND ItemType = 'Course'
            """,
            new { UserId = userId });

        var savedItemsList = savedItems.ToList();
        if (!savedItemsList.Any()) return Array.Empty<SavedCourseResponse>();

        // 2. Decode GUIDs to Course IDs
        var courseIdMap = savedItemsList.ToDictionary(
            x => BitConverter.ToInt32(x.ItemId.ToByteArray(), 0),
            x => x
        );
        var courseIds = courseIdMap.Keys.ToList();

        // 3. Query course details
        var courses = await connection.QueryAsync(
            """
            SELECT
                c.Id AS CourseId,
                COALESCE(ct.Name, c.Name) AS CourseName,
                c.Slug AS CourseSlug,
                c.DegreeLevel,
                c.DurationYears
            FROM [catalog].[Courses] c
            LEFT JOIN [catalog].[CourseTranslations] ct ON c.Id = ct.CourseId AND ct.Locale = @Locale
            WHERE c.Id IN @Ids
            """,
            new { Ids = courseIds, Locale = locale });

        // 4. Map back to SavedCourseResponse
        var results = new List<SavedCourseResponse>();
        foreach (var c in courses)
        {
            int courseId = c.CourseId;
            if (courseIdMap.TryGetValue(courseId, out var savedItem))
            {
                results.Add(new SavedCourseResponse(
                    Id: savedItem.Id,
                    CourseId: courseId,
                    CourseName: c.CourseName,
                    CourseSlug: c.CourseSlug,
                    DegreeLevel: c.DegreeLevel,
                    DurationYears: (decimal)c.DurationYears,
                    SavedAt: savedItem.SavedAt
                ));
            }
        }

        return results.OrderByDescending(r => r.SavedAt).ToList();
    }
}
