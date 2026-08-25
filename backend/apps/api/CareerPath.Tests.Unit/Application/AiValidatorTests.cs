using CareerPath.Application.Ai;
using FluentValidation.TestHelper;
using Xunit;

namespace CareerPath.Tests.Unit.Application;

public sealed class AiValidatorTests
{
    private readonly ChatValidator _chatValidator = new();

    [Fact]
    public void ChatCommand_Valid_Request_Passes()
    {
        var cmd = new ChatCommand(Guid.NewGuid(), "How do I prepare for UPSC exam?", "convo_123");
        _chatValidator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ChatCommand_Empty_Message_Fails()
    {
        var cmd = new ChatCommand(Guid.NewGuid(), "", "convo_123");
        _chatValidator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Message);
    }

    [Fact]
    public void ChatCommand_Too_Long_Message_Fails()
    {
        var cmd = new ChatCommand(Guid.NewGuid(), new string('A', 2001), "convo_123");
        _chatValidator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Message);
    }
}
