using CareerPath.Application.Editorial;
using FluentValidation.TestHelper;
using Xunit;

namespace CareerPath.Tests.Unit.Application;

public sealed class CreateArticleValidatorTests
{
    private readonly CreateArticleValidator _v = new();

    [Fact]
    public void Valid_Article_Passes()
    {
        var cmd = new CreateArticleCommand(
            "software-engineering-guide", "CareerGuide", "en",
            Guid.NewGuid(), null,
            "Complete Software Engineering Career Guide",
            "This is the body of the article with more than fifty characters to pass validation.",
            "A guide for aspiring software engineers in India.", null, null);

        _v.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Invalid Slug!")]
    [InlineData("UPPERCASE")]
    [InlineData("has spaces here")]
    public void Invalid_Slug_Fails(string slug)
    {
        var cmd = new CreateArticleCommand(slug, "CareerGuide", "en",
            Guid.NewGuid(), null, "Title", "Body text that is at least fifty characters long yes", null, null, null);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Invalid_ArticleType_Fails()
    {
        var cmd = new CreateArticleCommand("valid-slug", "UnknownType", "en",
            Guid.NewGuid(), null, "Title", "Body text that is at least fifty characters long yes.", null, null, null);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ArticleType);
    }

    [Fact]
    public void Invalid_Locale_Fails()
    {
        var cmd = new CreateArticleCommand("valid-slug", "CareerGuide", "xx",
            Guid.NewGuid(), null, "Title", "Body text that is at least fifty characters long yes.", null, null, null);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Locale);
    }

    [Fact]
    public void Body_Too_Short_Fails()
    {
        var cmd = new CreateArticleCommand("valid-slug", "CareerGuide", "en",
            Guid.NewGuid(), null, "Title", "Too short.", null, null, null);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Body);
    }
}

public sealed class ReviewDecisionValidatorTests
{
    private readonly RecordReviewDecisionValidator _v = new();

    [Fact]
    public void Approve_Without_Feedback_Passes()
    {
        var cmd = new RecordReviewDecisionCommand(Guid.NewGuid(), 1, Guid.NewGuid(), "Approve", null);
        _v.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RequestChanges_Requires_Feedback()
    {
        var cmd = new RecordReviewDecisionCommand(Guid.NewGuid(), 1, Guid.NewGuid(), "RequestChanges", null);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Feedback);
    }

    [Fact]
    public void Reject_Requires_Feedback()
    {
        var cmd = new RecordReviewDecisionCommand(Guid.NewGuid(), 1, Guid.NewGuid(), "Reject", null);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Feedback);
    }

    [Fact]
    public void Invalid_Decision_Fails()
    {
        var cmd = new RecordReviewDecisionCommand(Guid.NewGuid(), 1, Guid.NewGuid(), "DoSomethingElse", null);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Decision);
    }

    [Fact]
    public void RequestChanges_With_Feedback_Passes()
    {
        var cmd = new RecordReviewDecisionCommand(Guid.NewGuid(), 1, Guid.NewGuid(),
            "RequestChanges", "Please fix the introduction section.");
        _v.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
