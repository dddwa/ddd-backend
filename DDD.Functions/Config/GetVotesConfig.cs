using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Config
{
    public class GetVotesConfig : Attribute
    {
        // Conference details
        [AppSetting(Default = "ConferenceInstance")]
        public string ConferenceInstance { get; set; }

        // Sessions store
        [AppSetting(Default = "SessionsConnectionString")]
        public string SessionsConnectionString { get; set; }
        [AppSetting(Default = "SessionsDataSourceCosmosDatabaseId")]
        public string CosmosDatabaseId { get; set; }
        [AppSetting(Default = "SessionsDataSourceCosmosCollectionId")]
        public string CosmosCollectionId { get; set; }

        // Voting store
        [AppSetting(Default = "VotesConnectionString")]
        public string VotingConnectionString { get; set; }
        [AppSetting(Default = "VotingTable")]
        public string VotingTable { get; set; }

        // Eventbrite ids
        [AppSetting(Default = "EventbriteTable")]
        public string EventbriteTable { get; set; }

        // App Insights sessions

        [AppSetting(Default = "AppInsightsTable")]
        public string AppInsightsTable { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindGetVotesConfigAttribute : GetVotesConfig { }

    public class BindGetVotesConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindGetVotesConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private GetVotesConfig BuildItemFromAttr(BindGetVotesConfigAttribute attr)
        {
            return attr;
        }
    }
}
