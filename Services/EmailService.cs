using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace rps.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string templatePath, Dictionary<string, string> placeholders, bool isHtml = true);
    }
    public class EmailService : IEmailService
    {
        
         private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
           _configuration = configuration;
        }
        public async Task SendEmailAsync(string to, string subject, string templatePath, Dictionary<string, string> placeholders, bool isHtml = true)
        {
            string emailBody = await LoadEmailTemplate(templatePath, placeholders);

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("EUI RESULT PROCESSING SYSTEM", _configuration["EmailSettings:SenderEmail"]));
            emailMessage.To.Add(new MailboxAddress("", to));
            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = emailBody
                // TextBody = isHtml ? null : body
            };

            emailMessage.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_configuration["EmailSettings:SmtpServer"], 
                                          int.Parse(_configuration["EmailSettings:Port"]), 
                                          true);
                await client.AuthenticateAsync(_configuration["EmailSettings:SenderEmail"], 
                                               _configuration["EmailSettings:Password"]);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
        private async Task<string> LoadEmailTemplate(string templatePath, Dictionary<string, string> placeholders)
        {
            string template = await File.ReadAllTextAsync(templatePath);

            foreach (var placeholder in placeholders)
            {
                template = template.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
            }

            return template;
        }
    }
}