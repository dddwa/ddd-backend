using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{

    public class EloVotingConfig : Attribute
    {
        [AppSetting(Default = "EloVotesConnectionString")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "EloVotingTable")]
        public string Table { get; set; }
        [AppSetting(Default = "EloPasswordPhrase")]
        public string EloPasswordPhrase { get; set; }
        [AppSetting(Default = "EloAllowedTimeInSecondsToSubmit")]
        public int EloAllowedTimeInSecondsToSubmit { get; set; }
        [AppSetting(Default = "EloEnabled")]
        public bool EloEnabled { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindEloVotingConfigAttribute : EloVotingConfig { }

    public class BindEloVotingConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindEloVotingConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private EloVotingConfig BuildItemFromAttr(BindEloVotingConfigAttribute attr)
        {
            return attr;
        }
    }
}
