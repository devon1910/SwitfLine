using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Responses
{
    public record LineInfoRes(long LineMemberId, int Position, int TimeTillYourTurn, string PositionRank, string EventTitle);
}
