using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{
    public class AgendaScheduleConfig : Attribute
    {
        [AppSetting(Default = "AgendaScheduleConnectionString")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "AgendaScheduleContainer")]
        public string Container { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindAgendaScheduleConfigAttribute : AgendaScheduleConfig { }

    public class BindAgendaScheduleConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindAgendaScheduleConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private AgendaScheduleConfig BuildItemFromAttr(BindAgendaScheduleConfigAttribute attr)
        {
            return attr;
        }
    }
}
