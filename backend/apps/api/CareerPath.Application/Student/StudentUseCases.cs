using MediatR;
using FluentValidation;
using CareerPath.Contracts.V1.Student;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Application.Abstractions;

namespace CareerPath.Application.Student;

// ─── Get Profile ────────────────────────────────────────────────────────────

public sealed record GetMyProfileQuery(Guid UserId) : IRequest<StudentProfileResponse?>;

public sealed class GetMyProfileHandler : IRequestHandler<GetMyProfileQuery, StudentProfileResponse?>
{
    private readonly IStudentProfileRepository _repo;

    public GetMyProfileHandler(IStudentProfileRepository repo) => _repo = repo;

    public async Task<StudentProfileResponse?> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _repo.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null) return null;

        return new StudentProfileResponse(
            UserId: profile.UserId,
            DisplayName: profile.DisplayName,
            AvatarUrl: profile.AvatarUrl,
            CurrentEducationLevel: profile.CurrentEducationLevel,
            StateOfResidence: profile.StateOfResidence,
            PreferredLocale: profile.PreferredLocale,
            SchoolBoard: profile.SchoolBoard,
            StreamOrSubjects: profile.StreamOrSubjects,
            Interests: profile.Interests,
            IsOnboardingComplete: profile.IsOnboardingComplete,
            UpdatedAt: profile.UpdatedAt
        );
    }
}

// ─── Upsert Profile ──────────────────────────────────────────────────────────

public sealed record UpsertProfileCommand(
    Guid UserId,
    string? DisplayName,
    string? CurrentEducationLevel,
    string? StateOfResidence,
    string? PreferredLocale,
    string? SchoolBoard,
    string? StreamOrSubjects,
    IReadOnlyList<string>? Interests
) : IRequest<StudentProfileResponse>;

public sealed class UpsertProfileValidator : AbstractValidator<UpsertProfileCommand>
{
    private static readonly string[] AllowedLocales = ["en", "hi"];

    public UpsertProfileValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(100)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.CurrentEducationLevel)
            .MaximumLength(100)
            .When(x => x.CurrentEducationLevel is not null);

        RuleFor(x => x.StateOfResidence)
            .MaximumLength(100)
            .When(x => x.StateOfResidence is not null);

        RuleFor(x => x.PreferredLocale)
            .Must(l => AllowedLocales.Contains(l))
            .WithMessage("Locale must be one of: en, hi")
            .When(x => x.PreferredLocale is not null);
    }
}

public sealed class UpsertProfileHandler : IRequestHandler<UpsertProfileCommand, StudentProfileResponse>
{
    private readonly IStudentProfileRepository _repo;

    public UpsertProfileHandler(IStudentProfileRepository repo) => _repo = repo;

    public async Task<StudentProfileResponse> Handle(UpsertProfileCommand request, CancellationToken cancellationToken)
    {
        await _repo.UpsertAsync(
            request.UserId,
            request.DisplayName,
            request.CurrentEducationLevel,
            request.StateOfResidence,
            request.PreferredLocale,
            request.SchoolBoard,
            request.StreamOrSubjects,
            request.Interests,
            cancellationToken);

        var profile = (await _repo.GetByUserIdAsync(request.UserId, cancellationToken))!;

        return new StudentProfileResponse(
            UserId: profile.UserId,
            DisplayName: profile.DisplayName,
            AvatarUrl: profile.AvatarUrl,
            CurrentEducationLevel: profile.CurrentEducationLevel,
            StateOfResidence: profile.StateOfResidence,
            PreferredLocale: profile.PreferredLocale,
            SchoolBoard: profile.SchoolBoard,
            StreamOrSubjects: profile.StreamOrSubjects,
            Interests: profile.Interests,
            IsOnboardingComplete: profile.IsOnboardingComplete,
            UpdatedAt: profile.UpdatedAt
        );
    }
}

// ─── Save / Unsave Career ────────────────────────────────────────────────────

public sealed record SaveCareerCommand(Guid UserId, Guid CareerId) : IRequest<bool>;

public sealed class SaveCareerHandler : IRequestHandler<SaveCareerCommand, bool>
{
    private readonly IStudentProfileRepository _repo;
    public SaveCareerHandler(IStudentProfileRepository repo) => _repo = repo;
    public Task<bool> Handle(SaveCareerCommand request, CancellationToken cancellationToken) =>
        _repo.SaveItemAsync(request.UserId, "Career", request.CareerId, cancellationToken);
}

public sealed record UnsaveCareerCommand(Guid UserId, Guid CareerId) : IRequest<bool>;

public sealed class UnsaveCareerHandler : IRequestHandler<UnsaveCareerCommand, bool>
{
    private readonly IStudentProfileRepository _repo;
    public UnsaveCareerHandler(IStudentProfileRepository repo) => _repo = repo;
    public Task<bool> Handle(UnsaveCareerCommand request, CancellationToken cancellationToken) =>
        _repo.UnsaveItemAsync(request.UserId, "Career", request.CareerId, cancellationToken);
}

// ─── Save / Unsave Course ────────────────────────────────────────────────────

public sealed record SaveCourseCommand(Guid UserId, int CourseId) : IRequest<bool>;

public sealed class SaveCourseHandler : IRequestHandler<SaveCourseCommand, bool>
{
    private readonly IStudentProfileRepository _repo;
    public SaveCourseHandler(IStudentProfileRepository repo) => _repo = repo;
    
    public Task<bool> Handle(SaveCourseCommand request, CancellationToken cancellationToken)
    {
        var bytes = new byte[16];
        System.BitConverter.GetBytes(request.CourseId).CopyTo(bytes, 0);
        var itemId = new System.Guid(bytes);
        return _repo.SaveItemAsync(request.UserId, "Course", itemId, cancellationToken);
    }
}

public sealed record UnsaveCourseCommand(Guid UserId, int CourseId) : IRequest<bool>;

public sealed class UnsaveCourseHandler : IRequestHandler<UnsaveCourseCommand, bool>
{
    private readonly IStudentProfileRepository _repo;
    public UnsaveCourseHandler(IStudentProfileRepository repo) => _repo = repo;

    public Task<bool> Handle(UnsaveCourseCommand request, CancellationToken cancellationToken)
    {
        var bytes = new byte[16];
        System.BitConverter.GetBytes(request.CourseId).CopyTo(bytes, 0);
        var itemId = new System.Guid(bytes);
        return _repo.UnsaveItemAsync(request.UserId, "Course", itemId, cancellationToken);
    }
}

// ─── Get Saved Careers / Courses ─────────────────────────────────────────────

public sealed record GetSavedCareersQuery(Guid UserId, string Locale) : IRequest<IReadOnlyList<SavedCareerResponse>>;

public sealed class GetSavedCareersHandler : IRequestHandler<GetSavedCareersQuery, IReadOnlyList<SavedCareerResponse>>
{
    private readonly IStudentProfileRepository _repo;
    public GetSavedCareersHandler(IStudentProfileRepository repo) => _repo = repo;
    
    public Task<IReadOnlyList<SavedCareerResponse>> Handle(GetSavedCareersQuery request, CancellationToken cancellationToken) =>
        _repo.GetSavedCareersWithDetailsAsync(request.UserId, request.Locale, cancellationToken);
}

public sealed record GetSavedCoursesQuery(Guid UserId, string Locale) : IRequest<IReadOnlyList<SavedCourseResponse>>;

public sealed class GetSavedCoursesHandler : IRequestHandler<GetSavedCoursesQuery, IReadOnlyList<SavedCourseResponse>>
{
    private readonly IStudentProfileRepository _repo;
    public GetSavedCoursesHandler(IStudentProfileRepository repo) => _repo = repo;

    public Task<IReadOnlyList<SavedCourseResponse>> Handle(GetSavedCoursesQuery request, CancellationToken cancellationToken) =>
        _repo.GetSavedCoursesWithDetailsAsync(request.UserId, request.Locale, cancellationToken);
}
