using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Requests
{
    public record CreateEventModel
    {
        public required string Title { get; init; }
        public required string Description { get; init; }
        public required int AverageTime { get; init; }
        public required string EventStartTime { get; init; }
        public required string EventEndTime { get; init; }
    }
    public record EditEventReq 
    {
        public required long EventId { get; init; }
        public required string Title { get; init; }
        public required string Description { get; init; }
        public required int AverageTime { get; init; }
        public required string EventStartTime { get; init; }
        public required string EventEndTime { get; init; }
    }
}
