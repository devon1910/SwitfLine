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
           return await dbContext.EmailDeliveryJobs
                .Where(x => !x.IsSent && x.RetryCount <= 3 )
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task LogEmail(string username, string email, string subject, string link, EmailTypeEnum type)
        {
            await dbContext.EmailDeliveryJobs.AddAsync(new EmailsDelivery
            {
                RecipientUsername = username,
                RecipientEmail = email,
                EmailType = type,
                Subject = subject,
                Link = link
               
            });

        }

        public async Task MarkEmailAsSent(long emailId)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE EmailsDelivery SET IsSent = true WHERE Id = {emailId}");
        }

        public async Task UpdateRetryCount(long emailId, string message) 
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"EmailsDelivery\" SET \"RetryCount\" = \"RetryCount\" + 1 WHERE \"Id\" = {emailId}");
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"EmailsDelivery\" SET \"ErrorMessage\" = {message} WHERE \"Id\" = {emailId}");
        }

        public async Task<Tuple<bool,string>> SendEmail(EmailsDelivery emailRecord)
        {
            string htmlTemplate = EmailTemplates.getEmailTemplate(emailRecord.EmailType);
            string swiftlineLink = _configuration[emailRecord.Link]; //come back to this
            var email = await _fluentEmail
                .To(emailRecord.RecipientEmail)
                .Subject(emailRecord.Subject)
                .Body(htmlTemplate
                .Replace("{{UserName}}", emailRecord.RecipientUsername)
                .Replace("{{SwiftlineUrl}}", emailRecord.Link), true)
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
