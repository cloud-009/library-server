using System.Net;
using System.Net.Mail;

namespace library_server
{
    public class EmailService
    {
        public EmailService(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }

        public void SendEmail(string toEmail, string subject, string body)
        {
            var fromEmail = Configuration.GetSection("Constants:FromEmail").Value ?? string.Empty;
            var fromEmailPassword = Configuration.GetSection("Constants:FromEmailPassword").Value ?? string.Empty;

            var message = new MailMessage()
            {
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromEmail, fromEmailPassword),
                EnableSsl = true,
            };
            smtpClient.Send(message);
        }
    }
}
