using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Config
{
    public class EventbriteSyncConfig : Attribute
    {
        public DateTimeOffset Now => DateTimeOffset.UtcNow;

        [AppSetting(Default = "ConferenceInstance")]
        public string ConferenceInstance { get; set; }

        [AppSetting(Default = "VotesConnectionString")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "EventbriteTable")]
        public string EventbriteTable { get; set; }
        [AppSetting(Default = "EventbriteApiBearerToken")]
        public string EventbriteApiKey { get; set; }
        [AppSetting(Default = "EventbriteEventId")]
        public string EventId { get; set; }
        [AppSetting(Default = "StopSyncingEventbriteFrom")]
        public string StopSyncingEventbriteFrom { get; set; }
        public DateTimeOffset StopSyncingEventbriteFromDate => StopSyncingEventbriteFrom != null ? DateTimeOffset.Parse(StopSyncingEventbriteFrom) : DateTimeOffset.MinValue;
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
