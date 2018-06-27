using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Config
{
    public class SessionizeReadModelSyncConfig : Attribute
    {
        public DateTimeOffset Now => DateTimeOffset.UtcNow;

        [AppSetting(Default = "SessionsConnectionString")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "SessionsDataSourceCosmosDatabaseId")]
        public string CosmosDatabaseId { get; set; }
        [AppSetting(Default = "SessionsDataSourceCosmosCollectionId")]
        public string CosmosCollectionId { get; set; }
        [AppSetting(Default = "SessionizeApiKey")]
        public string SessionizeApiKey { get; set; }
        [AppSetting(Default = "StopSyncingSessionsFrom")]
        public string StopSyncingSessionsFrom { get; set; }
        public DateTimeOffset StopSyncingSessionsFromDate => StopSyncingSessionsFrom != null ? DateTimeOffset.Parse(StopSyncingSessionsFrom) : DateTimeOffset.MinValue;

        // Agenda
        [AppSetting(Default = "SessionizeAgendaApiKey")]
        public string SessionizeAgendaApiKey { get; set; }
        [AppSetting(Default = "StopSyncingAgendaFrom")]
        public string StopSyncingAgendaFrom { get; set; }
        public DateTimeOffset StopSyncingAgendaFromDate => StopSyncingAgendaFrom != null ? DateTimeOffset.Parse(StopSyncingAgendaFrom) : DateTimeOffset.MinValue;
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindSessionizeReadModelSyncConfigAttribute : SessionizeReadModelSyncConfig {}

    public class BindSessionizeReadModelSyncConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindSessionizeReadModelSyncConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private SessionizeReadModelSyncConfig BuildItemFromAttr(BindSessionizeReadModelSyncConfigAttribute attr)
        {
            return attr;
        }
    }
}
