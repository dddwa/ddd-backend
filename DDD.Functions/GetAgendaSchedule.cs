using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using DDD.Functions.Extensions;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Azure.Storage.Blobs;

namespace DDD.Functions
{
    public static class GetAgendaSchedule
    {
        [FunctionName("GetAgendaSchedule")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
            HttpRequest req,
            ILogger log,
            [BindConferenceConfig]
            ConferenceConfig conference,
            [BindKeyDatesConfig]
            KeyDatesConfig keyDates,
            [BindAgendaScheduleConfig]
            AgendaScheduleConfig agendaScheduleConfig)
        {
            if (keyDates.Before(x => x.SubmissionsAvailableToDate))
            {
                log.LogWarning("Attempt to access GetAgendaSchedule endpoint before they are available at {availableDate}.", keyDates.SubmissionsAvailableToDate);
                return new StatusCodeResult(404);
            }

            var agendaScheduleContent = null;
            var blobServiceClient = new BlobServiceClient(agendaScheduleConfig.ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(agendaScheduleConfig.Container);
            var blobClient = containerClient.GetBlobClient(conference.ConferenceInstance);
            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadAsync();
                using (var streamReader= new StreamReader(response.Value.Content))
                {
                    while (!streamReader.EndOfStream)
                    {
                        agendaScheduleContent = await streamReader.ReadLineAsync();
                    }
                }
            }

            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new DefaultContractResolver();

            return new JsonResult(agendaScheduleContent, settings);
        }

        public class AgendaSchedule
        {
            public string Year { get; set; }
            public string Content { get; set; }
        }
    }
}
