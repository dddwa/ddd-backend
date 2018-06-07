using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Config
{
    public class SubmissionsAndVotingConfig : Attribute
    {
        public DateTimeOffset Now => DateTimeOffset.UtcNow;

        // Conference details
        [AppSetting(Default = "ConferenceInstance")]
        public string ConferenceInstance { get; set; }

        // Sessions store
        [AppSetting(Default = "ConnectionStrings:Sessions")]
        public string SessionsConnectionString { get; set; }
        [AppSetting(Default = "SessionsDataSourceCosmosDatabaseId")]
        public string CosmosDatabaseId { get; set; }
        [AppSetting(Default = "SessionsDataSourceCosmosCollectionId")]
        public string CosmosCollectionId { get; set; }

        // Anonymous submissions
        [AppSetting(Default = "AnonymousSubmissions")]
        public string AnonymousSubmissionsAppSetting { get; set; }
        public bool AnonymousSubmissions => AnonymousSubmissionsAppSetting != "false";

        // Submission dates
        [AppSetting(Default = "SubmissionsAvailableFrom")]
        public string SubmissionsAvailableFrom { get; set; }
        public DateTimeOffset SubmissionsAvailableFromDate => SubmissionsAvailableFrom != null ? DateTimeOffset.Parse(SubmissionsAvailableFrom) : DateTimeOffset.MaxValue;
        [AppSetting(Default = "SubmissionsAvailableTo")]
        public string SubmissionsAvailableTo { get; set; }
        public DateTimeOffset SubmissionsAvailableToDate => SubmissionsAvailableTo != null ? DateTimeOffset.Parse(SubmissionsAvailableTo) : DateTimeOffset.MinValue;

        // Voting store
        [AppSetting(Default = "ConnectionStrings:Votes")]
        public string VotingConnectionString { get; set; }
        [AppSetting(Default = "VotingTable")]
        public string VotingTable { get; set; }

        // Voting dates
        [AppSetting(Default = "VotingAvailableFrom")]
        public string VotingAvailableFrom { get; set; }
        public DateTimeOffset VotingAvailableFromDate => VotingAvailableFrom != null ? DateTimeOffset.Parse(VotingAvailableFrom) : DateTimeOffset.MaxValue;
        [AppSetting(Default = "VotingAvailableTo")]
        public string VotingAvailableTo { get; set; }
        public DateTimeOffset VotingAvailableToDate => VotingAvailableTo != null ? DateTimeOffset.Parse(VotingAvailableTo) : DateTimeOffset.MinValue;

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
    public class BindSubmissionsAndVotingConfigAttribute : SubmissionsAndVotingConfig { }

    public class BindSubmissionsAndVotingConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindSubmissionsAndVotingConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private SubmissionsAndVotingConfig BuildItemFromAttr(BindSubmissionsAndVotingConfigAttribute attr)
        {
            return attr;
        }
    }
}
