using System;
using System.Linq;
using DDD.Functions.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(Startup))]

namespace DDD.Functions.Extensions
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            typeof(ConferenceConfig).Assembly.GetTypes()
                .Where(t => typeof(IExtensionConfigProvider).IsAssignableFrom(t))
                .ToList()
                .ForEach(t => builder.AddExtension(t));
        }
    }
}