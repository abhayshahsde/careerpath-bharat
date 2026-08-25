using MediatR;
using FluentValidation;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Application.Abstractions.Services;
using CareerPath.Contracts.V1.Ai;

namespace CareerPath.Application.Ai;

// ── Quota Query ──────────────────────────────────────────────────────────────

public sealed record GetQuotaQuery(Guid UserId) : IRequest<QuotaStatusDto>;

public sealed class GetQuotaHandler : IRequestHandler<GetQuotaQuery, QuotaStatusDto>
{
    private readonly IAiRepository _repo;
    public GetQuotaHandler(IAiRepository repo) => _repo = repo;
    public Task<QuotaStatusDto> Handle(GetQuotaQuery q, CancellationToken ct)
        => _repo.GetOrCreateQuotaAsync(q.UserId, ct);
}

// ── Chat Command ──────────────────────────────────────────────────────────────

public sealed record ChatCommand(
    Guid UserId, string Message, string? ConversationId) : IRequest<ChatResponse>;

public sealed class ChatValidator : AbstractValidator<ChatCommand>
{
    public ChatValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000)
            .WithMessage("Chat message must be between 1 and 2000 characters.");
    }
}

public sealed class ChatHandler : IRequestHandler<ChatCommand, ChatResponse>
{
    private readonly IAiRepository _repo;
    private readonly IAiService _aiService;
    private readonly IStudentProfileRepository _profileRepo;

    public ChatHandler(IAiRepository repo, IAiService aiService, IStudentProfileRepository profileRepo)
    {
        _repo = repo;
        _aiService = aiService;
        _profileRepo = profileRepo;
    }

    public async Task<ChatResponse> Handle(ChatCommand cmd, CancellationToken ct)
    {
        // 1. Verify and reset/get daily token quotas
        var quota = await _repo.GetOrCreateQuotaAsync(cmd.UserId, ct);

        // 2. Fetch student profile to ground the answer in their personal qualification and interests
        var profile = await _profileRepo.GetByUserIdAsync(cmd.UserId, ct);

        // 3. Perform RAG context search from the staged syllabus document chunks
        var citations = await _repo.SearchKnowledgeBaseAsync(cmd.Message, maxResults: 3, ct: ct);

        // 4. Formulate the contextual instructions
        var systemMessage = "You are the CareerPath Bharat AI Guide, an intelligent counselor for Indian students. " +
                            "You provide personalized career, exam, and course guidance based on the student's qualification and query. " +
                            "Support Hinglish, Hindi, and English seamlessly.";

        var promptBuilder = new System.Text.StringBuilder();

        if (profile is not null)
        {
            promptBuilder.AppendLine("--- Student Profile Context ---");
            promptBuilder.AppendLine($"Name: {profile.DisplayName ?? "Student"}");
            promptBuilder.AppendLine($"Education Level: {profile.CurrentEducationLevel ?? "Not specified"}");
            promptBuilder.AppendLine($"School Board: {profile.SchoolBoard ?? "Not specified"}");
            promptBuilder.AppendLine($"Stream/Subjects: {profile.StreamOrSubjects ?? "Not specified"}");
            promptBuilder.AppendLine($"State: {profile.StateOfResidence ?? "Not specified"}");
            if (profile.Interests is not null && profile.Interests.Count > 0)
            {
                promptBuilder.AppendLine($"Career Interests: {string.Join(", ", profile.Interests)}");
            }
            promptBuilder.AppendLine("-------------------------------");
            promptBuilder.AppendLine();
        }

        if (citations.Count > 0)
        {
            promptBuilder.AppendLine("Use the following document chunks as context to ground your answer:");
            foreach (var cite in citations)
            {
                promptBuilder.AppendLine($"[Doc: {cite.DocumentTitle}, Chunk Index: {cite.ChunkIndex}]");
                promptBuilder.AppendLine(cite.Content);
                promptBuilder.AppendLine();
            }
        }

        promptBuilder.AppendLine($"User Question: {cmd.Message}");

        // 5. Invoke LLM generation
        var (reply, tokensUsed) = await _aiService.GenerateCompletionAsync(promptBuilder.ToString(), systemMessage, ct);

        // 5. Consume token quota
        var success = await _repo.CheckAndConsumeQuotaAsync(cmd.UserId, tokensUsed, ct);
        if (!success)
        {
            throw new InvalidOperationException("Daily AI query quota limit exceeded. Please try again tomorrow.");
        }

        // 6. Audit log usage statistics
        await _repo.LogUsageAsync(cmd.UserId, "Chat", "gemini-1.5-flash", promptTokens: tokensUsed / 2, completionTokens: tokensUsed / 2, ct: ct);

        var convoId = cmd.ConversationId ?? Guid.NewGuid().ToString();

        return new ChatResponse(reply, convoId, citations, tokensUsed);
    }
}
