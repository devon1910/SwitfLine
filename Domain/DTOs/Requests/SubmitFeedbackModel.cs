using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.DTOs.Requests
{
    public class SubmitFeedbackModel
    {
        [JsonIgnore]
        public string? UserId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public List<string> Tags { get; set; }
    }
}
