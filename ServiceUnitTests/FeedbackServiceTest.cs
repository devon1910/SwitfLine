using Application.Services;
using Domain.DTOs.Requests;
using Domain.Interfaces;
using Domain.Models;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ServiceUnitTests
{
    public class FeedbackServiceTests
    {
        private readonly IFeedbackService _feedbackService;
        private readonly IFeedbackRepo _feedbackRepoMock;
        public FeedbackServiceTests()
        {
            _feedbackRepoMock = Substitute.For<IFeedbackRepo>();
            _feedbackService = new FeedbackService(_feedbackRepoMock);
        }

        [Fact]
        public void SubmitFeedback_ShouldReturnCreatedResult_WhenFeedbackIsSubmitted()
        {
            // Arrange
            var feedbackModel = new SubmitFeedbackModel
            {
                UserId = "user123",
                Rating = 5,
                Comment = "Great service!",
                Tags = new List<string> { "service", "feedback" }
            };

            _feedbackRepoMock.SubmitFeedback(feedbackModel).Returns(true);
            // Act
            var result = _feedbackService.SubmitFeedback(feedbackModel);
            // Assert
            Assert.True(result.Status);

            Assert.Equal("Operation completed successfully", result.Message);
        }

    }

}
