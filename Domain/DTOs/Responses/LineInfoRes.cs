using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Responses
{
    public record LineInfoRes(int Position, int TimeTillYourTurn, string PositionRank, string EventTitle, int averageWait, bool IsNotPaused =true, int StaffServing=1, int timeTillYourTurnAI=1, bool allowAutomaticSkips=true);
}
