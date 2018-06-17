using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Config
{
    public class AppInsightsSyncConfig : Attribute
    {
        public DateTimeOffset Now => DateTimeOffset.UtcNow;

        [AppSetting(Default = "ConferenceInstance")]
        public string ConferenceInstance { get; set; }

        [AppSetting(Default = "VotesConnectionString")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "AppInsightsTable")]
        public string AppInsightsTable { get; set; }
        [AppSetting(Default = "AppInsightsApplicationId")]
        public string AppInsightsApplicationId { get; set; }
        [AppSetting(Default = "AppInsightsApplicationKey")]
        public string AppInsightsApplicationKey { get; set; }
        [AppSetting(Default = "StartSyncingAppInsightsFrom")]
        public string StartSyncingAppInsightsFrom { get; set; }
        public DateTimeOffset StartSyncingAppInsightsFromDate => StartSyncingAppInsightsFrom != null ? DateTimeOffset.Parse(StartSyncingAppInsightsFrom) : DateTimeOffset.MinValue;
        [AppSetting(Default = "StopSyncingAppInsightsFrom")]
        public string StopSyncingAppInsightsFrom { get; set; }
        public DateTimeOffset StopSyncingAppInsightsFromDate => StopSyncingAppInsightsFrom != null ? DateTimeOffset.Parse(StopSyncingAppInsightsFrom) : DateTimeOffset.MinValue;
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindAppInsightsSyncConfigAttribute : AppInsightsSyncConfig { }

    public class BindAppInsightsSyncConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindAppInsightsSyncConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private AppInsightsSyncConfig BuildItemFromAttr(BindAppInsightsSyncConfigAttribute attr)
        {
            return attr;
        }
    }
}
