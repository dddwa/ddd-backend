
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DDD.Core.DocumentDb;
using DDD.Functions.Config;
using DDD.Sessionize;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DDD.Functions
{
    public static class NewSessionNotification
    {
        [FunctionName("NewSessionNotification")]
        public static async Task Run([CosmosDBTrigger(
            "%SessionsDataSourceCosmosDatabaseId%",
            "%SessionsDataSourceCosmosCollectionId%",
            ConnectionStringSetting = "ConnectionStrings:Sessions",
            CreateLeaseCollectionIfNotExists = true)]
            IReadOnlyList<Document> input,
            ILogger log,
            [BindNewSessionNotificationConfig]
            NewSessionNotificationConfig config)
        {
            var documentDbClient = DocumentDbAccount.Parse(config.ConnectionString);
            var repo = new DocumentDbRepository<SessionOrPresenter>(documentDbClient, config.CosmosDatabaseId, config.CosmosCollectionId);
            await repo.InitializeAsync();

            using (var client = new HttpClient())
            {
                foreach (var document in input)
                {
                    // Only want new sessions
                    var sessionOrPresentation = await repo.GetItemAsync(document.Id);
                    if (sessionOrPresentation.Session == null || sessionOrPresentation.Session.ModifiedDate != null)
                        continue;

                    // Get denormalised data
                    var session = sessionOrPresentation.Session;
                    var presenterIds = session.PresenterIds.Select(x => x.ToString()).ToArray();
                    var presenters = await repo.GetItemsAsync(sop => presenterIds.Contains(sop.Id));
                    var postContent = JsonConvert.SerializeObject(new
                    {
                        Session = session,
                        Presenters = presenters.Select(x => x.Presenter).ToArray()
                    }, Formatting.None, new StringEnumConverter());

                    // Post the data
                    log.LogInformation("Posting {documentId} to {logicAppUrl}", document.Id, config.LogicAppUrl);
                    var response = await client.PostAsync(config.LogicAppUrl, new StringContent(postContent, Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        log.LogError("Unsuccessful request to post {documentId}; received {statusCode} and {responseBody}", document.Id, response.StatusCode, await response.Content.ReadAsStringAsync());
                        response.EnsureSuccessStatusCode();
                    }
                }
            }
        }
    }
}
