using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{
    public class FeedbackConfig : Attribute
    {
        [AppSetting(Default = "FeedbackConnectionString")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "SessionFeedbackTable")]
        public string SessionFeedbackTable { get; set; }
        [AppSetting(Default = "ConferenceFeedbackTable")]
        public string ConferenceFeedbackTable { get; set; }
        [AppSetting(Default = "IsSingleVoteEligibleForPrizeDraw")]
        public bool IsSingleVoteEligibleForPrizeDraw { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindFeedbackConfigAttribute : FeedbackConfig { }

    public class BindFeedbackConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindFeedbackConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private FeedbackConfig BuildItemFromAttr(BindFeedbackConfigAttribute attr)
        {
            return attr;
        }
    }
}
