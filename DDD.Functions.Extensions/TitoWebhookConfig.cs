
using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{
    public class TitoWebhookConfig : Attribute
    {
        [AppSetting(Default = "TitoWebhookSecurityToken")]
        public string TitoWebhookSecurityToken { get; set; }

        [AppSetting(Default = "NotificationTable")]
        public string Table { get; set; }

        [AppSetting(Default = "NotificationConnectionString")]
        public string ConnectionString { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindTitoWebhookConfigAttribute : TitoWebhookConfig { }

    public class BindTitoWebhookConfigExtension : IExtensionConfigProvider
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
