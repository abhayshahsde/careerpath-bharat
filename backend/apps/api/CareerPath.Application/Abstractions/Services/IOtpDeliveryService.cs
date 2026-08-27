namespace CareerPath.Application.Abstractions.Services;

public interface IOtpDeliveryService
{
    Task<bool> SendEmailOtpAsync(string email, string otpCode, string displayName, CancellationToken ct = default);
    Task<bool> SendWhatsAppOtpAsync(string phoneNumber, string otpCode, string displayName, CancellationToken ct = default);
}
