using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ExitQueueRepo(SwiftLineDatabaseContext dbContext, ILineRepo lineRepo, INotifierRepo notifierRepo) : IExitQueue
    {
       

    }
}
