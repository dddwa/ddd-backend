using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{
    public class VotingConfig : Attribute
    {
        [AppSetting(Default = "VotesConnectionString")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "VotingTable")]
        public string Table { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindVotingConfigAttribute : VotingConfig { }

    public class BindVotingConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindVotingConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private VotingConfig BuildItemFromAttr(BindVotingConfigAttribute attr)
        {
            return attr;
        }
    }
}
