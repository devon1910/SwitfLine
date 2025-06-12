using Domain.DTOs.Requests;
using Domain.DTOs.Responses;

namespace Domain.Interfaces
{
    public interface IFeedbackRepo
    {
        public bool SubmitFeedback(SubmitFeedbackModel model);
    }

    public interface IFeedbackService 
    {
        public Result<bool> SubmitFeedback(SubmitFeedbackModel model);
    }
}
