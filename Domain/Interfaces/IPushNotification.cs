using Domain.DTOs.Requests;
using Domain.DTOs.Responses;


namespace Domain.Interfaces
{
    public interface IPushNotificationRepo
    {
        public Task<bool> Save(string UserId, string subscription);
    }
    public interface IPushNotificationService
    {
        public Task<Result<bool>> Save(string UserId, string subscription);
        public Task SendPushNotification(SubscriptionModel subscription, string payload);
    }

    
}
