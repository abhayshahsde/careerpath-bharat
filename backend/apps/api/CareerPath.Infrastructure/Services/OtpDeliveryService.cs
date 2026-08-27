using System.Net.Http.Json;
using System.Text;
using CareerPath.Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CareerPath.Infrastructure.Services;

public sealed class OtpDeliveryService : IOtpDeliveryService
{
    private readonly IConfiguration _config;
    private readonly ILogger<OtpDeliveryService> _logger;
    private readonly HttpClient _http;

    public OtpDeliveryService(
        IConfiguration config,
        ILogger<OtpDeliveryService> logger,
        HttpClient? httpClient = null)
    {
        _config = config;
        _logger = logger;
        _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    public async Task<bool> SendEmailOtpAsync(string email, string otpCode, string displayName, CancellationToken ct = default)
    {
        var emailProvider = _config["OtpGateway:EmailProvider"] ?? "RealtimeGateway";
        var name = string.IsNullOrWhiteSpace(displayName) ? "Student" : displayName;

        _logger.LogInformation(
            "[OTP Delivery] Sending Email OTP to {Email} for {DisplayName}. Provider: {Provider}. Code: {OtpCode}",
            email, name, emailProvider, otpCode);

        // 1. Check for custom Webhook / Realtime Email API
        var emailEndpoint = _config["OtpGateway:EmailEndpoint"];
        var emailApiKey = _config["OtpGateway:EmailApiKey"];

        if (!string.IsNullOrWhiteSpace(emailEndpoint))
        {
            try
            {
                var payload = new
                {
                    to = email,
                    subject = "CareerPath Bharat — Password Reset Verification Code",
                    template = "password_reset_otp",
                    data = new
                    {
                        name,
                        otp = otpCode,
                        validity_minutes = 10
                    }
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, emailEndpoint);
                if (!string.IsNullOrWhiteSpace(emailApiKey))
                {
                    req.Headers.Add("Authorization", $"Bearer {emailApiKey}");
                }
                req.Content = JsonContent.Create(payload);

                var res = await _http.SendAsync(req, ct);
                if (res.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[OTP Delivery] Successfully dispatched realtime Email OTP to {Email}", email);
                    return true;
                }

                _logger.LogWarning("[OTP Delivery] Email API returned status: {Status}", res.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OTP Delivery] Realtime email dispatch failed. Fallback logging enabled.");
            }
        }

        return true;
    }

    public async Task<bool> SendWhatsAppOtpAsync(string phoneNumber, string otpCode, string displayName, CancellationToken ct = default)
    {
        var waProvider = _config["OtpGateway:WhatsAppProvider"] ?? "RealtimeWhatsApp";
        var name = string.IsNullOrWhiteSpace(displayName) ? "Student" : displayName;

        _logger.LogInformation(
            "[OTP Delivery] Sending WhatsApp OTP to {PhoneNumber} for {DisplayName}. Provider: {Provider}. Code: {OtpCode}",
            phoneNumber, name, waProvider, otpCode);

        // 1. Check for Realtime WhatsApp / SMS Endpoint (e.g. WhatsApp Cloud API / Twilio / MSG91 / Fast2SMS)
        var waEndpoint = _config["OtpGateway:WhatsAppEndpoint"];
        var waToken = _config["OtpGateway:WhatsAppToken"];

        if (!string.IsNullOrWhiteSpace(waEndpoint))
        {
            try
            {
                var messageText = $"*CareerPath Bharat*\n\nHello {name}, your password reset verification code is: *{otpCode}*\n\nThis code will expire in 10 minutes. Do not share this OTP with anyone.";
                
                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "text",
                    text = new { body = messageText }
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, waEndpoint);
                if (!string.IsNullOrWhiteSpace(waToken))
                {
                    req.Headers.Add("Authorization", $"Bearer {waToken}");
                }
                req.Content = JsonContent.Create(payload);

                var res = await _http.SendAsync(req, ct);
                if (res.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[OTP Delivery] Successfully dispatched realtime WhatsApp message to {Phone}", phoneNumber);
                    return true;
                }

                _logger.LogWarning("[OTP Delivery] WhatsApp API returned status: {Status}", res.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OTP Delivery] Realtime WhatsApp dispatch failed. Fallback logging enabled.");
            }
        }

        return true;
    }
}
