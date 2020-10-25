using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Tago.Extensions.MongoQ.Abstractions;

namespace MongoQ.Writer
{
    class Program
    {
        static bool modeWriter = false;

        static int counter = 1;

        static async Task Main(string[] args)
        {
            var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var builder = new ConfigurationBuilder()
                .SetBasePath(dir)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();


            IConfigurationRoot configuration = builder.Build();

            var sc = new ServiceCollection()
                .AddLogging(ctx =>
                {
                    ctx.AddConfiguration(configuration.GetSection("Logging"));
                    //ctx.AddConsole(cfg =>
                    //{
                    //    //cfg.IncludeScopes = false;
                    //    //cfg.DisableColors = true;
                    //});
                    ctx.LogToConsole(opt => {
                        opt.Configure = (cfg) =>
                        {
                            cfg.WriteLevelString = false;
                            cfg.DisableColors = false;
                        };
                    });
                });

            sc.AddMongoJobs(opts =>
            {
                opts.ConnectionString = "mongodb://10.100.102.103:27017/JobsQ?authSource=admin&replicaSet=rs0";
                opts.Database = "JobsQ";
                opts.Collection = "TestQ";
                //opts.LocksCollection = "TestQ";
            });

            var sp = sc.BuildServiceProvider();

            IJobCreator jobsCreator = sp.GetService<IJobCreator>();
            
            try
            {
                while (true)
                {                    
                    Console.Clear();
                    Console.WriteLine("press enter to insert items");
                    Console.WriteLine("press 1 for sequential job or any other key for parallel");
                    var  key = Console.ReadKey();
                    if (key.Key != ConsoleKey.Escape)
                    {
                        var seq = key.KeyChar == '1';

                        Console.WriteLine("enter number of jobs to insert:");
                        var numStr = Console.ReadLine();
                        int num = 1;
                        if( Int32.TryParse(numStr, out var res))
                        {
                            num = res;
                        }
                        await InsertJobs(jobsCreator, seq, num);
                    }
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }
               

        private static async Task InsertJobs(IJobCreator jobsDb, bool squential, int num)
        {
            for (int i = 0; i < num; i++)
            {
                int cntr = (counter++);

                var job = JobsGroup.Create(1, $"job{cntr}");
                job.EntityIdentifier = $"job{cntr}";
                job.RetryInterval = TimeSpan.FromSeconds(10);
                job.MaxRetries = 3;
                job.Sequential = squential;

                for (int x = 0; x < 3; x++)
                {
                    var cj = job.AddJob(1, $"Childjob{cntr}_1");
                    cj.JobData = new { Name = "Golan" };                    
                }

                await jobsDb.AddAsync(job);
            }
        }
    }
}
