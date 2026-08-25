namespace CareerPath.Application.Abstractions.Services;

public interface IAiService
{
    Task<(string Reply, int TokensUsed)> GenerateCompletionAsync(string prompt, string systemMessage, CancellationToken ct = default);
}
