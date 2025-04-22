using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.RetryLogic
{
    public  static class RetryPolicy
    {
        public static async Task<T> ExecuteAsync<T>(Func<Task<T>> action, int maxRetryCount, TimeSpan delayBetweenRetries)
        {
            int retryAttempt = 0;
            while (true)
            {
                try
                {
                    return await action();
                }
                catch
                {
                    retryAttempt++;
                    if (retryAttempt >= maxRetryCount)
                    {
                        throw;
                    }
                    await Task.Delay(delayBetweenRetries);
                }
            }
        }
    }
}
