using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{
    public class EventbriteSyncConfig : Attribute
    {
        [AppSetting(Default = "VotesConnectionString")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "EventbriteTable")]
        public string Table { get; set; }
        [AppSetting(Default = "EventbriteApiBearerToken")]
        public string ApiKey { get; set; }
        [AppSetting(Default = "EventbriteEventId")]
        public string EventId { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindEventbriteSyncConfigAttribute : EventbriteSyncConfig { }

    public class BindEventbriteSyncConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindEventbriteSyncConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private EventbriteSyncConfig BuildItemFromAttr(BindEventbriteSyncConfigAttribute attr)
        {
            return attr;
        }
    }
}
