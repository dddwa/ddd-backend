using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{
    public class TitoSyncConfig : Attribute
    {
        [AppSetting(Default = "VotesConnectionString")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "TitoTable")]
        public string Table { get; set; }
        [AppSetting(Default = "TitoApiBearerToken")]
        public string ApiKey { get; set; }
        [AppSetting(Default = "TitoAccountId")]
        public string AccountId { get; set; }
        [AppSetting(Default = "TitoEventId")]
        public string EventId { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindTitoSyncConfigAttribute : TitoSyncConfig { }

    public class BindTitoSyncConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindTitoSyncConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private TitoSyncConfig BuildItemFromAttr(BindTitoSyncConfigAttribute attr)
        {
            return attr;
        }
    }
}
