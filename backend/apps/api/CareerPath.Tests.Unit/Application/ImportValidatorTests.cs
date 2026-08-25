using CareerPath.Application.Imports;
using FluentValidation.TestHelper;
using Xunit;

namespace CareerPath.Tests.Unit.Application;

public sealed class ImportValidatorTests
{
    private readonly GetUploadUrlValidator _uploadUrlValidator = new();
    private readonly ReviewImportJobValidator _reviewValidator = new();

    [Fact]
    public void GetUploadUrl_Valid_Request_Passes()
    {
        var query = new GetUploadUrlQuery(
            Guid.NewGuid(),
            "Careers",
            "careers_bulk_import.json",
            "application/json",
            1024 * 50); // 50 KB

        _uploadUrlValidator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetUploadUrl_Invalid_Type_Fails()
    {
        var query = new GetUploadUrlQuery(
            Guid.NewGuid(),
            "InvalidType",
            "careers.json",
            "application/json",
            1024 * 50);

        _uploadUrlValidator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.ImportType);
    }

    [Fact]
    public void GetUploadUrl_Unsupported_Extension_Fails()
    {
        var query = new GetUploadUrlQuery(
            Guid.NewGuid(),
            "Careers",
            "careers.pdf",
            "application/pdf",
            1024 * 50);

        _uploadUrlValidator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void GetUploadUrl_Exceeds_Size_Limit_Fails()
    {
        var query = new GetUploadUrlQuery(
            Guid.NewGuid(),
            "Careers",
            "careers.json",
            "application/json",
            20 * 1024 * 1024); // 20 MB

        _uploadUrlValidator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.FileSize);
    }

    [Fact]
    public void ReviewImport_Valid_Notes_Passes()
    {
        var cmd = new ReviewImportJobCommand(Guid.NewGuid(), Guid.NewGuid(), true, "Approved after verifying constraints.");
        _reviewValidator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReviewImport_Too_Long_Notes_Fails()
    {
        var cmd = new ReviewImportJobCommand(Guid.NewGuid(), Guid.NewGuid(), true, new string('A', 1001));
        _reviewValidator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Notes);
    }
}
