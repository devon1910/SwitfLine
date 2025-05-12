using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace RepositoryTests.Lines
{
    
    public class LinesTest(LineService _lineService)
    {
        private readonly Mock<ICCCurvesRepository> _cccRepositoryMock;
        private readonly Mock<AmazonCloudWatchLogger> _loggerMock;
        private readonly CCCurvesService _service;
        private readonly ClaimsPrincipal _claimsPrincipal;
        public LinesTest()
        {
            // Initialize any dependencies or mocks here
        }

        [Fact]
        public void TestLineCreation()
        {
            // Arrange
            var lineService = new LineService();
            var lineName = "Test Line";
            // Act
            var result = lineService.CreateLine(lineName);
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(lineName, result.Value.Name);
        }
    }
}
