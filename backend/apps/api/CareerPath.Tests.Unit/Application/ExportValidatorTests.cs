using CareerPath.Application.Exports;
using FluentValidation.TestHelper;
using Xunit;

namespace CareerPath.Tests.Unit.Application;

public sealed class ExportValidatorTests
{
    private readonly RequestExportValidator _validator = new();

    [Fact]
    public void RequestExport_Valid_Request_Passes()
    {
        var cmd = new RequestExportCommand(Guid.NewGuid(), "Careers", "CSV");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RequestExport_Invalid_Type_Fails()
    {
        var cmd = new RequestExportCommand(Guid.NewGuid(), "InvalidType", "CSV");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ExportType);
    }

    [Fact]
    public void RequestExport_Invalid_Format_Fails()
    {
        var cmd = new RequestExportCommand(Guid.NewGuid(), "Careers", "PDFX");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Format);
    }

    [Theory]
    [InlineData("PDF")]
    [InlineData("XLSX")]
    [InlineData("CSV")]
    [InlineData("DOCX")]
    public void RequestExport_All_Valid_Formats_Pass(string format)
    {
        var cmd = new RequestExportCommand(Guid.NewGuid(), "Roadmaps", format);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
