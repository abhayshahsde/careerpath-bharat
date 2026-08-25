using CareerPath.Application.Auth;
using CareerPath.Application.Abstractions;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Auth;
using CareerPath.Domain.Entities;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace CareerPath.Tests.Unit.Application;

public sealed class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    [InlineData("@no-domain")]
    public void Invalid_Email_Should_Fail(string email) =>
        _validator.TestValidate(new RegisterCommand(email, "ValidPass1", null, null))
                  .ShouldHaveValidationErrorFor(x => x.Email);

    [Theory]
    [InlineData("short")]         // < 8 chars
    [InlineData("alllowercase1")] // no uppercase
    [InlineData("NoDigitHere")]   // no digit
    public void Weak_Password_Should_Fail(string pw) =>
        _validator.TestValidate(new RegisterCommand("test@example.com", pw, null, null))
                  .ShouldHaveValidationErrorFor(x => x.Password);

    [Fact]
    public void Valid_Command_Should_Pass() =>
        _validator.TestValidate(new RegisterCommand("user@example.com", "SecurePass1", "Dev", null))
                  .ShouldNotHaveAnyValidationErrors();
}

public sealed class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Empty_Email_Should_Fail() =>
        _validator.TestValidate(new LoginCommand("", "password", null, null))
                  .ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Empty_Password_Should_Fail() =>
        _validator.TestValidate(new LoginCommand("a@b.com", "", null, null))
                  .ShouldHaveValidationErrorFor(x => x.Password);
}

public sealed class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _validator = new();

    [Fact]
    public void Same_Password_Should_Fail() =>
        _validator.TestValidate(
            new ChangePasswordCommand(Guid.NewGuid(), "SamePass1", "SamePass1"))
            .ShouldHaveValidationErrorFor(x => x.NewPassword);

    [Fact]
    public void Different_Valid_Passwords_Should_Pass() =>
        _validator.TestValidate(
            new ChangePasswordCommand(Guid.NewGuid(), "OldPass1", "NewPass2"))
            .ShouldNotHaveAnyValidationErrors();
}
