using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Requests
{
  
    public class QueueData
    {
        public float PositionInQueue { get; set; }
        public float AverageServiceTime { get; set; }
        public float PeopleInQueue { get; set; }
        public float PeopleServed { get; set; }
        public float TimeInQueue { get; set; }
    }

    public class WaitTimePrediction
    {
        public float Score { get; set; } 
    }
}
