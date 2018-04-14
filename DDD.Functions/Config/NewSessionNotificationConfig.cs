using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Config
{
    public class NewSessionNotificationConfig : Attribute
    {
        [AppSetting(Default = "ConnectionStrings:Sessions")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "NewSessionNotificationLogicAppUrl")]
        public string LogicAppUrl { get; set; }
        [AppSetting(Default = "SessionsDataSourceCosmosDatabaseId")]
        public string CosmosDatabaseId { get; set; }
        [AppSetting(Default = "SessionsDataSourceCosmosCollectionId")]
        public string CosmosCollectionId { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindNewSessionNotificationConfigAttribute : NewSessionNotificationConfig { }

    public class BindNewSessionNotificationConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindNewSessionNotificationConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private NewSessionNotificationConfig BuildItemFromAttr(BindNewSessionNotificationConfigAttribute attr)
        {
            return attr;
        }
    }
}
