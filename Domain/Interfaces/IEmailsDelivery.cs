using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IEmailsDeliveryRepo
    {
        public Task LogEmail(string username, string email, string subject, string link, EmailTypeEnum type);
        public Task<List<EmailsDelivery>> GetAllUnsentEmails();
        public Task<Tuple<bool, string>> SendEmail(EmailsDelivery email);
        public Task MarkEmailAsSent(long emailId);

        public Task UpdateRetryCount(long emailId, string message);

    }
    //public interface IEmailsDeliveryService
    //{
    //    public Task LogEmail(string username, string email, string type);
    //}

   
}
