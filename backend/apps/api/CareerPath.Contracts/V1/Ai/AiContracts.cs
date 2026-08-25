namespace CareerPath.Contracts.V1.Ai;

public sealed record ChatRequest(
    string Message,
    string? ConversationId);

public sealed record ChatResponse(
    string Reply,
    string ConversationId,
    IReadOnlyList<CitationDto> Citations,
    int TokensUsed);

public sealed record CitationDto(
    Guid DocumentId,
    string DocumentTitle,
    string DocType,
    int ChunkIndex,
    string Content);

public sealed record QuotaStatusDto(
    int MaxDailyTokens,
    int UsedDailyTokens,
    DateTimeOffset ResetAt);
