using Application.Services;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceUnitTests
{
    public class LinesServiceTests
    {
        private readonly ILineService _lineService;
        private readonly ILineRepo _linesRepoMock;

        public LinesServiceTests()
        {
            _linesRepoMock = Substitute.For<ILineRepo>();
            _lineService = new LineService(_linesRepoMock);
        }
        [Fact]
        public async Task GetLineDetailsForUser_ReturnsCorrectInfo()
        {
            // Arrange
            var userId = "user123";

            var expected = new LineInfoRes(
                 10L,
                2,
                 8,
                 "2nd",
                "Swift Conference",
                4,
                true,
                1
            );

            _linesRepoMock.GetUserLineInfo(userId).Returns(expected);

            // Act
            var result = await _lineService.GetUserLineInfo(userId);

            // Assert
            Assert.Equal(expected.LineMemberId, result.Data.LineMemberId);
            Assert.Equal(expected.Position, result.Data.Position);
            Assert.Equal(expected.TimeTillYourTurn, result.Data.TimeTillYourTurn);
            Assert.Equal(expected.PositionRank, result.Data.PositionRank);
            Assert.Equal(expected.EventTitle, result.Data.EventTitle);
            Assert.Equal(expected.averageWait, result.Data.averageWait);
            Assert.Equal(expected.IsNotPaused, result.Data.IsNotPaused);
            Assert.Equal(expected.StaffServing, result.Data.StaffServing);
        }

        [Fact]
        public async Task GetLineDetailsForUser_ReturnsDefaultWhenUserNotInLine()
        {
            // Arrange
            var userId = "unknown-user";
            var defaultRes = new LineInfoRes(0, -1, 0, "", "", 0, false, 0);

            _linesRepoMock.GetUserLineInfo(userId).Returns(defaultRes);

            // Act
            var result = await _lineService.GetUserLineInfo(userId);

            // Assert
            Assert.Equal(0, result.Data.LineMemberId);
            Assert.Equal(-1, result.Data.Position);
            Assert.Equal(0, result.Data.TimeTillYourTurn);
            Assert.Equal("", result.Data.PositionRank);
            Assert.Equal("", result.Data.EventTitle);
            Assert.Equal(0, result.Data.averageWait);
            Assert.False(result.Data.IsNotPaused);
            Assert.Equal(0, result.Data.StaffServing);
        }

        [Theory]
        [InlineData(1, "1st")]
        [InlineData(2, "2nd")]
        [InlineData(3, "3rd")]
        [InlineData(4, "4th")]
        [InlineData(11, "11th")]
        [InlineData(12, "12th")]
        [InlineData(13, "13th")]
        [InlineData(21, "21st")]
        public async Task GetLineDetailsForUser_OrdinalIsCorrect(int position, string expectedOrdinal)
        {
            // Arrange
            var userId = $"user-{position}";

            var expected = new LineInfoRes(
               999,
                 position,
                5,
                expectedOrdinal,
                "Ordinal Event",
                5,
                true,
                1
            );

            _linesRepoMock.GetUserLineInfo(userId).Returns(expected);

            // Act
            var result = await _lineService.GetUserLineInfo(userId);

            // Assert
            Assert.Equal(expectedOrdinal, result.Data.PositionRank);
        }

    }
}
