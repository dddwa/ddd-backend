using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Config
{
    public class ConferenceConfig : Attribute
    {
        [AppSetting(Default = "ConferenceInstance")]
        public string ConferenceInstance { get; set; }

        // Anonymous submissions
        [AppSetting(Default = "AnonymousSubmissions")]
        public string AnonymousSubmissionsAppSetting { get; set; }
        public bool AnonymousSubmissions => AnonymousSubmissionsAppSetting != "false";

        // Min votes
        [AppSetting(Default = "MinVotes")]
        public string MinVotesSetting { get; set; }
        public int MinVotes => MinVotesSetting != null ? Int32.Parse(MinVotesSetting) : 0;

        [AppSetting(Default = "MaxVotes")]
        public string MaxVotesSetting { get; set; }
        public int MaxVotes => MaxVotesSetting != null ? Int32.Parse(MaxVotesSetting) : 0;
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindConferenceConfigAttribute : ConferenceConfig { }

    public class BindConferenceConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindConferenceConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private ConferenceConfig BuildItemFromAttr(BindConferenceConfigAttribute attr)
        {
            return attr;
        }
    }
}
