using CareerPath.Contracts.V1.Ai;

namespace CareerPath.Application.Abstractions.Repositories;

public interface IAiRepository
{
    Task<QuotaStatusDto> GetOrCreateQuotaAsync(Guid userId, CancellationToken ct = default);
    Task<bool> CheckAndConsumeQuotaAsync(Guid userId, int tokens, CancellationToken ct = default);
    Task LogUsageAsync(Guid userId, string requestType, string modelName, int promptTokens, int completionTokens, CancellationToken ct = default);
    Task<IReadOnlyList<CitationDto>> SearchKnowledgeBaseAsync(string queryText, int maxResults = 3, CancellationToken ct = default);
}
