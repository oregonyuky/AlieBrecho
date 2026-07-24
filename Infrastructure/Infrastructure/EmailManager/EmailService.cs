using Application.Common.Services.EmailManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.EmailManager;


public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly SmtpSettings _smtpSettings;

    public EmailService(ILogger<EmailService> logger, IOptions<SmtpSettings> smtpSettings)
    {
        _logger = logger;
        _smtpSettings = smtpSettings.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            var host = GetRequiredSetting(_smtpSettings.Host, nameof(SmtpSettings.Host));
            var userName = GetRequiredSetting(_smtpSettings.UserName, nameof(SmtpSettings.UserName));
            var password = GetRequiredSetting(_smtpSettings.Password, nameof(SmtpSettings.Password));
            var fromAddress = string.IsNullOrWhiteSpace(_smtpSettings.FromAddress)
                ? userName
                : _smtpSettings.FromAddress;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpSettings.FromName ?? "noreply", fromAddress));
            message.To.Add(new MailboxAddress(email, email));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlMessage
            };

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                await client.ConnectAsync(host, _smtpSettings.Port, true);
                await client.AuthenticateAsync(userName, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email.");
        }
    }

    private static string GetRequiredSetting(string? value, string settingName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"SMTP setting '{settingName}' is required.")
            : value;
    }
}
