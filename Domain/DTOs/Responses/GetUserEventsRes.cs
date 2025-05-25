using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Responses
{
    public  record GetUserEventsRes(List<Event> Events, EventComparisonData EventComparisonData);

    public record EventComparisonData(List<ComparisonMetric> TotalAttendees, List<ComparisonMetric> TotalServed, List<ComparisonMetric> DropOffRate);

    public class ComparisonMetric {
        public long EventId { get; set; }

        public int Count { get; set; }

        public string EventName  { get; set; }
    }
}
