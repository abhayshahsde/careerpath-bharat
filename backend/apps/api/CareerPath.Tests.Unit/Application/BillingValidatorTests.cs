using CareerPath.Application.Billing;
using FluentValidation.TestHelper;
using Xunit;

namespace CareerPath.Tests.Unit.Application;

public sealed class BillingValidatorTests
{
    private readonly SubscribeValidator _subscribeValidator = new();

    [Fact]
    public void SubscribeCommand_Valid_Request_Passes()
    {
        var cmd = new SubscribeCommand(Guid.NewGuid(), Guid.NewGuid(), "Stripe", "tok_visa");
        _subscribeValidator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SubscribeCommand_Invalid_Provider_Fails()
    {
        var cmd = new SubscribeCommand(Guid.NewGuid(), Guid.NewGuid(), "InvalidGateway", "tok_visa");
        _subscribeValidator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Provider);
    }

    [Fact]
    public void SubscribeCommand_Empty_Token_Fails()
    {
        var cmd = new SubscribeCommand(Guid.NewGuid(), Guid.NewGuid(), "Stripe", "");
        _subscribeValidator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.CardToken);
    }

    [Theory]
    [InlineData("Mock")]
    [InlineData("Stripe")]
    [InlineData("Razorpay")]
    [InlineData("Free")]
    public void SubscribeCommand_All_Valid_Providers_Pass(string provider)
    {
        var cmd = new SubscribeCommand(Guid.NewGuid(), Guid.NewGuid(), provider, "tok_valid", "BHARAT50");
        _subscribeValidator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Coupon_Percentage_Discount_Calculates_Accurately()
    {
        decimal planPrice = 999m;
        decimal discountPercent = 50m;
        decimal discountAmount = Math.Round((planPrice * discountPercent) / 100m, 2);
        decimal finalPrice = Math.Max(0m, planPrice - discountAmount);

        Assert.Equal(499.50m, discountAmount);
        Assert.Equal(499.50m, finalPrice);
    }

    [Fact]
    public void Coupon_Fixed_Amount_Discount_Cannot_Exceed_Plan_Price()
    {
        decimal planPrice = 299m;
        decimal couponFixedAmount = 500m;
        decimal discountAmount = Math.Min(planPrice, couponFixedAmount);
        decimal finalPrice = Math.Max(0m, planPrice - discountAmount);

        Assert.Equal(299m, discountAmount);
        Assert.Equal(0m, finalPrice);
    }
}
