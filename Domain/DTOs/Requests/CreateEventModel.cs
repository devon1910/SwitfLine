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
        public required int Capacity { get; init; }
        public required int StaffCount { get; init; }

        public bool EnableGeographicRestriction { get; set; }
        public bool AllowAnonymousJoining { get; set; } = false;

        public bool AllowAutomaticSkips { get; set; } = true;

        public string? Address { get; set; }

        public decimal? Longitude { get; set; }

        public decimal? Latitude { get; set; }

        public int RadiusInMeters { get; set; }
    }
    public record EditEventReq 
    {
        public required long EventId { get; init; }
        public required string Title { get; init; }
        public required string Description { get; init; }
        public required int AverageTime { get; init; }
        public required string EventStartTime { get; init; }
        public required string EventEndTime { get; init; }
        public required int Capacity { get; init; }
        public required int StaffCount { get; init; }

        public bool AllowAnonymousJoining { get; set; } = false;

        public bool AllowAutomaticSkips { get; set; } = true;

        public bool EnableGeographicRestriction { get; set; }

        public string? Address { get; set; }

        public decimal? Longitude { get; set; }

        public decimal? Latitude { get; set; }

        public int RadiusInMeters { get; set; }
    }
}
