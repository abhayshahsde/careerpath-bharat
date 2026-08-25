using CareerPath.Application.Student;
using CareerPath.Contracts.V1.Common;
using CareerPath.Domain.Entities;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CareerPath.Tests.Unit.Application;

public class UpsertProfileValidatorTests
{
    private readonly UpsertProfileValidator _validator = new();

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var cmd = new UpsertProfileCommand(Guid.NewGuid(), "Ravi Kumar", "Class 12", "Maharashtra", "en", null, null, null);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DisplayName_TooLong_Should_Fail()
    {
        var cmd = new UpsertProfileCommand(Guid.NewGuid(), new string('A', 101), null, null, null, null, null, null);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void InvalidLocale_Should_Fail()
    {
        var cmd = new UpsertProfileCommand(Guid.NewGuid(), null, null, null, "fr", null, null, null);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PreferredLocale);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("hi")]
    public void ValidLocales_Should_Pass(string locale)
    {
        var cmd = new UpsertProfileCommand(Guid.NewGuid(), null, null, null, locale, null, null, null);
        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(x => x.PreferredLocale);
    }

    [Fact]
    public void NullLocale_Should_Pass()
    {
        // Locale is optional — null is fine
        var cmd = new UpsertProfileCommand(Guid.NewGuid(), null, null, null, null, null, null, null);
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

public class PaginationRequestTests
{
    [Fact]
    public void Page_Below_One_Clamped_To_One()
    {
        var req = new PaginationRequest(0, 20);
        req.Page.Should().Be(1);
    }

    [Fact]
    public void PageSize_Above_100_Clamped_To_100()
    {
        var req = new PaginationRequest(1, 500);
        req.PageSize.Should().Be(100);
    }

    [Fact]
    public void PageSize_Below_One_Clamped_To_One()
    {
        var req = new PaginationRequest(1, 0);
        req.PageSize.Should().Be(1);
    }

    [Fact]
    public void Offset_Calculated_Correctly()
    {
        var req = new PaginationRequest(3, 20);
        req.Offset.Should().Be(40);
    }
}

public class UserDomainTests
{
    [Fact]
    public void Create_User_Sets_Expected_Fields()
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        var user = User.Create(id, "Test@Example.com", "hash", "Test User", now);

        user.Id.Should().Be(id);
        user.Email.Should().Be("test@example.com");  // normalized
        user.IsEmailVerified.Should().BeFalse();
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void VerifyEmail_Sets_Flag_True()
    {
        var user = User.Create(Guid.NewGuid(), "a@b.com", "hash", null, DateTimeOffset.UtcNow);
        user.VerifyEmail(DateTimeOffset.UtcNow);
        user.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_Sets_IsActive_False()
    {
        var user = User.Create(Guid.NewGuid(), "a@b.com", "hash", null, DateTimeOffset.UtcNow);
        user.Deactivate(DateTimeOffset.UtcNow);
        user.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_With_Empty_Email_Throws(string email)
    {
        var act = () => User.Create(Guid.NewGuid(), email, "hash", null, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }
}

public class PagedResponseTests
{
    [Fact]
    public void HasNextPage_True_When_More_Items_Exist()
    {
        var response = PagedResponse<string>.Create(["a", "b"], page: 1, pageSize: 2, totalCount: 5);
        response.HasNextPage.Should().BeTrue();
        response.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_True_When_Page_Greater_Than_One()
    {
        var response = PagedResponse<string>.Create(["a", "b"], page: 2, pageSize: 2, totalCount: 5);
        response.HasPreviousPage.Should().BeTrue();
    }
}
