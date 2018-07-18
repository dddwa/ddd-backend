using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Config
{
    public class SessionsConfig : Attribute
    {
        [AppSetting(Default = "SessionsConnectionString")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "SessionsTable")]
        public string SessionsTable { get; set; }
        [AppSetting(Default = "PresentersTable")]
        public string PresentersTable { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindSessionsConfigAttribute : SessionsConfig { }

    public class BindSessionsConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindSessionsConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private SessionsConfig BuildItemFromAttr(BindSessionsConfigAttribute attr)
        {
            return attr;
        }
    }
}
