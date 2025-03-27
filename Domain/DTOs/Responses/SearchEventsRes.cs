using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Responses
{
    public record SearchEventsRes(List<Event> Events, int TotalPages, bool IsUserInQueue);
}
