using CareerPath.Application.Imports;
using FluentValidation.TestHelper;
using Xunit;

namespace CareerPath.Tests.Unit.Application;

public sealed class KnowledgeValidatorTests
{
    private readonly UploadDocumentValidator _uploadValidator = new();
    private readonly UpdateChunkValidator _chunkValidator = new();
    private readonly ReviewDocumentValidator _reviewValidator = new();

    [Fact]
    public void UploadDocument_Valid_Request_Passes()
    {
        var cmd = new UploadDocumentCommand(Guid.NewGuid(), "UPSC Syllabus 2026", "Syllabus", "upsc_2026.pdf", 1024 * 500);
        _uploadValidator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UploadDocument_Invalid_Type_Fails()
    {
        var cmd = new UploadDocumentCommand(Guid.NewGuid(), "UPSC Syllabus", "InvalidType", "upsc.pdf", 1024 * 500);
        _uploadValidator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DocType);
    }

    [Fact]
    public void UploadDocument_Unsupported_Extension_Fails()
    {
        var cmd = new UploadDocumentCommand(Guid.NewGuid(), "Syllabus", "Syllabus", "syllabus.png", 1024 * 500);
        _uploadValidator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void UploadDocument_Empty_Title_Fails()
    {
        var cmd = new UploadDocumentCommand(Guid.NewGuid(), "", "Syllabus", "syllabus.pdf", 1024 * 500);
        _uploadValidator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void UpdateChunk_Valid_Request_Passes()
    {
        var cmd = new UpdateChunkCommand(1234, "Updated study plan content.", true);
        _chunkValidator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateChunk_Empty_Content_Fails()
    {
        var cmd = new UpdateChunkCommand(1234, "", true);
        _chunkValidator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void ReviewDocument_Valid_Notes_Passes()
    {
        var cmd = new ReviewDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), true, "Approved for Vector Indexing.");
        _reviewValidator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
