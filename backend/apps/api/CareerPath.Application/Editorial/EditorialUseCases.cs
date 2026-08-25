using MediatR;
using FluentValidation;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Editorial;
using CareerPath.Contracts.V1.Common;

namespace CareerPath.Application.Editorial;

// ── List Articles ─────────────────────────────────────────────────────────────

public sealed record ListArticlesQuery(
    string? Status,
    string? ArticleType,
    string? Locale,
    Guid? AuthorId,
    string? Search,
    int Page,
    int PageSize) : IRequest<PagedResponse<ArticleSummaryDto>>;

public sealed class ListArticlesHandler : IRequestHandler<ListArticlesQuery, PagedResponse<ArticleSummaryDto>>
{
    private readonly IEditorialRepository _repo;
    public ListArticlesHandler(IEditorialRepository repo) => _repo = repo;

    public async Task<PagedResponse<ArticleSummaryDto>> Handle(ListArticlesQuery q, CancellationToken ct)
    {
        var req = new PaginationRequest(q.Page, q.PageSize);
        var (items, total) = await _repo.GetArticlesAsync(
            q.Status, q.ArticleType, q.Locale, q.AuthorId, q.Search,
            req.Page, req.PageSize, ct);
        return PagedResponse<ArticleSummaryDto>.Create(items, req.Page, req.PageSize, total);
    }
}

// ── Get Article Detail ────────────────────────────────────────────────────────

public sealed record GetArticleByIdQuery(Guid Id) : IRequest<ArticleDetailDto?>;

public sealed class GetArticleByIdHandler : IRequestHandler<GetArticleByIdQuery, ArticleDetailDto?>
{
    private readonly IEditorialRepository _repo;
    public GetArticleByIdHandler(IEditorialRepository repo) => _repo = repo;
    public Task<ArticleDetailDto?> Handle(GetArticleByIdQuery q, CancellationToken ct)
        => _repo.GetArticleByIdAsync(q.Id, ct);
}

public sealed record GetArticleBySlugQuery(string Slug, string Locale = "en") : IRequest<ArticleDetailDto?>;

public sealed class GetArticleBySlugHandler : IRequestHandler<GetArticleBySlugQuery, ArticleDetailDto?>
{
    private readonly IEditorialRepository _repo;
    public GetArticleBySlugHandler(IEditorialRepository repo) => _repo = repo;
    public Task<ArticleDetailDto?> Handle(GetArticleBySlugQuery q, CancellationToken ct)
        => _repo.GetArticleBySlugAsync(q.Slug, q.Locale, ct);
}

// ── Create Article ─────────────────────────────────────────────────────────────

public sealed record CreateArticleCommand(
    string Slug,
    string ArticleType,
    string Locale,
    Guid AuthorId,
    Guid? LinkedCareerId,
    string Title,
    string Body,
    string? Summary,
    string? MetaDescription,
    string? Keywords) : IRequest<Guid>;

public sealed class CreateArticleValidator : AbstractValidator<CreateArticleCommand>
{
    private static readonly string[] ValidTypes = ["CareerGuide", "ExamGuide", "ScholarshipGuide", "CollegeProfile", "BlogPost"];
    private static readonly string[] ValidLocales = ["en", "hi", "ta", "te", "kn", "mr", "bn", "gu", "ml", "or"];

    public CreateArticleValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(300)
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Slug must be lowercase letters, digits and hyphens only.");
        RuleFor(x => x.ArticleType).Must(t => ValidTypes.Contains(t))
            .WithMessage($"ArticleType must be one of: {string.Join(", ", ValidTypes)}");
        RuleFor(x => x.Locale).Must(l => ValidLocales.Contains(l))
            .WithMessage($"Locale must be one of: {string.Join(", ", ValidLocales)}");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Body).NotEmpty().MinimumLength(50);
        RuleFor(x => x.Summary).MaximumLength(1000).When(x => x.Summary is not null);
        RuleFor(x => x.MetaDescription).MaximumLength(300).When(x => x.MetaDescription is not null);
    }
}

public sealed class CreateArticleHandler : IRequestHandler<CreateArticleCommand, Guid>
{
    private readonly IEditorialRepository _repo;
    public CreateArticleHandler(IEditorialRepository repo) => _repo = repo;

    public Task<Guid> Handle(CreateArticleCommand cmd, CancellationToken ct)
        => _repo.CreateArticleAsync(
            cmd.Slug, cmd.ArticleType, cmd.Locale, cmd.AuthorId, cmd.LinkedCareerId,
            cmd.Title, cmd.Body, cmd.Summary, cmd.MetaDescription, cmd.Keywords, ct);
}

// ── Save Version ──────────────────────────────────────────────────────────────

public sealed record SaveVersionCommand(
    Guid ArticleId,
    Guid EditorId,
    string Title,
    string Body,
    string? Summary,
    string? MetaDescription,
    string? Keywords,
    string? ChangeNote) : IRequest<int>;

public sealed class SaveVersionValidator : AbstractValidator<SaveVersionCommand>
{
    public SaveVersionValidator()
    {
        RuleFor(x => x.ArticleId).NotEmpty();
        RuleFor(x => x.EditorId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Body).NotEmpty().MinimumLength(50);
        RuleFor(x => x.ChangeNote).MaximumLength(500).When(x => x.ChangeNote is not null);
    }
}

public sealed class SaveVersionHandler : IRequestHandler<SaveVersionCommand, int>
{
    private readonly IEditorialRepository _repo;
    public SaveVersionHandler(IEditorialRepository repo) => _repo = repo;

    public Task<int> Handle(SaveVersionCommand cmd, CancellationToken ct)
        => _repo.SaveVersionAsync(
            cmd.ArticleId, cmd.EditorId, cmd.Title, cmd.Body,
            cmd.Summary, cmd.MetaDescription, cmd.Keywords, cmd.ChangeNote, ct);
}

// ── Workflow Transitions ──────────────────────────────────────────────────────

public sealed record SubmitForReviewCommand(
    Guid ArticleId, Guid RequesterId,
    Guid? ReviewerId, DateTimeOffset? DueBy) : IRequest;

public sealed class SubmitForReviewHandler : IRequestHandler<SubmitForReviewCommand>
{
    private readonly IEditorialRepository _repo;
    public SubmitForReviewHandler(IEditorialRepository repo) => _repo = repo;
    public Task Handle(SubmitForReviewCommand cmd, CancellationToken ct)
        => _repo.SubmitForReviewAsync(cmd.ArticleId, cmd.RequesterId, cmd.ReviewerId, cmd.DueBy, ct);
}

public sealed record RecordReviewDecisionCommand(
    Guid ArticleId, int ReviewRequestId, Guid ReviewerId,
    string Decision, string? Feedback) : IRequest;

public sealed class RecordReviewDecisionValidator : AbstractValidator<RecordReviewDecisionCommand>
{
    public RecordReviewDecisionValidator()
    {
        RuleFor(x => x.Decision).Must(d => d is "Approve" or "RequestChanges" or "Reject")
            .WithMessage("Decision must be Approve, RequestChanges, or Reject.");
        RuleFor(x => x.Feedback).NotEmpty()
            .When(x => x.Decision is "RequestChanges" or "Reject")
            .WithMessage("Feedback is required when requesting changes or rejecting.");
        RuleFor(x => x.Feedback).MaximumLength(2000).When(x => x.Feedback is not null);
    }
}

public sealed class RecordReviewDecisionHandler : IRequestHandler<RecordReviewDecisionCommand>
{
    private readonly IEditorialRepository _repo;
    public RecordReviewDecisionHandler(IEditorialRepository repo) => _repo = repo;
    public Task Handle(RecordReviewDecisionCommand cmd, CancellationToken ct)
        => _repo.RecordReviewDecisionAsync(cmd.ArticleId, cmd.ReviewRequestId, cmd.ReviewerId, cmd.Decision, cmd.Feedback, ct);
}

public sealed record PublishArticleCommand(
    Guid ArticleId, Guid ActorId, DateTimeOffset? ScheduledAt) : IRequest;

public sealed class PublishArticleHandler : IRequestHandler<PublishArticleCommand>
{
    private readonly IEditorialRepository _repo;
    public PublishArticleHandler(IEditorialRepository repo) => _repo = repo;
    public Task Handle(PublishArticleCommand cmd, CancellationToken ct)
        => _repo.PublishArticleAsync(cmd.ArticleId, cmd.ActorId, cmd.ScheduledAt, ct);
}

public sealed record ArchiveArticleCommand(Guid ArticleId, Guid ActorId) : IRequest;

public sealed class ArchiveArticleHandler : IRequestHandler<ArchiveArticleCommand>
{
    private readonly IEditorialRepository _repo;
    public ArchiveArticleHandler(IEditorialRepository repo) => _repo = repo;
    public Task Handle(ArchiveArticleCommand cmd, CancellationToken ct)
        => _repo.ArchiveArticleAsync(cmd.ArticleId, cmd.ActorId, ct);
}
