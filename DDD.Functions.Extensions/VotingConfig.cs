using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{
    public enum TicketNumberWhileVoting
    {
        None,
        Required,
        Optional
    }

    public class VotingConfig : Attribute
    {
        [AppSetting(Default = "VotesConnectionString")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "VotingTable")]
        public string Table { get; set; }
        [AppSetting(Default = "TicketNumberWhileVoting")]
        public string TicketNumberWhileVoting { get; set; }

        public TicketNumberWhileVoting TicketNumberWhileVotingValue =>
            TicketNumberWhileVoting == null ?
                Extensions.TicketNumberWhileVoting.None  :
                (TicketNumberWhileVoting) Enum.Parse(typeof(TicketNumberWhileVoting), TicketNumberWhileVoting);
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
