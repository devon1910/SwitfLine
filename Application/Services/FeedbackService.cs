using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class FeedbackService(IFeedbackRepo FbRepo) : IFeedbackService
    {
        public Result<bool> SubmitFeedback(SubmitFeedbackModel model)
        {
            bool isFeedbackSumbitted= FbRepo.SubmitFeedback(model);
            if (isFeedbackSumbitted) {
                return Result<bool>.Created(true);
            }
            return Result<bool>.Failed("Unable to submit Feedback");
        }
    }
}
