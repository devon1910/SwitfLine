using Domain.DTOs.Requests;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class FeebackRepo(SwiftLineDatabaseContext databaseContext) : IFeedbackRepo
    {
        public bool SubmitFeedback(SubmitFeedbackModel model)
        {
            Feedback fb = new Feedback()
            {
                Comment = model.Comment,
                Rating = model.Rating,
                Tags = model.Tags,
                UserId = model.UserId,
            };

            databaseContext.SaveChanges();
            return true;
        }
    }
}
