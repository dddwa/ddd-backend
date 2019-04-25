using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{
    public class TitoWebhookConfig : Attribute
    {
        [AppSetting(Default = "TitoWebhookSecret")]
        public string Secret { get; set; }

        [AppSetting(Default = "TitoWebhookConnectionString")]
        public string ConnectionString { get; set; }

        [AppSetting(Default = "TitoWebhookDeDupeTable")]
        public string DeDupeTable { get; set; }

        [AppSetting(Default = "TitoWebhookOrderNotificationQueue")]
        public string OrderNotificationQueue { get; set; }

        [AppSetting(Default = "TitoWebhookTicketNotificationQueue")]
        public string TicketNotificationQueue { get; set; }

        [AppSetting(Default = "TitoApiToken")]
        public string ApiBearerToken { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindTitoWebhookConfigAttribute : TitoWebhookConfig { }

    public class TitoWebhookConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindTitoWebhookConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private TitoWebhookConfig BuildItemFromAttr(BindTitoWebhookConfigAttribute attr)
        {
            return attr;
        }
    }
}
