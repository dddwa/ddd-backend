using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{
    public class AppInsightsSyncConfig : Attribute
    {
        [AppSetting(Default = "VotesConnectionString")]
        public string ConnectionString { get; set; }

        [AppSetting(Default = "AppInsightsTable")]
        public string Table { get; set; }

        [AppSetting(Default = "AppInsightsApplicationId")]
        public string ApplicationId { get; set; }

        [AppSetting(Default = "AppInsightsApplicationKey")]
        public string ApplicationKey { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindAppInsightsSyncConfigAttribute : AppInsightsSyncConfig
    {
    }

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