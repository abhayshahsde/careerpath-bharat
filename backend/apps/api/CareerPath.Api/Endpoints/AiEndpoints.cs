using MediatR;
using CareerPath.Application.Ai;
using CareerPath.Application.Abstractions;
using CareerPath.Contracts.V1.Ai;

namespace CareerPath.Api.Endpoints;

public static class AiEndpoints
{
    public static void MapAi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai")
            .WithTags("AI Assistant")
            .RequireAuthorization();

        // 1. Submit chat prompt with RAG context
        group.MapPost("/chat", SubmitChat)
            .WithName("SubmitChatPrompt")
            .WithSummary("Send chat messages to RAG guide, retrieve citations and consume quota");

        // 2. Retrieve remaining daily token quota
        group.MapGet("/quota", GetQuotaStatus)
            .WithName("GetQuotaStatus")
            .WithSummary("Check user's max daily token allowance and current daily consumption");
    }

    private static async Task<IResult> SubmitChat(
        ChatRequest req, IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        try
        {
            var userId = currentUser.UserId.GetValueOrDefault();
            var result = await mediator.Send(new ChatCommand(userId, req.Message, req.ConversationId), ct);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetQuotaStatus(
        IMediator mediator, ICurrentUserService currentUser, CancellationToken ct)
    {
        var userId = currentUser.UserId.GetValueOrDefault();
        var result = await mediator.Send(new GetQuotaQuery(userId), ct);
        return Results.Ok(result);
    }
}
