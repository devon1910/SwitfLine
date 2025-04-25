using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
