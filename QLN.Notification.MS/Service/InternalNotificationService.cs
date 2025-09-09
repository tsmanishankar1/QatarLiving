using Microsoft.Extensions.Options;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Notification.MS.Dto;
using QLN.Notification.MS.IService.INotificationService;
using System.Net.Mail;
using System.Text;
using System.Xml.Linq;

namespace QLN.Notification.MS.Service
{
    public class InternalNotificationService : INotificationService
    {
        private readonly ILogger<InternalNotificationService> _logger;
        private readonly SmtpSettings _smtp;
        private readonly SmsSettings _sms;
        private readonly HttpClient _httpClient;
        private readonly QLNotificationContext _dbContext;
        private readonly string _emailTemplate;

        public InternalNotificationService(
            ILogger<InternalNotificationService> logger,
            IOptions<SmtpSettings> smtpOptions,
            IOptions<SmsSettings> smsOptions,
            IHttpClientFactory clientFactory,
            QLNotificationContext dbContext)
        {
            _logger = logger;
            _smtp = smtpOptions.Value;
            _sms = smsOptions.Value;
            _httpClient = clientFactory.CreateClient();
            _dbContext = dbContext;

            _emailTemplate = GetEmailTemplate();
        }

        private string GetEmailTemplate()
        {
            return @"<!DOCTYPE html>
            <html>
            <head>
            <style>
              body { font-family: Arial, sans-serif; margin: 0; padding: 0; }
              .email-wrapper { max-width: 600px; margin: 0 auto; border: 1px solid #d0d5dd; }
      
              /* Bigger header with more height */
              .email-header { background-color: #00426d; text-align: center; padding-top: 32px; padding-bottom: 32px; /* space below logo */}
      
              .email-body { padding: 24px; font-size: 14px; color: #242424; }
      
              /* Footer with extra gap below */
              .email-footer { 
                  text-align: center; 
                  font-size: 13px; 
                  padding: 20px; 
                  color: #242424; 
                  margin-bottom: 50px;
              }
            </style>
            </head>
            <body>
              <div class='email-wrapper'>
                <div class='email-header'>
                  <img src='https://qlsnext-prod-deexdvatdsatczcx.a03.azurefd.net/static-assets/email-logo-1.png' alt='Qatar Living Logo' width='140'/>
                </div>
                <div class='email-body'>
                  {MESSAGE_CONTENT}
                </div>
                <div class='email-footer'>
                  Copyright © 2005 - 2025 Qatar Living. All rights reserved.
                </div>
              </div>
            </body>
            </html>";
        }

        private string GenerateHtmlContent(string messageContent)
        {
            return _emailTemplate.Replace("{MESSAGE_CONTENT}", messageContent ?? string.Empty);
        }


        private async Task SaveNotificationToDb(NotificationDto request)
        {
            var entity = new NotificationEntity
            {
                Id = Guid.NewGuid(),
                Destinations = request.Destinations ?? new List<string>(),
                Sender = request.Sender,
                Recipients = request.Recipients ?? new List<RecipientDto>(),
                Subject = request.Subject ?? string.Empty,
                Plaintext = request.Plaintext ?? string.Empty,
                Html = request.Html,
                Attachments = request.Attachments
            };

            _dbContext.Notifications.Add(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SendMail(NotificationDto request)
        {
            await SaveNotificationToDb(request);

            foreach (var dest in request.Destinations)
            {
                switch (dest.ToLower())
                {
                    case "email":
                        await SendEmail(request);
                        break;
                    case "sms":
                        await SendSms(request);
                        break;
                    default:
                        _logger.LogWarning("Unknown destination type: {Destination}", dest);
                        break;
                }
            }
        }

        private async Task SendEmail(NotificationDto request)
        {
            var recipients = request.Recipients?.Where(r => !string.IsNullOrWhiteSpace(r.Email)).ToList();
            if (recipients == null || !recipients.Any())
            {
                _logger.LogWarning("Skipping email - No recipient email available.");
                return;
            }

            using var smtpClient = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                EnableSsl = _smtp.UseSsL,
                UseDefaultCredentials = _smtp.UseDefaultCredentials,
                Credentials = new System.Net.NetworkCredential(_smtp.Username, _smtp.Password)
            };

            var senderEmail = !string.IsNullOrWhiteSpace(request.Sender?.Email) ? request.Sender.Email.Trim() : _smtp.Email;
            var senderName = !string.IsNullOrWhiteSpace(request.Sender?.Name) ? request.Sender.Name.Trim() : _smtp.DisplayName;

            if (string.IsNullOrWhiteSpace(senderEmail) || !IsValidEmail(senderEmail))
            {
                _logger.LogError("Skipping email - Invalid or missing sender email: {SenderEmail}", senderEmail);
                return;
            }

            // Wrap HTML with template
            var htmlContent = GenerateHtmlContent(request.Html ?? request.Plaintext ?? string.Empty);

            foreach (var recipient in recipients)
            {
                if (!IsValidEmail(recipient.Email))
                {
                    _logger.LogWarning("Skipping invalid email address: {Email}", recipient.Email);
                    continue;
                }

                using var mail = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = request.Subject ?? "No Subject",
                    IsBodyHtml = true
                };
                mail.To.Add(recipient.Email);

                // Add plain text view
                if (!string.IsNullOrWhiteSpace(request.Plaintext))
                {
                    mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                        request.Plaintext, Encoding.UTF8, "text/plain"));
                }

                // Add HTML view
                mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                    htmlContent, Encoding.UTF8, "text/html"));

                // Attachments
                if (request.Attachments != null && request.Attachments.Any())
                {
                    await AddAttachmentsAsync(mail, request.Attachments);
                }

                try
                {
                    await smtpClient.SendMailAsync(mail);
                    _logger.LogInformation("Email sent successfully to {Email} from {SenderEmail}", recipient.Email, senderEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Email} from {SenderEmail}", recipient.Email, senderEmail);
                }
            }
        }

        private async Task AddAttachmentsAsync(MailMessage mail, IEnumerable<AttachmentDto> attachments)
        {
            foreach (var attachment in attachments)
            {
                try
                {
                    byte[] fileBytes;
                    if (Uri.IsWellFormedUriString(attachment.Content, UriKind.Absolute))
                        fileBytes = await _httpClient.GetByteArrayAsync(attachment.Content);
                    else
                        fileBytes = Convert.FromBase64String(attachment.Content);

                    var stream = new MemoryStream(fileBytes);
                    var mailAttachment = new Attachment(stream, attachment.Filename, attachment.ContentType);
                    mail.Attachments.Add(mailAttachment);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to attach file {Filename}", attachment.Filename);
                }
            }
        }
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        private async Task SendSms(NotificationDto request)
        {
            var recipients = request.Recipients
                .Where(r => !string.IsNullOrWhiteSpace(r.Phone))
                .ToList();

            if (!recipients.Any())
            {
                _logger.LogWarning("Skipping SMS - No recipient phone available.");
                return;
            }

            foreach (var recipient in recipients)
            {
                var phone = recipient.Phone.Replace("+", "").Replace(" ", "").Trim();

                var xml = new XElement("MESSAGE",
                    new XElement("CUSTOMER", _sms.CustomerId),
                    new XElement("USER", _sms.UserName),
                    new XElement("PASSWORD", _sms.UserPassword),
                    new XElement("ORG", _sms.Originator),
                    new XElement("TEXT", new XText(request.Plaintext ?? "")),
                    new XElement("MOBILE", phone)
                );

                var xmlString = xml.ToString(SaveOptions.DisableFormatting); 
                _logger.LogInformation("Sending SMS XML to {Phone}: {Xml}", phone, xmlString);

                var content = new StringContent(xmlString, Encoding.UTF8, "text/xml");

                try
                {
                    var response = await _httpClient.PostAsync(_sms.ApiUrl, content);
                    var respText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("SMS sent to {Phone}. Response: {Response}", phone, respText);
                    }
                    else
                    {
                        _logger.LogError("SMS failed to {Phone}. Status: {StatusCode}, Response: {Response}", phone, response.StatusCode, respText);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending SMS to {Phone}", phone);
                }
            }
        }
    }
}
