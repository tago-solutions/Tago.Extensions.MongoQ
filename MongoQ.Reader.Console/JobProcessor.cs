using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Tago.Extensions.MongoQ;
using Tago.Extensions.MongoQ.Abstractions;
using Tago.Extensions.MongoQ.DependencyInjection;

namespace MongoQ
{

    internal class JobProcessor : IJobProcessor
    {
        static Random randomResult = new Random(Environment.TickCount);

        private readonly ILogger<JobProcessor> logger;

        public JobProcessor(ILogger<JobProcessor> logger)
        {
            this.logger = logger;
        }
        public async Task<JobResult> ProcessJob(IJobContext jobCtx)
        {
            var res = JobResult.Failure("never run");
            try
            {
                var job = jobCtx.Job;

                int delay = 0;
                
                if (delay > 0)
                {
                    this.logger.LogTrace($"suspending job for {delay} ms");
                    await Task.Delay(delay);
                }
                this.logger.LogDebug($"Processing {jobCtx}");

                int resultType = randomResult.Next(0, 2);

                resultType = 0;

                switch (resultType)
                {
                    case 0:
                        var opts = new JobFailureOptions { JobPriority = 9, GroupPriority = 8, NextGroupRetry = DateTime.Now.AddDays(1) };
                        res = JobResult.Failure("failed by tester");
                        break;
                    case 1:
                        var opts1 = new JobSuccessOptions { CleanUpTypes = JobCleanUpTypes.DeleteJobAndGroup };
                        res = JobResult.Success("completed by tester");                        
                        break;
                    case 2:
                        res = JobResult.Cancel("cancled by tester");
                        break;
                    case 3:
                        throw new ApplicationException("exception thrown by tester");
                }
            }
            catch (Exception ex)
            {
                res = JobResult.Failure(ex, "failed by tester");
            }

            return res;
        }
    }
}
