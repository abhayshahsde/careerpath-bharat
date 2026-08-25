using CareerPath.Application.NotificationAnalytics;
using FluentValidation.TestHelper;
using Xunit;

namespace CareerPath.Tests.Unit.Application;

public sealed class NotificationAnalyticsValidatorTests
{
    private readonly LogTelemetryValidator _telemetryValidator = new();

    [Fact]
    public void LogTelemetry_Valid_Event_Passes()
    {
        var cmd = new LogTelemetryCommand(Guid.NewGuid(), "CareerDetailViewed", "{\"slug\":\"software-engineer\"}");
        _telemetryValidator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LogTelemetry_Empty_EventName_Fails()
    {
        var cmd = new LogTelemetryCommand(Guid.NewGuid(), "", "{\"slug\":\"software-engineer\"}");
        _telemetryValidator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.EventName);
    }

    [Fact]
    public void LogTelemetry_Too_Long_EventName_Fails()
    {
        var cmd = new LogTelemetryCommand(Guid.NewGuid(), new string('A', 101), "{}");
        _telemetryValidator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.EventName);
    }

    [Fact]
    public void LogTelemetry_Too_Long_Payload_Fails()
    {
        var cmd = new LogTelemetryCommand(Guid.NewGuid(), "Click", new string('B', 8001));
        _telemetryValidator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PayloadJson);
    }
}
