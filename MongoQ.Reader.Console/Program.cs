using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Tago.Extensions.MongoQ.Abstractions;
using Tago.Extensions.MongoQ.DependencyInjection;

namespace MongoQ
{
    class Program
    {        
        static void Main(string[] args)
        {
            //Tago.Extensions.MongoQ.Abstractions.


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
                    ctx.LogToConsole(opt =>
                    {
                        opt.Configure = (cfg) =>
                        {
                            cfg.WriteLevelString = false;
                            cfg.DisableColors = false;
                        };
                    });
                });
            
            sc.AddMongoJobs<JobProcessor>(opts =>
            {
                opts.ConnectionString = "mongodb://10.100.102.103:27017/JobsQ?authSource=admin&replicaSet=rs0";
                opts.Database = "JobsQ";
                opts.Collection = "TestQ";
                //opts.LocksCollection = "TestQ";
                opts.ConnectTimeout = TimeSpan.FromSeconds(3);
            });

            var sp = sc.BuildServiceProvider();            
            var  jober = sp.GetService<IJobProcessorService>();
           
            try
            {
                Console.WriteLine("press enter to start..");
                Console.ReadLine();
                Console.Clear();


                var options = new ProcessNextjobOptions
                {
                    //Squential = true,
                    Squential = JobSquential.JobDefined,
                    //JobTypes = new int[] { 1, 2 } 
                    Timeout = Timeout.InfiniteTimeSpan,// TimeSpan.FromMilliseconds(30000),
                    MaxConnectionReties = 5,
                    LongRunningRetry = TimeSpan.FromMilliseconds(200),
                    IgnoreLockedFor = TimeSpan.FromSeconds(3),
                };

                var task = jober.RunAsync(options);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }
    }
}
