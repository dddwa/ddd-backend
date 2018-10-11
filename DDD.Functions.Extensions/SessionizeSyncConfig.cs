using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{
    public class SessionizeSyncConfig : Attribute
    {
        [AppSetting(Default = "SessionizeApiKey")]
        public string SubmissionsApiKey { get; set; }
        [AppSetting(Default = "SessionizeAgendaApiKey")]
        public string AgendaApiKey { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindSessionizeSyncConfigAttribute : SessionizeSyncConfig {}

    public class BindSessionizeSyncConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindSessionizeSyncConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private SessionizeSyncConfig BuildItemFromAttr(BindSessionizeSyncConfigAttribute attr)
        {
            return attr;
        }
    }
}
