using Domain.Interfaces;
using Domain.Models;
using FluentEmail.Core;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Infrastructure.Repositories
{
    public class EmailsDeliveryRepo(SwiftLineDatabaseContext dbContext, IConfiguration _configuration,
        IFluentEmail _fluentEmail, ILogger<EmailsDeliveryRepo> _logger) : IEmailsDeliveryRepo
    {
        public async Task<List<EmailsDelivery>> GetAllUnsentEmails()
        {
           return await dbContext.EmailDeliveryRequests
                .Where(x => !x.IsSent && x.RetryCount < 3 )
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task LogEmail(string username, string email, string subject, string link, EmailTypeEnum type, string estimatedWait)
        {
            await dbContext.EmailDeliveryRequests.AddAsync(new EmailsDelivery
            {
                RecipientUsername = username,
                RecipientEmail = email,
                EmailType = type,
                Subject = subject,
                Link = link,
                EstimatedWait= estimatedWait 
            });

        }

        public async Task MarkEmailAsSent(long emailId)
        {
            try
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"EmailDeliveryRequests\" SET \"IsSent\" = true WHERE \"Id\" = {emailId}");
                await dbContext.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                throw;
            }
           
        }

        public async Task UpdateRetryCount(long emailId, string message) 
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"EmailDeliveryRequests\" SET \"RetryCount\" = \"RetryCount\" + 1 WHERE \"Id\" = {emailId}");
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"EmailDeliveryRequests\" SET \"Message\" = {message} WHERE \"Id\" = {emailId}");
            await dbContext.SaveChangesAsync();
        }

        public async Task<Tuple<bool,string>> SendEmail(EmailsDelivery emailRecord)
        {
            string htmlTemplate = EmailTemplates.getEmailTemplate(emailRecord.EmailType);

           //string logoPath = Path.Combine(AppContext.BaseDirectory, "assets\\theSwiftlineLogo.png");

            string expirationTime = emailRecord.DateCreated.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss");

            string swiftlineLink = _configuration[emailRecord.Link]; //come back to this
            var email = await _fluentEmail
                .To(emailRecord.RecipientEmail)
                .Subject(emailRecord.Subject)
                .Body(htmlTemplate
                .Replace("{{Logo}}", "https://theswiftline.com/theSwiftlineLogo.png")
                .Replace("{{UserName}}", emailRecord.RecipientUsername)
                .Replace("{{theswiftlineUrl}}", emailRecord.Link)
                .Replace("{{EstimatedWait}}", emailRecord.EstimatedWait)
                .Replace("{{ExpirationTime}}", expirationTime),
                true)
                .SendAsync();
            _logger.LogInformation("Email Sent Successfully");
            if (!email.Successful)
            {
                _logger.LogError("Failed to send email: {Errors}",
                    string.Join(", ", email.ErrorMessages));

                return Tuple.Create(false, email.ErrorMessages.ToString());
            }
            return Tuple.Create(true, email.ToString()); 
        }

       
      
    }
}
