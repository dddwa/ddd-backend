using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{
    public class KeyDatesConfig : Attribute
    {
        public DateTimeOffset Now => DateTimeOffset.UtcNow;

        // Sessionize Sync
        [AppSetting(Default = "StopSyncingSessionsFrom")]
        public string StopSyncingSessionsFrom { get; set; }
        public DateTimeOffset StopSyncingSessionsFromDate => StopSyncingSessionsFrom != null ? DateTimeOffset.Parse(StopSyncingSessionsFrom) : DateTimeOffset.MinValue;

        [AppSetting(Default = "StopSyncingAgendaFrom")]
        public string StopSyncingAgendaFrom { get; set; }
        public DateTimeOffset StopSyncingAgendaFromDate => StopSyncingAgendaFrom != null ? DateTimeOffset.Parse(StopSyncingAgendaFrom) : DateTimeOffset.MinValue;

        // Submissions
        [AppSetting(Default = "SubmissionsAvailableFrom")]
        public string SubmissionsAvailableFrom { get; set; }
        public DateTimeOffset SubmissionsAvailableFromDate => SubmissionsAvailableFrom != null ? DateTimeOffset.Parse(SubmissionsAvailableFrom) : DateTimeOffset.MaxValue;

        [AppSetting(Default = "SubmissionsAvailableTo")]
        public string SubmissionsAvailableTo { get; set; }
        public DateTimeOffset SubmissionsAvailableToDate => SubmissionsAvailableTo != null ? DateTimeOffset.Parse(SubmissionsAvailableTo) : DateTimeOffset.MinValue;

        // Voting
        [AppSetting(Default = "VotingAvailableFrom")]
        public string VotingAvailableFrom { get; set; }
        public DateTimeOffset VotingAvailableFromDate => VotingAvailableFrom != null ? DateTimeOffset.Parse(VotingAvailableFrom) : DateTimeOffset.MaxValue;

        [AppSetting(Default = "VotingAvailableTo")]
        public string VotingAvailableTo { get; set; }
        public DateTimeOffset VotingAvailableToDate => VotingAvailableTo != null ? DateTimeOffset.Parse(VotingAvailableTo) : DateTimeOffset.MinValue;

        // App Insights Sync
        public string AppInsightsApplicationKey { get; set; }
        [AppSetting(Default = "StartSyncingAppInsightsFrom")]
        public string StartSyncingAppInsightsFrom { get; set; }
        public DateTimeOffset StartSyncingAppInsightsFromDate => StartSyncingAppInsightsFrom != null ? DateTimeOffset.Parse(StartSyncingAppInsightsFrom) : DateTimeOffset.MinValue;

        [AppSetting(Default = "StopSyncingAppInsightsFrom")]
        public string StopSyncingAppInsightsFrom { get; set; }
        public DateTimeOffset StopSyncingAppInsightsFromDate => StopSyncingAppInsightsFrom != null ? DateTimeOffset.Parse(StopSyncingAppInsightsFrom) : DateTimeOffset.MinValue;


        // Tito Sync
        [AppSetting(Default = "StopSyncingTitoFrom")]
        public string StopSyncingTitoFrom { get; set; }
        public DateTimeOffset StopSyncingTitoFromDate => StopSyncingTitoFrom != null ? DateTimeOffset.Parse(StopSyncingTitoFrom) : DateTimeOffset.MinValue;


        public bool Before(Func<KeyDatesConfig, DateTimeOffset> date, TimeSpan? tolerance = null)
        {
            return Now < date(this).Add(tolerance ?? TimeSpan.Zero);
        }

        public bool After(Func<KeyDatesConfig, DateTimeOffset> date, TimeSpan? tolerance = null)
        {
            return Now > date(this).Add(tolerance ?? TimeSpan.Zero);
        }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindKeyDatesConfigAttribute : KeyDatesConfig { }

    public class BindKeyDatesConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindKeyDatesConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private KeyDatesConfig BuildItemFromAttr(BindKeyDatesConfigAttribute attr)
        {
            return attr;
        }
    }
}
